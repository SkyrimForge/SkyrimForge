namespace SkyrimForge.Game;

internal static class TravelPackages
{
    internal static Package? Create(PACKAGE_DATA.PreferredSpeed? speed)
    {
        Package? package = Package.CreatePackage(PACKAGE_TYPE.kTravel);
        // The flag is the Creation Kit's Preferred Speed box, without which the package ignores the speed.
        if (speed is { } preferredSpeed && package?.PackData is { } packData)
        {
            packData.MaxSpeed = preferredSpeed;
            packData.PackFlags |= PACKAGE_DATA.GeneralFlag.kPreferredSpeed;
        }
        return package?.PackLoc != null ? package : null;
    }

    internal static void SetLocation(Package package, ObjectReference nearReference, uint radius)
    {
        PackageLocation location = package.PackLoc!.Value;
        location.LocType = PackageLocation.Type.kNearReference;
        PackageLocation.Data locationData = location.GetData();
        locationData.RefHandle = nearReference;
        location.Rad = radius;
    }
}
