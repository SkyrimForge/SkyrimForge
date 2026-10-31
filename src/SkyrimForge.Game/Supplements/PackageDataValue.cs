namespace SkyrimForge.Game;

/// <summary>One value a package built on a template sets.</summary>
/// <param name="Name">The name the template declares for the value, such as Location or Allow Sitting. Empty if the template declares none.</param>
/// <param name="TypeName">The game's name for the value's type, such as Bool or Location.</param>
/// <param name="Value">The text IPackageData::GetDataAsString returns for the value.</param>
public readonly record struct PackageDataValue(string Name, string TypeName, string Value);
