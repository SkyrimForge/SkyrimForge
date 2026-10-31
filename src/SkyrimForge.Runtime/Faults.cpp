#include "Shared.h"

#include <Windows.h>

#include <spdlog/spdlog.h>

#include <atomic>
#include <cstring>
#include <format>

namespace
{
    // The handler also runs for routine exceptions, so a count bounds what one session writes.
    std::atomic<int> g_writtenCount{ 0 };
    constexpr int maximumWrittenCount = 32;

    DWORD g_installedThreadId = 0;

    // Managed exceptions and C++ throws are excluded. Both are raised through RaiseException, so their address is inside it, and a routine one would spend the count.
    // A stack overflow is excluded, since reporting one needs stack, and none is left.
    bool IsFault(DWORD code)
    {
        switch (code)
        {
            case EXCEPTION_ACCESS_VIOLATION:
            case EXCEPTION_ILLEGAL_INSTRUCTION:
            case EXCEPTION_PRIV_INSTRUCTION:
            case EXCEPTION_INT_DIVIDE_BY_ZERO:
                return true;
            default:
                return false;
        }
    }

    const char* DescribeAccess(ULONG_PTR operation)
    {
        switch (operation)
        {
            case 0: return "reading";
            case 1: return "writing";
            case 8: return "executing";
            default: return "reaching";
        }
    }

    LONG CALLBACK OnException(EXCEPTION_POINTERS* pointers)
    {
        const EXCEPTION_RECORD* record = pointers != nullptr ? pointers->ExceptionRecord : nullptr;
        if (record == nullptr || !IsFault(record->ExceptionCode))
        {
            return EXCEPTION_CONTINUE_SEARCH;
        }
        if (g_writtenCount.fetch_add(1) >= maximumWrittenCount)
        {
            return EXCEPTION_CONTINUE_SEARCH;
        }

        const DWORD threadId = GetCurrentThreadId();
        const char* threadDescription = threadId == g_installedThreadId ? "the thread the framework was installed on" : "another thread";
        if (record->ExceptionCode == EXCEPTION_ACCESS_VIOLATION && record->NumberParameters >= 2)
        {
            spdlog::error("Fault 0x{:X} at {} on thread {} ({}), {} 0x{:X}.", record->ExceptionCode, SF_Faults_DescribeAddress(record->ExceptionAddress),
                threadId, threadDescription, DescribeAccess(record->ExceptionInformation[0]), record->ExceptionInformation[1]);
        }
        else
        {
            spdlog::error("Fault 0x{:X} at {} on thread {} ({}).", record->ExceptionCode, SF_Faults_DescribeAddress(record->ExceptionAddress), threadId, threadDescription);
        }

        return EXCEPTION_CONTINUE_SEARCH;
    }
}

std::string SF_Faults_DescribeAddress(const void* address)
{
    HMODULE module = nullptr;
    const bool found = GetModuleHandleExA(
        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        static_cast<LPCSTR>(address), &module) != 0;
    if (!found)
    {
        return "no module";
    }

    char path[MAX_PATH] = {};
    if (GetModuleFileNameA(module, path, MAX_PATH) == 0)
    {
        return "unnamed module";
    }

    const char* name = std::strrchr(path, '\\');
    const std::uintptr_t offset = reinterpret_cast<std::uintptr_t>(address) - reinterpret_cast<std::uintptr_t>(module);
    return std::format("{}+0x{:X}", name != nullptr ? name + 1 : path, offset);
}

// SafetyHook's handler, registered later, runs first and handles only faults on pages it hooked.
extern "C" void SF_Faults_Install()
{
    g_installedThreadId = GetCurrentThreadId();
    if (AddVectoredExceptionHandler(1, OnException) != nullptr)
    {
        return;
    }

    spdlog::error("The fault handler could not be installed, so faults will not be logged.");
}
