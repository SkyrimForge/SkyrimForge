using SkyrimForge.Papyrus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

internal sealed class WorldMapMarkers : IWorldMapMarkers
{
    private static readonly FormId<Static> mapMarker = new(0x10);

    internal const string SavedStateKey = "SkyrimForge.WorldMapMarkers";

    // The size CommonLib asserts for MapMarkerData.
    private const int mapMarkerDataSize = 0x18;

    private readonly GameTarget gameTarget;
    private readonly ILog log;

    private readonly ISavedStateRecordPair<Dictionary<string, SavedMapMarker>> markersPair;

    public WorldMapMarkers(GameTarget gameTarget, IGameEvents events, IPerPluginSavedState savedState, ILog log)
    {
        this.gameTarget = gameTarget;
        this.log = log;
        markersPair = savedState.GetRecordPair<Dictionary<string, SavedMapMarker>>(SavedStateKey);
        events.OnSaveLoaded.Subscribe(_ => Restore());
    }

    public ObjectReference Add(string key, FormId<WorldSpace> worldspaceId, NiPoint3 position, string name, MARKER_TYPE kind)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(name);
        WorldSpace worldspace = worldspaceId.FormOrNull ?? throw new ArgumentException($"No worldspace has the ID {worldspaceId}.", nameof(worldspaceId));
        Dictionary<string, SavedMapMarker> markers = Read();
        if (markers.ContainsKey(key))
        {
            throw new ArgumentException($"This plugin already added a map marker with the key \"{key}\".", nameof(key));
        }
        // With no target cell, the reference belongs to the worldspace's cell containing the position, wherever the player is.
        ObjectReference reference = gameTarget.DataHandler.CreateReferenceAtLocation(mapMarker.Form, position, new NiPoint3(0f, 0f, 0f), null, worldspace, null, null, null, true, true)
            ?? throw new InvalidOperationException($"The game created no map marker in {worldspaceId}.");
        // Bound as Papyrus's PlaceAtMe binds the reference it returns, which a Papyrus call on it requires.
        ObjectReferenceScript.Dispatcher.Bind(reference);
        var marker = new SavedMapMarker(reference.FormId, name, kind);
        try
        {
            markers.Add(key, marker);
            if (!Set(marker))
            {
                throw new InvalidOperationException($"The map marker \"{key}\" was not set on {reference.FormId}.");
            }
            markersPair.Value = markers;
        }
        catch
        {
            reference.DisableAndDelete();
            throw;
        }
        return reference;
    }

    public FormId<ObjectReference>? GetReferenceId(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Read().TryGetValue(key, out SavedMapMarker? marker) ? marker.ReferenceId : null;
    }

    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        Dictionary<string, SavedMapMarker> markers = Read();
        if (!markers.Remove(key, out SavedMapMarker? marker))
        {
            return;
        }
        markersPair.Value = markers;
        if (marker.ReferenceId.FormOrNull is { } reference)
        {
            Unset(reference);
            reference.DisableAndDelete();
        }
    }


    // A save records neither the extra data of a map marker nor the player's listing of it.
    private void Restore()
    {
        foreach ((string key, SavedMapMarker marker) in Read())
        {
            try
            {
                if (!Set(marker))
                {
                    log.Write($"The map marker \"{key}\" was not restored: no reference has the ID {marker.ReferenceId}.");
                }
            }
            catch (InvalidOperationException ex)
            {
                log.Write($"The map marker \"{key}\" was not restored: {ex.Message}");
            }
        }
    }

    private bool Set(SavedMapMarker marker)
    {
        // The size CommonLib asserts for ExtraMapMarker.
        const ulong extraMapMarkerSize = 0x18;
        if (marker.ReferenceId.FormOrNull is not { } reference)
        {
            return false;
        }
        ExtraDataList extraList = reference.ExtraList;
        if (extraList.GetByType(ExtraDataType.kMapMarker)?.AsExtraMapMarker() is not { } extraMapMarker)
        {
            extraMapMarker = BSExtraData.Create(extraMapMarkerSize, (ulong)GameHeap.GetVTable("ExtraMapMarker"))?.AsExtraMapMarker()
                ?? throw new InvalidOperationException("The game created no map marker extra data.");
            extraMapMarker.MapData = CreateData();
            extraList.Add(extraMapMarker);
        }
        MapMarkerData data = extraMapMarker.MapData ?? throw new InvalidOperationException($"The map marker extra data of {reference.FormId} has no data.");
        data.LocationName.SetFullName(marker.Name);
        data.Flags = MapMarkerData.Flag.kVisible | MapMarkerData.Flag.kCanTravelTo;
        data.Type = marker.Icon;
        GameList<ObjectReference> currentMarkers = gameTarget.Player.PlayerRuntimeData.CurrentMapMarkers;
        if (!currentMarkers.Get().Any(listedMarker => listedMarker.FormId == reference.FormId))
        {
            currentMarkers.Add(reference);
        }
        return true;
    }

    // The extra data's destructor frees the MapMarkerData.
    private void Unset(ObjectReference reference)
    {
        GameList<ObjectReference> currentMarkers = gameTarget.Player.PlayerRuntimeData.CurrentMapMarkers;
        int index = currentMarkers.Get().Select(listedMarker => listedMarker.FormId).ToList().IndexOf(reference.FormId);
        if (index >= 0)
        {
            currentMarkers.RemoveAt(index);
        }
        reference.ExtraList.RemoveByType(ExtraDataType.kMapMarker);
    }

    // CommonLib declares MapMarkerData without a constructor. Its first member is a TESFullName, whose first word is that type's virtual table.
    private MapMarkerData CreateData() => MapMarkerData.OrNull(GameHeap.AllocateObject(gameTarget, mapMarkerDataSize, "TESFullName"))!.Value;

    private Dictionary<string, SavedMapMarker> Read() => markersPair.Value ?? [];

    private sealed record SavedMapMarker(FormId<ObjectReference> ReferenceId, string Name, MARKER_TYPE Icon);
}
