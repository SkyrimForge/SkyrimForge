#include "Shared.h"

#include <algorithm>
#include <filesystem>
#include <vector>

namespace
{
    using SaveHandler = void (*)();
    using LoadHandler = void (*)(std::uint32_t type, std::uint32_t version, const void* data, std::uint32_t length);
    using RevertHandler = void (*)();

    SaveHandler g_saveHandler = nullptr;
    LoadHandler g_loadHandler = nullptr;
    RevertHandler g_revertHandler = nullptr;

    // Set only during the callback that provides them, the only time SKSE's serialization interface is usable.
    const SKSE::SerializationInterface* g_writingInterface = nullptr;
    const SKSE::SerializationInterface* g_loadingInterface = nullptr;

    constexpr std::uint32_t Signature(char first, char second, char third, char fourth)
    {
        return (static_cast<std::uint32_t>(first) << 24) | (static_cast<std::uint32_t>(second) << 16) |
               (static_cast<std::uint32_t>(third) << 8) | static_cast<std::uint32_t>(fourth);
    }

    // Must not collide with another plugin's ID, and must not change, since saves store it.
    constexpr std::uint32_t uniqueId = Signature('S', 'F', 'H', 'P');

    void OnSave(SKSE::SerializationInterface* serialization)
    {
        if (g_saveHandler != nullptr)
        {
            g_writingInterface = serialization;
            g_saveHandler();
            g_writingInterface = nullptr;
        }

        SF_PapyrusEvents_SaveRecord(serialization);
    }

    void OnLoad(SKSE::SerializationInterface* serialization)
    {
        std::uint32_t type = 0;
        std::uint32_t version = 0;
        std::uint32_t length = 0;
        std::vector<char> buffer;
        g_loadingInterface = serialization;
        while (serialization->GetNextRecordInfo(type, version, length))
        {
            if (SF_PapyrusEvents_LoadRecord(serialization, type))
            {
                continue;
            }
            // Checked inside the loop, so the registration sets still read their records if no handler is set.
            if (g_loadHandler == nullptr)
            {
                continue;
            }
            buffer.assign(length, 0);
            const std::uint32_t readLength = length > 0 ? serialization->ReadRecordData(buffer.data(), length) : 0;
            g_loadHandler(type, version, buffer.data(), readLength);
        }
        g_loadingInterface = nullptr;
    }

    void OnRevert(SKSE::SerializationInterface*)
    {
        SF_PapyrusEvents_RevertAll();
        if (g_revertHandler != nullptr)
        {
            g_revertHandler();
        }
    }
}

extern "C" __declspec(dllexport) void SF_Saving_SetHandlers(SaveHandler save, LoadHandler load, RevertHandler revert)
{
    g_saveHandler = save;
    g_loadHandler = load;
    g_revertHandler = revert;
}

extern "C" __declspec(dllexport) bool SF_Saving_WriteRecord(std::uint32_t type, std::uint32_t version, const void* data, std::uint32_t length)
{
    return g_writingInterface != nullptr && g_writingInterface->WriteRecord(type, version, data, length);
}

extern "C" __declspec(dllexport) bool SF_Saving_ResolveFormId(std::uint32_t storedFormId, std::uint32_t* currentFormId)
{
    *currentFormId = 0;
    return g_loadingInterface != nullptr && g_loadingInterface->ResolveFormID(storedFormId, *currentFormId);
}

// Must match the folder SKSE's MakeSavePath builds.
extern "C" __declspec(dllexport) std::uint32_t SF_Saving_SaveFolder(char* buffer, std::uint32_t capacity)
{
    const std::optional<std::filesystem::path> logDirectory = SKSE::log::log_directory();
    if (!logDirectory)
    {
        return 0;
    }
    RE::INISettingCollection* settings = RE::INISettingCollection::GetSingleton();
    const RE::Setting* localSavePath = settings != nullptr ? settings->GetSetting("sLocalSavePath:General") : nullptr;
    const bool isSet = localSavePath != nullptr && localSavePath->GetType() == RE::Setting::Type::kString && localSavePath->GetString() != nullptr;
    // The / operator would replace the whole path with a setting that begins with a backslash.
    std::filesystem::path folder = logDirectory->parent_path();
    folder += "\\";
    folder += isSet ? localSavePath->GetString() : "Saves\\";
    const std::u8string utf8Folder = folder.u8string();
    std::copy_n(utf8Folder.data(), std::min<std::size_t>(utf8Folder.size(), capacity), buffer);
    return static_cast<std::uint32_t>(utf8Folder.size());
}

// Call after the managed side sets its handlers, since SKSE may load a save immediately.
extern "C" void SF_Saving_Install()
{
    const SKSE::SerializationInterface* serialization = SKSE::GetSerializationInterface();
    if (serialization == nullptr)
    {
        return;
    }
    serialization->SetUniqueID(uniqueId);
    serialization->SetSaveCallback(OnSave);
    serialization->SetLoadCallback(OnLoad);
    serialization->SetRevertCallback(OnRevert);
}
