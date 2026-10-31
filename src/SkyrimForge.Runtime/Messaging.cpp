#include <RE/Skyrim.h>
#include <SKSE/SKSE.h>

extern "C" __declspec(dllexport) void SF_Messaging_SendModEvent(const char* eventName, const char* stringArgument, float numberArgument, RE::TESForm* sender)
{
    RE::BSTEventSource<SKSE::ModCallbackEvent>* source = SKSE::GetModCallbackEventSource();
    if (source == nullptr)
    {
        return;
    }
    // SendEvent runs every sink before it returns, so a stack local is sound.
    SKSE::ModCallbackEvent event{ eventName, stringArgument, numberArgument, sender };
    source->SendEvent(&event);
}
// SKSE keys a listener by the listening plugin and the sender, so registering for another sender leaves the framework's own listener for SKSE in place.
namespace
{
    // One handler for every sender, since SKSE's callback has no state parameter.
    void (*managedHandler)(std::uint32_t, void*, std::uint32_t, const char*) = nullptr;
}

extern "C" __declspec(dllexport) void SF_Messaging_SetHandler(void (*handler)(std::uint32_t, void*, std::uint32_t, const char*))
{
    managedHandler = handler;
}

extern "C" __declspec(dllexport) bool SF_Messaging_Listen(const char* sender)
{
    return SKSE::GetMessagingInterface()->RegisterListener(sender, [](SKSE::MessagingInterface::Message* message)
        {
            if (managedHandler == nullptr)
            {
                return;
            }
            managedHandler(message->type, message->data, message->dataLen, message->sender);
        });
}

extern "C" __declspec(dllexport) bool SF_Messaging_Dispatch(std::uint32_t messageType, void* data, std::uint32_t length, const char* receiver)
{
    return SKSE::GetMessagingInterface()->Dispatch(messageType, data, length, receiver);
}
