using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class ScenePlay(SceneRecord sceneRecord, int firstPhase)
{
    internal SceneRecord SceneRecord { get; } = sceneRecord;

    internal Scene Scene => SceneRecord.Scene;

    internal int FirstPhase { get; } = firstPhase;

    internal Task? StartTask { get; set; }

    internal bool IsBound { get; set; }

    internal bool HasPlayed { get; set; }

    // Equals the scene's phase count once its last phase finishes.
    internal int ReachedPhase { get; set; } = firstPhase;

    internal int? LastPassedPhase { get; set; }

    internal int? AnnouncedPhase { get; set; }

    internal TimeSpan PhaseBeginTime { get; set; }

    internal QuestSceneEndReason? EndReason { get; set; }

    // Also set for a scene played again after a load, so it enables the controls as it ends.
    internal bool ControlsDisabled { get; set; }

    internal bool PlayerDriven { get; set; }

    internal bool IsForced { get; init; }

    internal HashSet<SceneRecordAction> RunningActions { get; } = [];

    internal HashSet<FormId<Actor>> SilencedRemarkers { get; } = [];

    internal TimeSpan NextActionReadTime { get; set; }

    internal int? ConversationPhase { get; set; }

    // Recorded with the phase, since two conversations over one alias and one branch compare equal and a scene can declare one in each of two phases.
    internal HashSet<(int Phase, SceneConversation Conversation)> EndedConversations { get; } = [];

    internal HashSet<SceneConversation> OpenedConversations { get; } = [];

    // The scenes the actors played in as this one started. A scene of the game's own can play on throughout with one of them.
    internal HashSet<nint> ScenesAtStart { get; set; } = [];
}
