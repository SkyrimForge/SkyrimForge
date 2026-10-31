using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using unsafe OpenBookMenuFunction = delegate* unmanaged<nint, SkyrimForge.Game.ExtraDataList, SkyrimForge.Game.ObjectReference, SkyrimForge.Game.Book, nint, nint, float, byte, nint, void>;

namespace SkyrimForge.Game;

internal sealed class ReadableNotes : IReadableNotes
{
    private const string pagesKey = "SkyrimForge.ReadableNote.Pages";
    private const string canBeTakenKey = "SkyrimForge.ReadableNote.CanBeTaken";

    private static ReadableNotes[] instances = [];
    private static unsafe OpenBookMenuFunction originalOpenBookMenu;

    private readonly IBookMenuPresenter bookMenu;
    private readonly IPerPluginSavedState savedState;
    private readonly IGameSession session;
    private readonly IUtility utility;
    private readonly ICourier courier;
    private readonly ILog log;
    private readonly GameEvent<NoteReadEventArgs> onRead;

    public ReadableNotes(IGameEvents events, IBookMenuPresenter bookMenu, IPerPluginSavedState savedState, IGameSession session, IUtility utility, IFrameworkFunctionHooks functionHooks, ICourier courier, ILog log)
    {
        this.bookMenu = bookMenu;
        this.savedState = savedState;
        this.session = session;
        this.utility = utility;
        this.courier = courier;
        this.log = log;
        onRead = new GameEvent<NoteReadEventArgs>(ex => log.Write($"A subscriber to {nameof(IReadableNotes)}.{nameof(OnRead)} threw and was removed: {ex}"));
        events.OnPlayerActivate.Subscribe(OnActivateAsync);

        // Detoured at data load, since the tests construct a plugin's services with no native runtime.
        events.OnDataLoaded.Subscribe(_ =>
        {
            DetourOpenBookMenu(functionHooks);
            instances = [.. instances, this];
        });
    }

    public IGameEvent<NoteReadEventArgs> OnRead => onRead;

    public void Write<TNote>(TNote note, string name, IEnumerable<string> htmlPages, bool canBeTaken = false) where TNote : struct, IObjectReference
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(htmlPages);
        ISavedStateEntries entries = savedState.GetFormEntries(note.FormId);
        entries.SetString(pagesKey, JsonSerializer.Serialize<string[]>([.. htmlPages]));
        entries.GetPair<bool>(canBeTakenKey).Value = canBeTaken ? true : null;
        note.BlockActivation(!canBeTaken);
        note.SetDisplayName(name, true);
    }

    public Task<ObjectReference> SendByCourierAsync(string name, IEnumerable<string> htmlPages)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(htmlPages);
        return Core();

        async Task<ObjectReference> Core()
        {
            ObjectReference letter = KnownForms.WICourierContainerRef.Form.PlaceObjectAtMe(KnownForms.dunLabyrinthianMazeClue.Form, true) ?? throw new InvalidOperationException("The game created no letter.");
            Write(letter, name, htmlPages, true);
            await courier.SendAsync(letter);
            return letter;
        }
    }

    private string[]? ReadPages(FormId<ObjectReference> noteId, bool canBeTaken)
    {
        ISavedStateEntries entries = savedState.GetFormEntries(noteId);
        return (entries.GetPair<bool>(canBeTakenKey).Value == true) == canBeTaken && entries.GetString(pagesKey) is { } storedPages ? JsonSerializer.Deserialize<string[]>(storedPages) : null;
    }

    // The game opens no menu on a note that cannot be taken, because its activation is blocked.
    private async Task OnActivateAsync(ActivateEventArgs args)
    {
        await utility.WaitForGameThreadAsync();
        if (args.ObjectActivated is { } note && ReadPages(note.FormId, false) is { } pages)
        {
            ShowAsync(pages).LogFault(log);
            onRead.Raise(new NoteReadEventArgs(note));
        }
    }

    private async Task RaiseReadAsync(ObjectReference note)
    {
        await utility.WaitForGameThreadAsync();
        onRead.Raise(new NoteReadEventArgs(note));
    }

    private static unsafe void DetourOpenBookMenu(IFunctionHooks functionHooks)
    {
        if (originalOpenBookMenu == null)
        {
            OpenBookMenuFunction replacement = &OnOpenBookMenu;
            originalOpenBookMenu = (OpenBookMenuFunction)functionHooks.DetourFunction(GameFunction.BookMenu__OpenMenu_Impl, (nint)replacement);
        }
    }

    // BookMenu::OpenBookMenu declares a ninth parameter on Skyrim VR, the 3D object beside which the game places the book, which CommonLib's declaration omits.
    // On the other editions the ninth is the contents of the stack, which the game's function does not read.
    [UnmanagedCallersOnly]
    private static unsafe void OnOpenBookMenu(nint description, ExtraDataList extraList, ObjectReference reference, Book book, nint position, nint rotation, float scale, byte useDefaultPosition, nint placement)
    {
        ObjectReference? note = reference.Exists ? reference
            : extraList.Exists ? extraList.GetByType(ExtraDataType.kReferenceHandle)?.AsExtraReferenceHandle()?.ContainerRef : null;
        if (note != null
            && instances.Select(notes => (Notes: notes, Pages: notes.ReadPages(note.Value.FormId, true))).FirstOrDefault(candidate => candidate.Pages != null) is { Pages: { } pages } match)
        {
            fixed (byte* text = Encoding.UTF8.GetBytes(BookMenuPresenter.JoinPages(pages) + '\0'))
            {
                // A length of 0xFFFF makes the game measure the text, as it does for a string too long to count in 16 bits.
                GameString replacement = new() { Data = (nint)text, Length = ushort.MaxValue, Capacity = ushort.MaxValue };
                originalOpenBookMenu((nint)(&replacement), extraList, reference, book, position, rotation, scale, useDefaultPosition, placement);
            }
            match.Notes.RaiseReadAsync(note.Value).LogFault(match.Notes.log);
        }
        else
        {
            originalOpenBookMenu(description, extraList, reference, book, position, rotation, scale, useDefaultPosition, placement);
        }
    }

    // The game's BSString, as CommonLib declares it.
    [StructLayout(LayoutKind.Sequential)]
    private struct GameString
    {
        internal nint Data;
        internal ushort Length, Capacity;
        internal uint Padding;
    }

    // Awaited here, not in OnActivateAsync, so a book menu that fails to open is logged without faulting the activation handler, which would unsubscribe it.
    private async Task ShowAsync(string[] pages)
    {
        try
        {
            await bookMenu.ShowAsync(pages, session.NewGameOrSaveLoading);
        }
        catch (InvalidOperationException ex)
        {
            log.Write($"{nameof(IReadableNotes)} opened no book menu: {ex.Message}");
        }
    }
}
