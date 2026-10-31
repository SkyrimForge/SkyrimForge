using System;
using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="SubtitleManager"/>.</summary>
public static class SubtitleManagerExtensions
{
    extension(SubtitleManager manager)
    {
        /// <summary>Shows <paramref name="text"/> as a subtitle under <paramref name="speaker"/>.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <remarks>The subtitle remains until RemoveSubtitle removes it, or until one of the speaker's own lines ends, which removes all of the speaker's subtitles.</remarks>
        public void AddSubtitle<TSpeaker>(TSpeaker speaker, string text, float distance = 0f, bool forceDisplay = true) where TSpeaker : IObjectReference
        {
            ArgumentNullException.ThrowIfNull(text);
            // The list stores a copy, so the entry built here is deleted afterward.
            SubtitleInfo entry = SubtitleInfo.New();
            entry.Speaker = ObjectReference.OrNull(speaker.Address);
            entry.Subtitle = text;
            entry.TargetDistance = distance;
            entry.ForceDisplay = forceDisplay;
            BSSpinLock spinLock = manager.Lock;
            spinLock.Lock();
            try
            {
                manager.Subtitles.Add(entry);
            }
            finally
            {
                spinLock.Unlock();
                entry.Delete();
            }
        }

        /// <summary>Removes every subtitle showing <paramref name="text"/> under <paramref name="speaker"/>.</summary>
        /// <returns>The number of entries removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public int RemoveSubtitle<TSpeaker>(TSpeaker speaker, string text, MenuTopicManager menuTopicManager) where TSpeaker : IObjectReference
        {
            ArgumentNullException.ThrowIfNull(text);
            nint speakerAddress = speaker.Address;
            int removedCount = 0;
            bool emptied = false;
            bool wasCurrent = false;
            BSSpinLock spinLock = manager.Lock;
            spinLock.Lock();
            try
            {
                GameList<SubtitleInfo> subtitles = manager.Subtitles;
                IReadOnlyList<SubtitleInfo> entries = subtitles.Get();
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    if (entries[i].Speaker?.Address == speakerAddress && entries[i].Subtitle == text)
                    {
                        subtitles.RemoveAt(i);
                        removedCount++;
                    }
                }
                // A removed line of the current speaker stays drawn and makes the manager skip the speaker's next line, so the current speaker is cleared.
                wasCurrent = removedCount > 0 && manager.CurrentSpeaker?.Address == speakerAddress;
                if (wasCurrent)
                {
                    manager.CurrentSpeaker = null;
                }
                emptied = removedCount > 0 && subtitles.Count == 0;
            }
            finally
            {
                spinLock.Unlock();
            }
            // Removing the last entry leaves its line drawn, and KillSubtitles clears it.
            if (emptied)
            {
                manager.KillSubtitles();
            }
            // KillSubtitles hides only the HUD's copy. The game's own removal sends Hide Text for the Dialogue Menu's copy.
            if (wasCurrent && menuTopicManager.MenuOpen && menuTopicManager.Speaker?.Address == speakerAddress)
            {
                BSUIMessageData.SendUIStringMessage(DialogueMenu.MenuName, UI_MESSAGE_TYPE.kUpdate, "Hide Text");
            }
            return removedCount;
        }
    }
}
