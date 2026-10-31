// RE::CreateMessage ends the process when passed zero for its three undocumented arguments, so the game's MessageBoxData factory is used.
// BSIntrusiveRefCounted declares TES_HEAP_REDEFINE_NEW(), so the callback is allocated where the game frees it.
#include "Shared.h"

#include <cstdint>

namespace
{
	// A box destroyed without a press still calls the handler.
	using ButtonHandler = void (*)(std::int32_t, std::uint32_t, std::uint8_t);
	ButtonHandler g_buttonHandler = nullptr;

	class ManagedMessageBoxCallback : public RE::IMessageBoxCallback
	{
	public:
		explicit ManagedMessageBoxCallback(std::int32_t a_token) :
			token(a_token)
		{
			pad0C = 0;
		}

		~ManagedMessageBoxCallback() override
		{
			if (g_buttonHandler != nullptr)
			{
				g_buttonHandler(token, 0, 0);
			}
		}

		void Run(std::uint8_t a_button) override
		{
			if (g_buttonHandler != nullptr)
			{
				g_buttonHandler(token, static_cast<std::uint32_t>(a_button), 1);
			}
		}

	private:
		std::int32_t token;
	};

	// Separate from the caller because a function using __try cannot contain objects that need destruction. 1 is EXCEPTION_EXECUTE_HANDLER.
	// A fault is caught, since a relocation wrong for this runtime would otherwise end the process.
	std::int32_t Queue(RE::MessageBoxData* data)
	{
		// CommonLib declares no MessageBoxData::QueueMessage.
		const std::uintptr_t address = SF_Hook_FunctionAddress(51422, 52271, 51422);
		if (address == 0)
		{
			return 3;
		}

		__try
		{
			using QueueMessageFunction = void (*)(RE::MessageBoxData*);
			reinterpret_cast<QueueMessageFunction>(address)(data);
			return 0;
		}
		__except (1)
		{
			return 4;
		}
	}
}

extern "C" __declspec(dllexport) void SF_MessageBoxData_SetCallback(
	void (*handler)(std::int32_t, std::uint32_t, std::uint8_t))
{
	g_buttonHandler = handler;
}

// The message data and its callback are not freed if Queue fails.
extern "C" __declspec(dllexport) std::int32_t SF_MessageBoxData_QueueMessage(const char* message, std::int32_t token,
	const char* const* buttonTexts, std::int32_t count)
{
	if (message == nullptr || buttonTexts == nullptr || count < 1)
	{
		return 1;
	}
	for (std::int32_t i = 0; i < count; ++i)
	{
		if (buttonTexts[i] == nullptr)
		{
			return 1;
		}
	}
	RE::InterfaceStrings* strings = RE::InterfaceStrings::GetSingleton();
	RE::MessageDataFactoryManager* manager = RE::MessageDataFactoryManager::GetSingleton();
	if (strings == nullptr || manager == nullptr)
	{
		return 2;
	}
	const RE::BSTDerivedCreator<RE::MessageBoxData, RE::IUIMessageData>* creator =
		manager->GetCreator<RE::MessageBoxData>(strings->messageBoxData);
	if (creator == nullptr)
	{
		return 2;
	}
	RE::MessageBoxData* data = creator->Create();
	if (data == nullptr)
	{
		return 2;
	}

	// The message data's destruction frees the callback.
	data->callback = RE::BSTSmartPointer<RE::IMessageBoxCallback>(new ManagedMessageBoxCallback(token));
	data->bodyText = message;
	for (std::int32_t i = 0; i < count; ++i)
	{
		data->buttonText.emplace_back(buttonTexts[i]);
	}

	return Queue(data);
}
