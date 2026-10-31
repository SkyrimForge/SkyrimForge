using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class BookMenuPresenter(GameTarget gameTarget, IGameEvents events, ITaskQueue taskQueue) : IBookMenuPresenter
{
    private readonly GameTarget gameTarget = gameTarget;
    private readonly IGameEvents events = events;
    private readonly ITaskQueue taskQueue = taskQueue;

    public Task ShowAsync(IEnumerable<string> htmlPages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(htmlPages);
        return Core();

        async Task Core()
        {
            cancellationToken.ThrowIfCancellationRequested();
            // A book menu requested while one is open never opens, so the task would never complete.
            if (gameTarget.UI.IsMenuOpen(BookMenu.MenuName))
            {
                throw new InvalidOperationException("A book menu is already open. The game will not open another until it closes.");
            }
            string text = JoinPages(htmlPages);
            Book note = KnownForms.dunLabyrinthianMazeClue.Form;
            var closedSource = new GameTaskCompletionSource<bool>(taskQueue);
            // Set when this menu opens, so an earlier book menu closing does not complete the task.
            bool opened = false;
            ISubscription subscription = events.OnMenuOpenClose.Subscribe(args =>
            {
                if (args.MenuName == BookMenu.MenuName)
                {
                    if (args.Opening)
                    {
                        opened = true;
                    }
                    else if (opened)
                    {
                        closedSource.SetResult(true);
                    }
                }
            });
            try
            {
                using (cancellationToken.Register(closedSource.SetCanceled))
                {
                    NiMatrix3 rotation = NiMatrix3.New();
                    BookMenu.OpenBookMenu(text, null, null, note, default, rotation, 1f, true);
                    rotation.Delete();
                    await closedSource.Task;
                }
            }
            finally
            {
                subscription.Unsubscribe();
            }
        }
    }

    // The book menu recognizes [pagebreak] only on a line of its own.
    internal static string JoinPages(IEnumerable<string> htmlPages) => string.Join("\n[pagebreak]\n", htmlPages);
}
