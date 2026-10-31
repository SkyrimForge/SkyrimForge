using System;
using System.Buffers.Binary;
using System.IO;

namespace SkyrimForge.Infrastructure;

// Reads the format SKSE's Serialization.cpp writes.
internal static class CoSaveFile
{
    private const uint signature = 0x45534B53; // "SKSE"

    private const uint formatVersion = 1;

    // Matches the unique ID in Saving.cpp.
    internal const uint PluginId = 0x53464850; // "SFHP"

    internal static bool TryFindRecord(ReadOnlySpan<byte> file, uint pluginId, uint recordType, out uint version, out byte[] data)
    {
        if (ReadUInt32(ref file) != signature)
        {
            throw new InvalidDataException("The file is not an SKSE co-save.");
        }
        if (ReadUInt32(ref file) != formatVersion)
        {
            throw new InvalidDataException($"The co-save format version is not {formatVersion}.");
        }
        Read(ref file, sizeof(uint) * 2);
        uint pluginCount = ReadUInt32(ref file);
        for (uint i = 0; i < pluginCount; i++)
        {
            uint id = ReadUInt32(ref file);
            uint recordCount = ReadUInt32(ref file);
            ReadOnlySpan<byte> records = Read(ref file, ReadUInt32(ref file));
            if (id == pluginId)
            {
                for (uint j = 0; j < recordCount; j++)
                {
                    uint type = ReadUInt32(ref records);
                    uint recordVersion = ReadUInt32(ref records);
                    ReadOnlySpan<byte> recordData = Read(ref records, ReadUInt32(ref records));
                    if (type == recordType)
                    {
                        version = recordVersion;
                        data = recordData.ToArray();
                        return true;
                    }
                }
                break;
            }
        }
        version = 0;
        data = [];
        return false;
    }

    private static uint ReadUInt32(ref ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt32LittleEndian(Read(ref bytes, sizeof(uint)));

    private static ReadOnlySpan<byte> Read(ref ReadOnlySpan<byte> bytes, uint length)
    {
        if (length > bytes.Length)
        {
            throw new InvalidDataException("A length in the co-save extends past the end of the file.");
        }
        ReadOnlySpan<byte> read = bytes[..(int)length];
        bytes = bytes[(int)length..];
        return read;
    }
}
