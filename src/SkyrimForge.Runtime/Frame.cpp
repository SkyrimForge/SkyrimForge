#include "Shared.h"

namespace
{
	using UpdateFunction = void (*)(RE::Main*, float);

	UpdateFunction g_originalUpdate = nullptr;
	void (*g_frameHandler)(float) = nullptr;
	bool g_installed = false;

	std::uint32_t g_lastUiRunTime = 0;
	std::uint32_t g_lastGameRunTime = 0;

	// The main loop also calls Main::Update without running a frame, and runTimeMS advances only once per frame.
	// On Skyrim VR the game's timer advances on every update, and the field CommonLib declares as the UI's timer is a different value.
	bool DetectNewFrame()
	{
		const RE::UI* ui = RE::UI::GetSingleton();
		if (ui == nullptr)
		{
			return true;
		}
		const std::uint32_t uiRunTime = ui->uiTimer.runTimeMS;
		const std::uint32_t gameRunTime = RE::BSTimer::GetSingleton()->runTimeMS;
		if (uiRunTime == g_lastUiRunTime && gameRunTime == g_lastGameRunTime)
		{
			return false;
		}
		g_lastUiRunTime = uiRunTime;
		g_lastGameRunTime = gameRunTime;
		return true;
	}

	// The second argument is not a frame time. GetSecondsSinceLastFrame is called after the original returns, once per frame, so every subscriber receives the same value.
	void FrameThunk(RE::Main* main, float secondArgument)
	{
		g_originalUpdate(main, secondArgument);
		if (DetectNewFrame() && g_frameHandler != nullptr)
		{
			g_frameHandler(RE::GetSecondsSinceLastFrame());
		}
	}

	// A call at the per-runtime offset into the game's code is rejected, since SF_Hook_FindCall finds a direct call to Main::Update, and any other call means the offset is wrong.
	std::uintptr_t FindReplacedCall(std::uintptr_t mainLoop, std::uint32_t seOffset, std::uint32_t aeOffset, std::uint32_t vrOffset)
	{
		const std::uintptr_t site = SF_Hook_CallAt(mainLoop, seOffset, aeOffset, vrOffset);
		if (site == 0)
		{
			return 0;
		}
		const std::uintptr_t target = site + 5 + *reinterpret_cast<const std::int32_t*>(site + 1);
		const REL::Segment text = REL::Module::get().segment(REL::Segment::textx);
		return target >= text.address() && target < text.address() + text.size() ? 0 : site;
	}
}

// A call an earlier SKSE plugin replaced is found at the per-runtime offset, and the hook then calls that plugin's replacement.
extern "C" __declspec(dllexport) bool SF_Frame_Register(void (*handler)(float), std::uintptr_t mainLoop, std::uintptr_t mainUpdate, std::uint32_t seOffset,
	std::uint32_t aeOffset, std::uint32_t vrOffset)
{
	g_frameHandler = handler;
	if (g_installed)
	{
		return true;
	}
	if (mainLoop == 0 || mainUpdate == 0)
	{
		return false;
	}

	std::uintptr_t site = SF_Hook_FindCall(mainLoop, mainUpdate, 0, 0x200);
	if (site == 0)
	{
		site = FindReplacedCall(mainLoop, seOffset, aeOffset, vrOffset);
	}
	if (site == 0)
	{
		return false;
	}

	g_originalUpdate = reinterpret_cast<UpdateFunction>(SF_Hook_WriteCall(site, reinterpret_cast<std::uintptr_t>(&FrameThunk), "SkyrimForge"));
	if (g_originalUpdate == nullptr)
	{
		return false;
	}
	g_installed = true;
	return true;
}
