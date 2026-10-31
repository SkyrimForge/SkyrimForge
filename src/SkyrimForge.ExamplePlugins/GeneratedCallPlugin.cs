using SkyrimForge.Game;
using System;
using System.Diagnostics;

namespace SkyrimForge.ExamplePlugins;

/// <summary>
/// Calls the game's own functions on the player through the generated bindings, and logs the results once a second.
/// </summary>
/// <remarks>
/// Each call shows a different binding shape, among them:
/// <list type="bullet">
/// <item><c>GetDisplayFullName</c>: a <c>const char*</c> from <c>TESObjectREFR</c>, returned as a managed string.</item>
/// <item><c>GetEmotionType</c>: a mirrored enum.</item>
/// <item><c>ActorBase.GetHeight</c>: a <c>TESNPC</c> member on the <see cref="ActorBase"/> view.</item>
/// <item><c>AsActorValueOwner().GetActorValue</c> and <c>AsActorState().GetLifeState</c>: members of base classes an Actor inherits from, reached through accessors.</item>
/// </list>
/// </remarks>
public sealed class GeneratedCallPlugin(GameTarget gameTarget, IGameEvents events, ILog log) : IPlugin
{
    private static readonly TimeSpan logInterval = TimeSpan.FromSeconds(1);

    private long? lastLogTicks;

    /// <inheritdoc/>
    public void Start()
    {
        events.OnFrame.Subscribe(Read);
        log.Write("Logging the player's state once a second.");
    }

    private void Read(FrameEventArgs args)
    {
        long nowTicks = Stopwatch.GetTimestamp();
        if (lastLogTicks is { } lastTicks && Stopwatch.GetElapsedTime(lastTicks, nowTicks) < logInterval)
        {
            return;
        }
        lastLogTicks = nowTicks;

        PlayerCharacter player = gameTarget.Player;
        ushort level = player.GetLevel();
        string name = player.GetDisplayFullName() ?? "unknown";
        FormType formType = player.GetFormType();
        string baseForm = player.GetActorBase() is { } actorBase ? $"0x{actorBase.FormId} height {actorBase.GetHeight():F2}" : "none";
        NiPoint3 boundMax = player.GetBoundMax();
        EmotionType emotion = player.GetEmotionType();
        ActorValueOwner actorValueOwner = player.AsActorValueOwner();
        string health = $"{actorValueOwner.GetActorValue(ActorValue.kHealth):F0}/{actorValueOwner.GetPermanentActorValue(ActorValue.kHealth):F0}";
        string lifeState = player.AsActorState().GetLifeState().ToString();
        // Read as a Form, so the checked casts below have a type to test.
        FormId<Form> goldId = KnownForms.Gold001;
        Form goldForm = goldId.Form;
        string gold = $"{goldForm.GetName()} (bound object {goldForm.AsTESBoundObject() != null}, actor {goldForm.AsActor() != null})";
        // The game retains editor IDs for keywords and discards most others.
        bool isNpc = player.HasKeywordString("ActorTypeNPC");
        string byName = Form.LookupByEditorID("Gold001") is { } goldByName ? $"0x{goldByName.FormId}" : "not stored by the game";
        player.GetLinearVelocity(out NiPoint3 velocity);
        player.GetEyeVector(out NiPoint3 _, out NiPoint3 lookDirection, false);
        string mountText = player.GetMount(out Actor? mount) ? $"riding {mount?.GetDisplayFullName() ?? "unnamed"}" : "on foot";
        string resolutionText = ObjectReference.LookupByHandle(player.GetHandle()) is { } resolvedReference ? $"0x{resolvedReference.FormId}" : "did not resolve";
        player.GetAnimationGraphManagerImpl(out BSAnimationGraphManager? graphManager);
        string graph = graphManager != null ? "present" : "none";

        string[] readings =
        [
            $"level {level}",
            $"name {name}",
            $"form type {formType}",
            $"base form {baseForm}",
            $"bound max ({boundMax.X:F1}, {boundMax.Y:F1}, {boundMax.Z:F1})",
            $"emotion {emotion}",
            $"health {health}",
            $"life state {lifeState}",
            $"velocity ({velocity.X:F0}, {velocity.Y:F0}, {velocity.Z:F0})",
            $"looking ({lookDirection.X:F2}, {lookDirection.Y:F2}, {lookDirection.Z:F2})",
            mountText,
            $"anim graph {graph}",
            $"own handle {resolutionText}",
            $"looked up {gold}",
            $"by editor ID {byName}",
            $"ActorTypeNPC keyword {isNpc}"
        ];
        log.Write($"Player: {string.Join(", ", readings)}.");
    }
}
