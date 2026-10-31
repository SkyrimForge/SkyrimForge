#include <SKSE/SKSE.h>

namespace
{
    using RunHandler = void (*)(std::uint64_t token);

    RunHandler g_runHandler = nullptr;

    bool Queue(std::uint64_t token, bool isUITask)
    {
        const SKSE::TaskInterface* tasks = SKSE::GetTaskInterface();
        const RunHandler handler = g_runHandler;
        if (tasks == nullptr || handler == nullptr)
        {
            return false;
        }
        if (isUITask)
        {
            tasks->AddUITask([handler, token]() { handler(token); });
        }
        else
        {
            tasks->AddTask([handler, token]() { handler(token); });
        }
        return true;
    }
}

extern "C" __declspec(dllexport) void SF_Task_SetHandler(RunHandler handler)
{
    g_runHandler = handler;
}

extern "C" __declspec(dllexport) bool SF_Task_IsAvailable()
{
    return SKSE::GetTaskInterface() != nullptr;
}

extern "C" __declspec(dllexport) bool SF_Task_Add(std::uint64_t token)
{
    return Queue(token, false);
}

extern "C" __declspec(dllexport) bool SF_Task_AddUI(std::uint64_t token)
{
    return Queue(token, true);
}
