// The game creates event sources at different points, so the generated sinks attach on each startup message up to data-loaded.
// Sinks on ScriptEventSourceHolder attach at data-loaded, since the game raises form events while it loads forms.
#include "Shared.h"

namespace
{
	using AnimationGraphHandler = void (*)(const char* tag, const char* payload, void* holder);
	AnimationGraphHandler g_animationGraphHandler = nullptr;

	// Every actor shares one sink instance, so an actor unloading cannot leave it dangling.
	class AnimationGraphEventSink : public RE::BSTEventSink<RE::BSAnimationGraphEvent>
	{
	public:
		RE::BSEventNotifyControl ProcessEvent(const RE::BSAnimationGraphEvent* a_event,
			RE::BSTEventSource<RE::BSAnimationGraphEvent>*) override
		{
			if (g_animationGraphHandler != nullptr && a_event != nullptr)
			{
				g_animationGraphHandler(a_event->tag.c_str(), a_event->payload.c_str(),
					const_cast<RE::TESObjectREFR*>(a_event->holder));
			}
			return RE::BSEventNotifyControl::kContinue;
		}

		static AnimationGraphEventSink* GetSingleton() { static AnimationGraphEventSink singleton; return &singleton; }
	};
}

extern "C" __declspec(dllexport) void SF_Events_RegisterAnimationGraph(AnimationGraphHandler handler)
{
	g_animationGraphHandler = handler;
}

extern "C" __declspec(dllexport) bool SF_Actor_AddAnimationGraphEventSink(RE::Actor* self)
{
	return self != nullptr && self->AddAnimationGraphEventSink(AnimationGraphEventSink::GetSingleton());
}

extern "C" __declspec(dllexport) bool SF_Actor_RemoveAnimationGraphEventSink(RE::Actor* self)
{
	if (self != nullptr)
	{
		self->RemoveAnimationGraphEventSink(AnimationGraphEventSink::GetSingleton());
		return true;
	}
	return false;
}

extern "C" void SF_Events_Install()
{
	SKSE::GetMessagingInterface()->RegisterListener([](SKSE::MessagingInterface::Message* message)
		{
			// Skyrim VR sends kPostLoad and kPostPostLoad with the loader lock acquired, under which starting CoreCLR deadlocks.
			if (message->type != SKSE::MessagingInterface::kPostLoad && message->type != SKSE::MessagingInterface::kPostPostLoad)
			{
				SF_StartFramework();
			}

			// Queued, so the SKSE plugins later in SKSE's order have handled this message and made their writes first.
			if (message->type == SKSE::MessagingInterface::kDataLoaded && SKSE::GetTaskInterface() != nullptr)
			{
				SKSE::GetTaskInterface()->AddTask([] { SF_Code_VerifyWrites(); });
			}

			if (!SF_FrameworkRunning())
			{
				return;
			}
			// A source does not add a sink it already contains.
			switch (message->type)
			{
			case SKSE::MessagingInterface::kPostLoad:
			case SKSE::MessagingInterface::kPostPostLoad:
			case SKSE::MessagingInterface::kInputLoaded:
			case SKSE::MessagingInterface::kDataLoaded:
				SF_AttachGeneratedEventSinks(message->type == SKSE::MessagingInterface::kDataLoaded);
				break;
			default:
				break;
			}
			SF_Lifecycle_Forward(message);
		});
}
