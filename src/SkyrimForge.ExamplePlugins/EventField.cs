using SkyrimForge.Game;

namespace SkyrimForge.ExamplePlugins;

internal static class EventField
{
    internal static string Describe(Actor? value) => value?.GetDisplayFullName() ?? "none";

    internal static string Describe(ObjectReference? value) => value?.GetDisplayFullName() ?? "none";

    internal static string Describe(Form? value) => value?.GetName() ?? "none";

    internal static string Describe(StoryEventValue value)
    {
        return value.Kind switch
        {
            StoryEventValueKind.Reference => Describe(value.Reference),
            StoryEventValueKind.Form => Describe(value.Form),
            StoryEventValueKind.Location => value.Location?.GetName() ?? "none",
            StoryEventValueKind.Keyword => value.Keyword?.GetName() ?? "none",
            StoryEventValueKind.Number => value.Number.ToString(),
            _ => $"kind {(int)value.Kind}"
        };
    }

    internal static string Describe<T>(T value) => value?.ToString() ?? "none";
}
