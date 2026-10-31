using SkyrimForge.Game;
using System;

namespace SkyrimForge;

/// <summary>The game events the framework exposes to plugins.</summary>
/// <remarks>
/// A handler runs on the thread that raises its event, before the code that raised it continues.
/// The game's thread raises <see cref="OnFrame"/>, the SKSE events from <see cref="OnPostLoad"/> to <see cref="OnSaveDeleted"/> other than <see cref="OnDataLoaded"/>,
/// mouse events mostly, and most events of menus, the crosshair, sleeping, waiting, crafting, and loading cells.
/// Most other events arrive on worker threads, or on a cell-loading thread. Handlers on different worker threads run at the same time.
/// In a handler on any of those threads,
/// await <see cref="UtilityExtensions.WaitForGameThreadAsync"/> before changing the game or calling a member documented as called on the game's thread,
/// or queue that work through <see cref="ITaskQueue"/>.
/// </remarks>
public partial interface IGameEvents
{
    /// <summary>Raised once per frame, from the game's startup onward.</summary>
    /// <remarks>
    /// The game is not fully initialized on early frames, and the save's player is in the world only once the player's 3D has loaded.
    /// <c>ObjectReference.Is3DLoaded</c> tests that, and <c>ObjectReference.WaitFor3DLoadAsync</c> waits for it.
    /// </remarks>
    IGameEvent<FrameEventArgs> OnFrame { get; }

    /// <summary>Raised once every SKSE plugin has been loaded. Never raised on Skyrim VR, whose first event is <see cref="OnInputLoaded"/>.</summary>
    /// <remarks><see cref="IPlugin.Start"/> runs while SKSE is still loading plugins, so test here whether another plugin loaded.</remarks>
    IGameEvent<PostLoadEventArgs> OnPostLoad { get; }

    /// <summary>Raised once every SKSE plugin has received <see cref="OnPostLoad"/>. Never raised on Skyrim VR, whose first event is <see cref="OnInputLoaded"/>.</summary>
    IGameEvent<PostPostLoadEventArgs> OnPostPostLoad { get; }

    /// <summary>Raised once the game has loaded its input, before <see cref="OnDataLoaded"/>. On Skyrim VR, this is the first event raised.</summary>
    IGameEvent<InputLoadedEventArgs> OnInputLoaded { get; }

    /// <summary>Raised once the game has loaded its data files.</summary>
    /// <remarks>
    /// A form can first be looked up by editor ID here, and the arrays <c>TESDataHandler</c> exposes are populated only by now.
    /// Raised once per process. A handler subscribed in <see cref="IPlugin.Start"/> receives it.
    /// Menu, input, and SKSE events can arrive before this. The game's events about forms and references, <see cref="OnInitScript"/> among them, arrive only after it.
    /// </remarks>
    IGameEvent<DataLoadedEventArgs> OnDataLoaded { get; }

    /// <summary>Raised once a new character has been made, before the game has loaded them.</summary>
    /// <remarks>A new game reads no save, so the game's own load event does not report it.</remarks>
    IGameEvent<NewGameEventArgs> OnNewGame { get; }

    /// <summary>Raised before the game reads a save file, while views into the current world are still valid.</summary>
    /// <remarks>
    /// Release every view on this event. Loading rebuilds the game's memory, and reading a retained view returns plausible values rather than failing.
    /// A form ID remains valid across the load, so look the form up again afterward.
    /// </remarks>
    IGameEvent<SaveEventArgs> OnSaveLoading { get; }

    /// <summary>Raised as a new game starts or a save starts loading, before <see cref="OnNewGame"/> or <see cref="OnSaveLoading"/>.</summary>
    /// <remarks><see cref="IGameSession.NewGameOrSaveLoading"/> is canceled on this event.</remarks>
    IGameEvent<NewGameOrSaveLoadingEventArgs> OnNewGameOrSaveLoading { get; }

    /// <summary>Raised once the game has finished trying to read a save file.</summary>
    /// <remarks><see cref="OnLoadGame"/>, the game's own event, is raised after this for the same load.</remarks>
    IGameEvent<SaveLoadedEventArgs> OnSaveLoaded { get; }

    /// <summary>Raised as a new game starts, after <see cref="OnNewGame"/>, and once a save finishes loading, after <see cref="OnSaveLoaded"/>.</summary>
    /// <remarks>For a new game it is raised before the game has loaded the character, as <see cref="OnNewGame"/> is.</remarks>
    IGameEvent<NewGameOrSaveLoadedEventArgs> OnNewGameOrSaveLoaded { get; }

    /// <summary>Raised as the game begins writing a save file.</summary>
    /// <remarks>
    /// <see cref="IPerPluginSavedState"/> writes co-save state without this event, which serves state a plugin stores elsewhere.
    /// The save file and the co-save are still being written, so neither can be read from here.
    /// </remarks>
    IGameEvent<SaveEventArgs> OnSaveWriting { get; }

    /// <summary>Raised as a save file is deleted.</summary>
    /// <remarks>The state a plugin stored in that save's co-save is deleted with it.</remarks>
    IGameEvent<SaveEventArgs> OnSaveDeleted { get; }

    /// <summary>
    /// Returns an event raised once as <paramref name="key"/> goes down while the modifier keys <paramref name="modifiers"/> are down,
    /// unless a combination subscribed for <paramref name="key"/> has more modifier keys and all of them are down.
    /// </summary>
    /// <remarks>
    /// That comparison includes the combinations other plugins subscribed.
    /// Raised mostly on a worker thread. In a handler,
    /// await <see cref="UtilityExtensions.WaitForGameThreadAsync"/> before changing the game or calling a member documented as called on the game's thread.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="modifiers"/> includes a value <see cref="ModifierKeys"/> does not define.</exception>
    IGameEvent<InputEventArgs> OnKeyDown(BSKeyboardDevice.Keys.Key key, ModifierKeys modifiers);
}
