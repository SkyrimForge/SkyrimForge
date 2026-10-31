namespace SkyrimForge.Game;

// Phase is the scene's phase whose actions include the action. StartPhase and EndPhase are the scene's phases the record starts and ends it in.
internal sealed record SceneRecordAction(SceneAction Action, int Phase, int StartPhase, int EndPhase, uint Id);
