using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace SkyrimForge.Infrastructure;

internal sealed class CoSave
{
    internal const uint RecordType = 0x53464B56; // "SFKV"

    // Increment when CoSaveRecord serializes or compresses differently.
    internal const uint RecordVersion = 1;

    private readonly CoSaveStore store = new();
    private readonly CoSaveFormIdResolver formIdResolver;
    private readonly ILog log;

    private static CoSave? instance;

    internal CoSave(ILog log)
    {
        this.log = log;
        // ResolveFormId is read on each call, because the init property is set after the constructor runs.
        formIdResolver = new CoSaveFormIdResolver(formId => ResolveFormId(formId), log);
    }

    internal Func<uint, uint?> ResolveFormId { get; init; } = formId => NativeGlobals.SF_Saving_ResolveFormId(formId, out uint resolvedFormId) ? resolvedFormId : null;

    internal Func<byte[], bool> WriteRecord { get; init; } = Write;

    internal Func<string> GetSaveFolder { get; init; } = ReadSaveFolder;

    public unsafe void Install()
    {
        instance = this;
        NativeGlobals.SF_Saving_SetHandlers(&OnSave, &OnLoad, &OnRevert);
    }

    // An exception escaping one of these handlers ends the process.
    [UnmanagedCallersOnly]
    private static void OnSave() => InvokeWithTryCatch(state => state.Save(), nameof(OnSave));

    [UnmanagedCallersOnly]
    private static void OnLoad(uint type, uint version, nint data, uint length)
    {
        // Copied because the native buffer exists only for this call.
        byte[] buffer = new byte[length];
        if (length > 0)
        {
            Marshal.Copy(data, buffer, 0, (int)length);
        }
        InvokeWithTryCatch(state => state.Load(type, version, buffer), nameof(OnLoad));
    }

    [UnmanagedCallersOnly]
    private static void OnRevert() => InvokeWithTryCatch(state => state.Revert(), nameof(OnRevert));

    private static void InvokeWithTryCatch(Action<CoSave> action, string hookName)
    {
        CoSave? state = instance;
        if (state == null)
        {
            return;
        }
        try
        {
            action(state);
        }
        catch (Exception ex)
        {
            state.log.Write($"{hookName} failed: {ex}");
        }
    }

    public IPerPluginSavedState CreatePluginSavedState(string pluginTypeName) => new PerPluginSavedState(this, store, pluginTypeName);

    public CoSaveEntries? ReadEntries(string pluginTypeName, string saveFileName)
    {
        const string saveExtension = ".ess";
        string saveName = saveFileName.EndsWith(saveExtension, StringComparison.OrdinalIgnoreCase) ? saveFileName[..^saveExtension.Length] : saveFileName;
        string path = Path.Combine(GetSaveFolder(), saveName + ".skse");
        if (File.Exists(path) && CoSaveFile.TryFindRecord(File.ReadAllBytes(path), CoSaveFile.PluginId, RecordType, out uint version, out byte[] data))
        {
            if (version != RecordVersion)
            {
                throw new InvalidDataException($"{path} contains state at version {version}. This build reads only version {RecordVersion}.");
            }
            CoSaveRecord? record;
            try
            {
                record = CoSaveRecord.Decode(data);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"The saved state in {path} could not be decoded.", ex);
            }
            return record?.EntriesByPlugin?.GetValueOrDefault(pluginTypeName);
        }
        return null;
    }

    public void Save()
    {
        try
        {
            byte[] record = store.Encode();
            if (!WriteRecord(record))
            {
                log.Write("SKSE rejected the saved state record. This save contains no saved state.");
            }
        }
        catch (Exception ex)
        {
            log.Write($"Could not write the saved state: {ex}");
        }
    }

    public void Load(uint type, uint version, ReadOnlySpan<byte> data)
    {
        if (type != RecordType)
        {
            return;
        }
        if (version != RecordVersion)
        {
            log.Write($"The save contains state at version {version}. This build reads only version {RecordVersion}.");
            return;
        }
        try
        {
            CoSaveRecord? decodedRecord = CoSaveRecord.Decode(data);
            if (decodedRecord == null)
            {
                return;
            }
            if (decodedRecord.EntriesByPlugin == null || decodedRecord.EntriesByPluginAndForm == null)
            {
                log.Write("The save's state record is missing a section. None of its state was read.");
                return;
            }
            // Deserializing a damaged record can put null where the declared type is not nullable.
            foreach ((string pluginTypeName, CoSaveEntries entries) in decodedRecord.EntriesByPlugin)
            {
                if (entries != null)
                {
                    store.Set(pluginTypeName, formIdResolver.Resolve(entries));
                }
            }
            foreach ((string pluginTypeName, Dictionary<uint, CoSaveEntries> entriesByForm) in decodedRecord.EntriesByPluginAndForm)
            {
                if (entriesByForm != null)
                {
                    store.SetFormEntries(pluginTypeName, formIdResolver.ResolveFormEntries(entriesByForm));
                }
            }
        }
        catch (Exception ex)
        {
            log.Write($"Could not read the saved state. Plugins start without it: {ex}");
        }
    }

    public void Revert()
    {
        store.Clear();
    }

    private static unsafe string ReadSaveFolder()
    {
        uint length = NativeGlobals.SF_Saving_SaveFolder(null, 0);
        if (length == 0)
        {
            throw new InvalidOperationException("SKSE provides no co-save folder.");
        }
        byte[] folder = new byte[length];
        fixed (byte* buffer = folder)
        {
            NativeGlobals.SF_Saving_SaveFolder(buffer, length);
        }
        return Encoding.UTF8.GetString(folder);
    }

    private static bool Write(byte[] record)
    {
        // SKSE copies the record before returning.
        GCHandle pinnedHandle = GCHandle.Alloc(record, GCHandleType.Pinned);
        try
        {
            return NativeGlobals.SF_Saving_WriteRecord(RecordType, RecordVersion, pinnedHandle.AddrOfPinnedObject(), (uint)record.Length);
        }
        finally
        {
            pinnedHandle.Free();
        }
    }
}
