using System;

namespace SkyrimForge.Game;

internal sealed class QuestReferenceAliasEvents(IGameEvents events, Func<FormId<ObjectReference>?> reference) : IQuestReferenceAliasEvents
{
    public IGameEvent<ActivateEventArgs> OnActivate { get; } =
        new FilteredGameEvent<ActivateEventArgs>(events.OnActivate, args => IsFilledWith(reference, args.ObjectActivated));

    public IGameEvent<ActivateEventArgs> OnPlayerActivate { get; } =
        new FilteredGameEvent<ActivateEventArgs>(events.OnPlayerActivate, args => IsFilledWith(reference, args.ObjectActivated));

    public IGameEvent<ObjectLoadedEventArgs> OnLoad { get; } =
        new FilteredGameEvent<ObjectLoadedEventArgs>(events.OnObjectLoaded, args => args.Loaded && args.FormID is { } loadedId && IsFilledWith(reference, loadedId.Value));

    public IGameEvent<ObjectLoadedEventArgs> OnUnload { get; } =
        new FilteredGameEvent<ObjectLoadedEventArgs>(events.OnObjectLoaded, args => !args.Loaded && args.FormID is { } unloadedId && IsFilledWith(reference, unloadedId.Value));

    public IGameEvent<DeathEventArgs> OnDying { get; } =
        new FilteredGameEvent<DeathEventArgs>(events.OnDeath, args => !args.Dead && IsFilledWith(reference, args.ActorDying));

    public IGameEvent<DeathEventArgs> OnDeath { get; } = new FilteredGameEvent<DeathEventArgs>(events.OnDeath, args => args.Dead && IsFilledWith(reference, args.ActorDying));

    public IGameEvent<HitEventArgs> OnHit { get; } = new FilteredGameEvent<HitEventArgs>(events.OnHit, args => IsFilledWith(reference, args.Target));

    public IGameEvent<CombatEventArgs> OnCombatStateChanged { get; } = new FilteredGameEvent<CombatEventArgs>(events.OnCombat, args => IsFilledWith(reference, args.Actor));

    public IGameEvent<CellAttachDetachEventArgs> OnCellAttach { get; } =
        new FilteredGameEvent<CellAttachDetachEventArgs>(events.OnCellAttachDetach, args => args.Attached && IsFilledWith(reference, args.Reference));

    public IGameEvent<CellAttachDetachEventArgs> OnCellDetach { get; } =
        new FilteredGameEvent<CellAttachDetachEventArgs>(events.OnCellAttachDetach, args => !args.Attached && IsFilledWith(reference, args.Reference));

    public IGameEvent<ContainerChangedEventArgs> OnContainerChanged { get; } =
        new FilteredGameEvent<ContainerChangedEventArgs>(events.OnContainerChanged, args => IsFilledWith(reference, args.Reference));

    private static bool IsFilledWith<TForm>(Func<FormId<ObjectReference>?> reference, TForm? eventForm) where TForm : struct, IForm =>
        eventForm is { } form && IsFilledWith(reference, form.FormID);

    private static bool IsFilledWith(Func<FormId<ObjectReference>?> reference, uint formId) => reference() is { } referenceId && referenceId.Value == formId;
}
