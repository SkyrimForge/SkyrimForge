using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

internal sealed class TriggerZoneFactory : ITriggerZoneFactory
{
    private static readonly TimeSpan checkInterval = TimeSpan.FromSeconds(0.25);

    private readonly IPerPluginSavedState savedState;
    private readonly ILog log;
    private readonly Func<WorldspaceOrCellPosition?> getPlayerPosition;
    private readonly Func<FormId<ObjectReference>, WorldspaceOrCellPosition?> getReferencePosition;
    private readonly List<Zone> zones = [];

    // Frames run at the main menu, before the game may have built the player, and during a load, before the load has moved the player.
    private bool gameLoaded;

    internal TriggerZoneFactory(IGameEvents events, IPerPluginSavedState savedState, ILog log, Func<WorldspaceOrCellPosition?> getPlayerPosition, Func<FormId<ObjectReference>, WorldspaceOrCellPosition?> getReferencePosition)
    {
        this.savedState = savedState;
        this.log = log;
        this.getPlayerPosition = getPlayerPosition;
        this.getReferencePosition = getReferencePosition;
        events.OnFrame.SubscribeEvery(checkInterval, _ => Check());
        events.OnNewGameOrSaveLoading.Subscribe(_ => gameLoaded = false);
        events.OnNewGameOrSaveLoaded.Subscribe(_ => gameLoaded = true);
    }

    internal static TriggerZoneFactory Create(GameTarget gameTarget, IGameEvents events, IPerPluginSavedState savedState, ILog log) =>
        new(events, savedState, log, () => gameTarget.Player.Is3DLoaded() ? GetWorldspaceOrCellPosition(gameTarget.Player) : null, referenceId => referenceId.FormOrNull is { } reference ? GetWorldspaceOrCellPosition(reference) : null);

    /// <inheritdoc/>
    public ITriggerZone Create(string key, Func<FormId<ObjectReference>?> getCenterId, float radius)
    {
        const string keyPrefix = "SkyrimForge.TriggerZone.";
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(getCenterId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        if (zones.Any(zone => zone.Key == key))
        {
            throw new ArgumentException($"This plugin already created a trigger zone with the key \"{key}\".", nameof(key));
        }
        var zone = new Zone(key, getCenterId, radius, savedState.GetPair<bool>(keyPrefix + key), log);
        zones.Add(zone);
        return zone;
    }

    private void Check()
    {
        if (!gameLoaded || zones.Count == 0 || getPlayerPosition() is not { } position)
        {
            return;
        }
        // Copied since a handler can create a zone.
        foreach (Zone zone in zones.ToArray())
        {
            zone.Update(position, getReferencePosition);
        }
    }

    private static WorldspaceOrCellPosition? GetWorldspaceOrCellPosition<TReference>(TReference reference) where TReference : IObjectReference
    {
        FormId<Form> worldspaceOrCellId;
        if (reference.GetWorldspace() is { } worldspace)
        {
            worldspaceOrCellId = worldspace.FormId;
        }
        else if (reference.ParentCell is { } cell)
        {
            worldspaceOrCellId = cell.FormId;
        }
        else
        {
            return null;
        }
        return new WorldspaceOrCellPosition(worldspaceOrCellId, reference.GetPosition());
    }

    private sealed class Zone(string key, Func<FormId<ObjectReference>?> getCenterId, float radius, ISavedStatePair<bool> isPlayerInsidePair, ILog log) : ITriggerZone
    {
        private readonly GameEvent<TriggerZoneEventArgs> onEnter = new(ex => log.Write($"A subscriber to the OnEnter of the trigger zone \"{key}\" threw: {ex}"));
        private readonly GameEvent<TriggerZoneEventArgs> onLeave = new(ex => log.Write($"A subscriber to the OnLeave of the trigger zone \"{key}\" threw: {ex}"));
        private bool centerThrew;

        internal string Key { get; } = key;

        /// <inheritdoc/>
        public bool IsPlayerInside => isPlayerInsidePair.Value == true;

        /// <inheritdoc/>
        public IGameEvent<TriggerZoneEventArgs> OnEnter => onEnter;

        /// <inheritdoc/>
        public IGameEvent<TriggerZoneEventArgs> OnLeave => onLeave;

        internal void Update(WorldspaceOrCellPosition playerPosition, Func<FormId<ObjectReference>, WorldspaceOrCellPosition?> referencePosition)
        {
            if (centerThrew)
            {
                return;
            }
            FormId<ObjectReference>? centerId;
            try
            {
                centerId = getCenterId();
            }
            catch (Exception ex)
            {
                centerThrew = true;
                log.Write($"The center function of the trigger zone \"{Key}\" threw, and the zone is no longer tested: {ex}");
                return;
            }
            bool isInside = centerId is { } id && referencePosition(id) is { } centerPosition && centerPosition.WorldspaceOrCellId == playerPosition.WorldspaceOrCellId && centerPosition.Position.GetDistance(playerPosition.Position) <= radius;
            if (isInside == IsPlayerInside)
            {
                return;
            }
            // Stored as null outside, so a zone the player never entered stores nothing.
            isPlayerInsidePair.Value = isInside ? true : null;
            (isInside ? onEnter : onLeave).Raise(new TriggerZoneEventArgs(centerId));
        }
    }
}
