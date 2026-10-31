using System.Collections.Generic;
using System.Threading;

namespace SkyrimForge.Game;

// With General Subtitles off, the HUD shows neither a scene's line nor a subtitle added to the subtitle manager, regardless of Dialogue Subtitles and kForceSubtitle.
// The game writes its settings to SkyrimPrefs.ini as it exits, so General Subtitles are switched off while the Journal is open.
internal sealed class SubtitleSwitch
{
    private readonly GameTarget gameTarget;
    private readonly IUtility utility;
    private readonly Lock stateLock = new();

    // Each line is a topic info's form ID or the array of lines UnvoicedSpeech says. Guarded by stateLock.
    private readonly HashSet<object> generalLines = [];

    private Setting? dialogueSubtitles;
    private Setting? generalSubtitles;

    private volatile bool dialogueSwitchedOn;

    // Guarded by stateLock.
    private bool generalSwitchedOn;
    private bool journalOpen;

    public SubtitleSwitch(GameTarget gameTarget, IGameEvents events, IUtility utility)
    {
        this.gameTarget = gameTarget;
        this.utility = utility;
        events.OnFrame.Subscribe(_ =>
        {
            if (dialogueSwitchedOn && gameTarget.MenuTopicManager is not { MenuOpen: true })
            {
                RestoreDialogueSubtitles();
            }
        });
        // Topic events arrive on worker threads and menu events can, so both handlers run on the game's thread.
        events.OnTopicInfo.Subscribe(async args =>
        {
            if (args.Type == TESTopicInfoEvent.TopicInfoEventType.kTopicEnd && args.TopicInfoFormID is { } topicInfoId)
            {
                FormId<TopicInfo> endedId = (FormId<TopicInfo>)topicInfoId;
                await utility.WaitForGameThreadAsync();
                EndLine(endedId);
            }
        });
        events.OnMenuOpenClose.Subscribe(async args =>
        {
            if (args.MenuName == JournalMenu.MenuName)
            {
                bool opening = args.Opening;
                await utility.WaitForGameThreadAsync();
                lock (stateLock)
                {
                    journalOpen = opening;
                    if (journalOpen)
                    {
                        RestoreGeneralSubtitles();
                    }
                    else if (generalLines.Count > 0)
                    {
                        EnableGeneralSubtitles();
                    }
                }
            }
        });
        events.OnNewGameOrSaveLoading.Subscribe(_ =>
        {
            RestoreDialogueSubtitles();
            lock (stateLock)
            {
                generalLines.Clear();
                RestoreGeneralSubtitles();
            }
        });
    }

    internal void FindSettings()
    {
        dialogueSubtitles ??= gameTarget.INIPrefSettingCollection.GetSetting("bDialogueSubtitles:Interface");
        generalSubtitles ??= gameTarget.INIPrefSettingCollection.GetSetting("bGeneralSubtitles:Interface");
    }

    // Called on any thread, before the game shows the response. Each interleaving with RestoreDialogueSubtitles ends with the setting as the player had it.
    internal void EnableDialogueSubtitles()
    {
        if (dialogueSubtitles is { } setting && !setting.GetBool())
        {
            setting.SetBool(true);
            dialogueSwitchedOn = true;
        }
    }

    // Called after FindSettings.
    internal void BeginLine(object line)
    {
        lock (stateLock)
        {
            generalLines.Add(line);
            if (!journalOpen)
            {
                EnableGeneralSubtitles();
            }
        }
    }

    internal void EndLine(object line)
    {
        lock (stateLock)
        {
            if (generalLines.Remove(line) && generalLines.Count == 0)
            {
                RestoreGeneralSubtitles();
            }
        }
    }

    private void RestoreDialogueSubtitles()
    {
        if (dialogueSwitchedOn)
        {
            dialogueSwitchedOn = false;
            dialogueSubtitles?.SetBool(false);
        }
    }

    // Called with stateLock held.
    private void EnableGeneralSubtitles()
    {
        if (generalSubtitles is { } setting && !setting.GetBool())
        {
            setting.SetBool(true);
            generalSwitchedOn = true;
        }
    }

    // Called with stateLock held.
    private void RestoreGeneralSubtitles()
    {
        if (generalSwitchedOn)
        {
            generalSwitchedOn = false;
            generalSubtitles?.SetBool(false);
        }
    }
}
