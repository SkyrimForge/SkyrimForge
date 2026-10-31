// SKSE registers one listener per plugin for each sender, so the listener for SKSE's messages in Events.cpp calls SF_Lifecycle_Forward.
#include "Shared.h"

namespace
{
	// The managed side mirrors these values.
	enum class SessionMessage : std::int32_t
	{
		PostLoad = 1,
		PostPostLoad = 2,
		InputLoaded = 3,
		DataLoaded = 4,
		NewGame = 5,
		PreLoadGame = 6,
		PostLoadGame = 7,
		SaveGame = 8,
		DeleteGame = 9
	};

	void (*g_sessionHandler)(std::int32_t, const char*, std::int32_t) = nullptr;
}

void SF_Lifecycle_Forward(SKSE::MessagingInterface::Message* message)
{
	if (g_sessionHandler == nullptr)
	{
		return;
	}

	SessionMessage sessionMessage;
	switch (message->type)
	{
	case SKSE::MessagingInterface::kPostLoad: sessionMessage = SessionMessage::PostLoad; break;
	case SKSE::MessagingInterface::kPostPostLoad: sessionMessage = SessionMessage::PostPostLoad; break;
	case SKSE::MessagingInterface::kInputLoaded: sessionMessage = SessionMessage::InputLoaded; break;
	case SKSE::MessagingInterface::kDataLoaded: sessionMessage = SessionMessage::DataLoaded; break;
	case SKSE::MessagingInterface::kNewGame: sessionMessage = SessionMessage::NewGame; break;
	case SKSE::MessagingInterface::kPreLoadGame: sessionMessage = SessionMessage::PreLoadGame; break;
	case SKSE::MessagingInterface::kPostLoadGame: sessionMessage = SessionMessage::PostLoadGame; break;
	case SKSE::MessagingInterface::kSaveGame: sessionMessage = SessionMessage::SaveGame; break;
	case SKSE::MessagingInterface::kDeleteGame: sessionMessage = SessionMessage::DeleteGame; break;
	default: return;
	}

	const char* name = nullptr;
	std::int32_t length = 0;
	// SKSE's header does not state whether kSaveGame includes a name, so reading is bounded by dataLen.
	if (sessionMessage == SessionMessage::PreLoadGame || sessionMessage == SessionMessage::SaveGame || sessionMessage == SessionMessage::DeleteGame)
	{
		if (message->data != nullptr && message->dataLen > 0)
		{
			name = static_cast<const char*>(message->data);
			length = static_cast<std::int32_t>(message->dataLen);
		}
	}

	g_sessionHandler(static_cast<std::int32_t>(sessionMessage), name, length);
}

extern "C" __declspec(dllexport) void SF_Lifecycle_Register(void (*handler)(std::int32_t, const char*, std::int32_t))
{
	g_sessionHandler = handler;
}
