using SkyrimForge.Papyrus;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class ScenePlayerControl(GameTarget gameTarget, ISceneOwner owner, SceneReferences references, LinkedRefChanges linkedRefs, ILog log)
{
    private readonly GameTarget gameTarget = gameTarget;
    private readonly ISceneOwner owner = owner;
    private readonly SceneReferences references = references;
    private readonly LinkedRefChanges linkedRefs = linkedRefs;
    private readonly ILog log = log;

    internal void DisableControls(ScenePlayerControls controls) => CallControls(GameScript.DisablePlayerControls, controls).LogFault(log);

    // The game counts no disables, so its EnablePlayerControls enables a control another mod disabled too.
    internal void EnableControls(ScenePlayerControls controls)
    {
        controls &= ~owner.PluginScenes.Aggregate(ScenePlayerControls.None, (disabledControls, scenes) => disabledControls | scenes.DisabledControls);
        if (controls != ScenePlayerControls.None)
        {
            CallControls(GameScript.EnablePlayerControls, controls).LogFault(log);
        }
    }

    // The final argument, 0, leaves the point of view unchanged.
    private static Task CallControls(Func<bool, bool, bool, bool, bool, bool, bool, bool, int, Task> function, ScenePlayerControls controls) =>
        function(controls.HasFlag(ScenePlayerControls.Movement), controls.HasFlag(ScenePlayerControls.Fighting), controls.HasFlag(ScenePlayerControls.CameraSwitch),
            controls.HasFlag(ScenePlayerControls.Looking), controls.HasFlag(ScenePlayerControls.Sneaking), controls.HasFlag(ScenePlayerControls.Menu),
            controls.HasFlag(ScenePlayerControls.Activate), controls.HasFlag(ScenePlayerControls.JournalTabs), 0);

    internal static LinkedRefPackage GetPlayerTravelPackage(ScenePackage action) =>
        action.Speed is PACKAGE_DATA.PreferredSpeed.kRun or PACKAGE_DATA.PreferredSpeed.kJog ? LinkedRefPackage.TravelAtRun : LinkedRefPackage.Travel;

    internal void SetPlayerLink(ScenePlay play, ScenePackage action)
    {
        if (SceneReferences.Player.FormOrNull?.AsActor() is { } playerActor && references.GetDestination(action) is { } destination)
        {
            linkedRefs.SetLink(play, playerActor, GetPlayerTravelPackage(action).KeywordId?.Form, destination);
        }
    }

    // The player performs a package only while AI-driven.
    internal void SetPlayerAIDriven(QuestScene scene, ScenePlay play)
    {
        if (scene.GetActions(play.FirstPhase).Select(action => action.Action).OfType<ScenePackage>().Any(package => references.IsPlayer(package.Actor)))
        {
            play.PlayerDriven = true;
            gameTarget.Player.SetAIDriven(true);
        }
    }

    internal void ReleasePlayer()
    {
        if (!owner.PluginScenes.Any(scenes => scenes.DrivesPlayer))
        {
            gameTarget.Player.SetAIDriven(false);
        }
    }
}
