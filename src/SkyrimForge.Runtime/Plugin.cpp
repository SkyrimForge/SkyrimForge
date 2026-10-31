#include "Shared.h"
#include <spdlog/sinks/basic_file_sink.h>

using namespace std::string_view_literals;

namespace
{
    bool g_frameworkAttempted = false;
    bool g_frameworkRunning = false;

    // Read once, since SKSE chooses it by testing for files in the working directory, and a second read at the deferred start could differ.
    std::string g_logDirectory;

    std::string SetUpLog()
    {
        std::optional<std::filesystem::path> directory = SKSE::log::log_directory();
        if (!directory)
        {
            return {};
        }
        // string() converts to the ANSI code page, which cannot represent every folder name.
        const std::u8string utf8Folder = directory->u8string();
        std::string folder(utf8Folder.begin(), utf8Folder.end());
        const std::filesystem::path logPath = *directory / "SkyrimForge.Runtime.log";
        // Not registered, since spdlog's registry throws on a duplicate name. Appended, so the record of a fault survives the next launch.
        auto sink = std::make_shared<spdlog::sinks::basic_file_sink_mt>(logPath.string(), false);
        std::shared_ptr<spdlog::logger> logger = std::make_shared<spdlog::logger>("SkyrimForge.Runtime", std::move(sink));
        logger->set_level(spdlog::level::info);
        logger->flush_on(spdlog::level::info);
        spdlog::set_default_logger(std::move(logger));
        return folder;
    }
}

extern "C" bool SF_FrameworkRunning()
{
    return g_frameworkRunning;
}

extern "C" __declspec(dllexport) std::uint8_t SF_Module_Edition()
{
    return static_cast<std::uint8_t>(REL::Module::GetRuntime());
}

extern "C" __declspec(dllexport) std::uintptr_t SF_Module_Base()
{
    return REL::Module::get().base();
}

extern "C" __declspec(dllexport) void SF_Module_Version(std::uint16_t* parts)
{
    const REL::Version version = REL::Module::get().version();
    for (std::size_t i = 0; i < 4; ++i)
    {
        parts[i] = version[i];
    }
}

extern "C" __declspec(dllexport) void SF_Module_Segment(std::uint32_t name, std::uintptr_t* address, std::size_t* size)
{
    const REL::Segment segment = REL::Module::get().segment(static_cast<REL::Segment::Name>(name));
    *address = segment.address();
    *size = segment.size();
}

extern "C" bool SF_StartFramework()
{
    if (g_frameworkAttempted)
    {
        return g_frameworkRunning;
    }
    g_frameworkAttempted = true;

    // Not checked in SetUpLog, since Skyrim VR loads plugins with the loader lock acquired and ShowStartFailure creates a window.
    if (g_logDirectory.empty())
    {
        ShowStartFailure("Windows resolved no Documents folder, so SKSE has no log folder", false);
        return false;
    }

    // Installed before plugins start, so SafetyHook's handler, created later, runs first.
    SF_Faults_Install();

    const char* error = StartClr([](const char* step) { spdlog::info("CLR host: {}", step); }, g_logDirectory.c_str());
    if (error != nullptr)
    {
        spdlog::error("The CLR host failed: {}. The framework will not run.", error);
        ShowStartFailure(error, true);
        return false;
    }

    spdlog::info("Managed framework hosted. It logs to SkyrimForge.log.");

    // The game may load a save immediately, so this follows the framework's handler registration.
    SF_Saving_Install();
    g_frameworkRunning = true;
    return true;
}

SKSEPluginInfo(
    .Version = REL::Version{ 0, 1, 1 },
    .Name = "SkyrimForge"sv,
    .Author = "Wessley Mitchell (wessleym)"sv,
    .RuntimeCompatibility = SKSE::VersionIndependence::AddressLibrary)

SKSEPluginLoad(const SKSE::LoadInterface* skse)
{
    // SKSE reports a plugin that throws as a fatal error without naming the exception.
    try
    {
        // false disables SKSE's logger, which would also write to SkyrimForge.log, the managed framework's log.
        SKSE::Init(skse, false);
        g_logDirectory = SetUpLog();
        SF_Hook_AllocateTrampoline();

        SF_Events_Install();

        // Skyrim VR calls this with the loader lock acquired, and CoreCLR waits for threads that cannot attach until the lock is released.
        if (REL::Module::IsVR())
        {
            spdlog::info("Skyrim VR: the runtime starts on the first message after plugins load.");
            return true;
        }
        // True even if the start failed, since ShowStartFailure has already shown the reason.
        SF_StartFramework();
        return true;
    }
    catch (const std::exception& exception)
    {
        spdlog::error("The plugin threw while loading: {}. The framework will not run.", exception.what());
        return false;
    }
    catch (...)
    {
        spdlog::error("The plugin threw a non-std::exception while loading. The framework will not run.");
        return false;
    }
}
