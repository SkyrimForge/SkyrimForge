#include "Shared.h"

#include <cstring>
#include <iterator>

namespace
{
	using AddEventFunction = std::uint32_t (*)(RE::BGSStoryEventManager*, std::uint32_t, const void*);
	using StoryHandler = void (*)(std::uint32_t index, const std::uint64_t* members, std::int32_t count);

	AddEventFunction g_originalAddEvent = nullptr;
	StoryHandler g_storyHandler = nullptr;
	bool g_installed = false;

	// Story events raise no BSTEventSource, so this hooks the function every one of them enters.
	// The handler runs after the lock is released, since the lock is not recursive and a subscriber can raise a story event.
	std::uint32_t AddEventThunk(RE::BGSStoryEventManager* manager, std::uint32_t index, const void* event)
	{
		const std::uint32_t eventId = g_originalAddEvent(manager, index, event);
		if (g_storyHandler == nullptr)
		{
			return eventId;
		}

		std::uint64_t members[6]{};
		std::uint32_t raisedIndex = 0;
		bool queued = false;
		{
			RE::BSSpinLockGuard lock(manager->eventArrayLock);
			const std::uint32_t count = manager->events.size();
			if (count > 0 && manager->events[count - 1].id == eventId)
			{
				const RE::BGSStoryEvent& raisedEvent = manager->events[count - 1];
				raisedIndex = raisedEvent.index;
				static_assert(sizeof(members) == sizeof(raisedEvent.members));
				std::memcpy(members, raisedEvent.members, sizeof(members));
				queued = true;
			}
		}
		if (queued)
		{
			g_storyHandler(raisedIndex, members, static_cast<std::int32_t>(std::size(members)));
		}
		return eventId;
	}
}

extern "C" __declspec(dllexport) bool SF_Story_Register(void (*handler)(std::uint32_t, const std::uint64_t*, std::int32_t), std::uintptr_t addEvent)
{
	g_storyHandler = handler;
	if (g_installed)
	{
		return true;
	}
	if (addEvent == 0)
	{
		return false;
	}

	g_originalAddEvent = reinterpret_cast<AddEventFunction>(SF_Hook_WriteEntry(addEvent, reinterpret_cast<std::uintptr_t>(&AddEventThunk), "SkyrimForge"));
	if (g_originalAddEvent == nullptr)
	{
		return false;
	}
	g_installed = true;
	return true;
}
