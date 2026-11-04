// SKSE registers one listener per plugin for each sender, so the listener for SKSE's messages in Events.cpp calls SF_Lifecycle_Forward.
#include "Shared.h"

namespace
{
	void (*g_sessionHandler)(std::int32_t, const char*, std::int32_t) = nullptr;
}

// The managed side's SessionMessage has SKSE's message type values, and ignores a type it does not name.
void SF_Lifecycle_Forward(SKSE::MessagingInterface::Message* message)
{
	if (g_sessionHandler == nullptr)
	{
		return;
	}
	// SKSE's header does not state whether kSaveGame includes a name, so reading is bounded by dataLen.
	const bool named = (message->type == SKSE::MessagingInterface::kPreLoadGame || message->type == SKSE::MessagingInterface::kSaveGame
		|| message->type == SKSE::MessagingInterface::kDeleteGame) && message->data != nullptr && message->dataLen > 0;
	g_sessionHandler(static_cast<std::int32_t>(message->type), named ? static_cast<const char*>(message->data) : nullptr, named ? static_cast<std::int32_t>(message->dataLen) : 0);
}

extern "C" __declspec(dllexport) void SF_Lifecycle_Register(void (*handler)(std::int32_t, const char*, std::int32_t))
{
	g_sessionHandler = handler;
}
