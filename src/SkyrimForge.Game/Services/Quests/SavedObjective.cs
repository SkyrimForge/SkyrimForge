namespace SkyrimForge.Game;

internal sealed record SavedObjective(ushort Index, string Text, bool Completed, bool Failed)
{
    internal bool IsOpen => !Completed && !Failed;

    internal QUEST_OBJECTIVE_STATE State => this switch
    {
        { Failed: true } => QUEST_OBJECTIVE_STATE.kFailedDisplayed,
        { Completed: true } => QUEST_OBJECTIVE_STATE.kCompletedDisplayed,
        _ => QUEST_OBJECTIVE_STATE.kDisplayed
    };
}
