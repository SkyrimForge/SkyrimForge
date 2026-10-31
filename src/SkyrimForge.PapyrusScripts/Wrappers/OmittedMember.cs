namespace SkyrimForge.PapyrusScripts;

internal sealed record OmittedMember(string Script, string Member, string Reason, int Line)
{
    public override string ToString() => $"{Script}.{Member}: {Reason}";
}
