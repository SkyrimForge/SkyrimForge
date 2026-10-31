using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="IPackage"/>.</summary>
public static class PackageExtensions
{
    extension<TPackage>(TPackage package) where TPackage : IPackage
    {
        /// <summary>Gets each value the package sets, under the name its template gives it.</summary>
        /// <returns>The values, or an empty list for a package not built on a template.</returns>
        public IReadOnlyList<PackageDataValue> GetDataValues()
        {
            nint address = package.Address;
            return [.. Enumerable.Range(0, NativeSupplements.SF_Package_DataCount(address)).Select(i => new PackageDataValue(
                Sequences.ReadStringOrEmpty((ref byte first, int capacity) => NativeSupplements.SF_Package_DataName(address, i, ref first, capacity)) ?? "",
                Sequences.ReadStringOrEmpty((ref byte first, int capacity) => NativeSupplements.SF_Package_DataTypeName(address, i, ref first, capacity)) ?? "",
                Sequences.ReadStringOrEmpty((ref byte first, int capacity) => NativeSupplements.SF_Package_DataValue(address, i, ref first, capacity)) ?? ""))];
        }
    }
}

internal static partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial int SF_Package_DataCount(nint package);

    [LibraryImport(library)]
    internal static partial int SF_Package_DataName(nint package, int index, ref byte buffer, int capacity);

    [LibraryImport(library)]
    internal static partial int SF_Package_DataTypeName(nint package, int index, ref byte buffer, int capacity);

    [LibraryImport(library)]
    internal static partial int SF_Package_DataValue(nint package, int index, ref byte buffer, int capacity);
}
