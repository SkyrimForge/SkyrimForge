using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace SkyrimForge.Infrastructure;

internal sealed record CoSaveRecord(Dictionary<string, CoSaveEntries>? EntriesByPlugin, Dictionary<string, Dictionary<uint, CoSaveEntries>>? EntriesByPluginAndForm)
{
    internal byte[] Encode() => Compress(JsonSerializer.SerializeToUtf8Bytes(this));

    internal static CoSaveRecord? Decode(ReadOnlySpan<byte> record) => JsonSerializer.Deserialize<CoSaveRecord>(Decompress(record));

    internal static byte[] Compress(ReadOnlySpan<byte> json)
    {
        using (var compressedStream = new MemoryStream())
        {
            // The encoder writes the final block on disposal.
            using (var brotli = new BrotliStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                brotli.Write(json);
            }
            return compressedStream.ToArray();
        }
    }

    private static byte[] Decompress(ReadOnlySpan<byte> record)
    {
        using (var decompressedStream = new MemoryStream())
        {
            using (var brotli = new BrotliStream(new MemoryStream(record.ToArray()), CompressionMode.Decompress))
            {
                brotli.CopyTo(decompressedStream);
            }
            return decompressedStream.ToArray();
        }
    }
}
