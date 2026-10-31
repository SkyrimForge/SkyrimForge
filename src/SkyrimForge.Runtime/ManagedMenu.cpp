// The factory signature IMenu*() takes no argument, so each registration has its own template-instantiated factory.
#include <RE/Skyrim.h>

#include <array>
#include <cstddef>
#include <cstdint>
#include <mutex>
#include <string>
#include <utility>

namespace
{
	constexpr std::size_t registrationCount = 32;

	struct MenuRegistration
	{
		bool inUse = false;
		std::int32_t token = 0;
		std::string movieFile;
		std::uint32_t flags = 0;
		std::int8_t depth = 3;
		std::uint32_t context = 0;
	};

	std::array<MenuRegistration, registrationCount> g_registrations{};

	// Plugins may register from any thread.
	std::mutex g_registrationMutex;

	using MessageHandler = std::int32_t (*)(std::int32_t, std::uint32_t);
	MessageHandler g_messageHandler = nullptr;

	class ManagedMenu : public RE::IMenu
	{
	public:
		explicit ManagedMenu(const MenuRegistration& a_registration) :
			token(a_registration.token)
		{
			depthPriority = a_registration.depth;
			menuFlags = static_cast<RE::UI_MENU_FLAGS>(a_registration.flags);
			inputContext = static_cast<RE::UserEvents::INPUT_CONTEXT_ID>(a_registration.context);
			if (!a_registration.movieFile.empty())
			{
				RE::BSScaleformManager* manager = RE::BSScaleformManager::GetSingleton();
				if (manager != nullptr)
				{
					manager->LoadMovie(this, uiMovie, a_registration.movieFile.c_str());
				}
			}
		}

		RE::UI_MESSAGE_RESULTS ProcessMessage(RE::UIMessage& a_message) override
		{
			if (g_messageHandler == nullptr)
			{
				return RE::UI_MESSAGE_RESULTS::kPassOn;
			}
			return static_cast<RE::UI_MESSAGE_RESULTS>(g_messageHandler(token, static_cast<std::uint32_t>(a_message.type.get())));
		}

		// CommonLib's AdvanceMovie and PostDisplay dereference uiMovie, which is null if the movie did not load.
		void AdvanceMovie(float a_interval, std::uint32_t a_currentTime) override
		{
			if (uiMovie != nullptr)
			{
				RE::IMenu::AdvanceMovie(a_interval, a_currentTime);
			}
		}

		void PostDisplay() override
		{
			if (uiMovie != nullptr)
			{
				RE::IMenu::PostDisplay();
			}
		}

	private:
		// RE::IMenu ends at 0x30 on SE and AE and at 0x40 on VR, where the game writes a menuName at 0x38. This unit is built for all three runtimes, so the base ends at 0x30.
		// Reserving the range confines the game's VR writes to the object, and zero is an empty BSFixedString. Declared before token, so that token lies past the range.
		std::byte vrRuntimeData[0x10]{};

		std::int32_t token;
	};

	// IMenu inherits GFC_MEMORY_REDEFINE_NEW_IMPL, so new allocates on Scaleform's heap, where the game frees it.
	template <std::size_t Index>
	RE::IMenu* CreateForRegistration()
	{
		return new ManagedMenu(g_registrations[Index]);
	}

	template <std::size_t... Index>
	constexpr std::array<RE::UI::Create_t*, sizeof...(Index)> MakeFactories(std::index_sequence<Index...>)
	{
		return { &CreateForRegistration<Index>... };
	}

	const std::array<RE::UI::Create_t*, registrationCount> g_factories = MakeFactories(std::make_index_sequence<registrationCount>{});
}

// The handler runs on the game's interface thread with the game stopped, so it may call the game directly.
extern "C" __declspec(dllexport) void SF_ManagedMenu_SetHandler(std::int32_t (*handler)(std::int32_t, std::uint32_t))
{
	g_messageHandler = handler;
}

extern "C" __declspec(dllexport) std::int32_t SF_UI_RegisterManagedMenu(const char* name, std::int32_t token,
	const char* movieFile, std::uint32_t flags, std::int8_t depth, std::uint32_t context)
{
	if (name == nullptr)
	{
		return 3;
	}
	RE::UI* ui = RE::UI::GetSingleton();
	if (ui == nullptr)
	{
		return 1;
	}

	// UI::Register silently leaves an existing entry in place. Checked before a registration is claimed, since a registration is never released.
	if (ui->menuMap.find(RE::BSFixedString(name)) != ui->menuMap.end())
	{
		return 4;
	}

	std::scoped_lock lock(g_registrationMutex);
	for (std::size_t i = 0; i < registrationCount; ++i)
	{
		if (g_registrations[i].inUse)
		{
			continue;
		}
		// Filled before the factory is registered, since the game may construct the menu immediately.
		g_registrations[i] = MenuRegistration{
			.inUse = true,
			.token = token,
			.movieFile = movieFile == nullptr ? std::string() : std::string(movieFile),
			.flags = flags,
			.depth = depth,
			.context = context
		};
		ui->Register(name, g_factories[i]);
		return 0;
	}
	return 2;
}
