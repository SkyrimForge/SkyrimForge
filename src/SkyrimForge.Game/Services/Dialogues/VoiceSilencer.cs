using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

// The game shows a response's text as a subtitle until its voice ends, and a created topic info otherwise plays the responses of the record it reuses.
internal sealed unsafe class VoiceSilencer(GameTarget gameTarget, IFrameworkFunctionHooks functionHooks, IGameEvents events, SubtitleSwitch subtitleSwitch, UnvoicedLipSync unvoicedLipSync, ILog log)
{
    private static VoiceSilencer? instance;
    private static nint originalUpdateInDialogue;
    private static delegate* unmanaged<nint, nint, nint, nint, nint, nint> originalDialogueItemConstructor;

    private readonly GameTarget gameTarget = gameTarget;
    private readonly IFunctionHooks functionHooks = functionHooks;
    private readonly IGameEvents events = events;
    private readonly SubtitleSwitch subtitleSwitch = subtitleSwitch;
    private readonly UnvoicedLipSync unvoicedLipSync = unvoicedLipSync;
    private readonly ILog log = log;

    // A created form is never freed, so its address outlives a load. The game enters the replacements, which read this, from any thread.
    private readonly ConcurrentDictionary<nint, WrittenResponse[]> topicInfoAddressesToResponses = new();

    // Only the game's thread writes a set, and the replacements read only the keys.
    // Cleared on a load or a new game, since a form ID the game assigns at runtime can identify another reference after a load.
    private readonly ConcurrentDictionary<FormId<Actor>, HashSet<object>> silencedGreeters = new();

    // Remarks are idle chatter and corpse notices.
    private readonly ConcurrentDictionary<FormId<Actor>, HashSet<object>> silencedRemarkers = new();

    private readonly ConcurrentDictionary<FormId<Actor>, OwnedResponses> goodbyes = new();

    private readonly ConcurrentDictionary<FormId<Actor>, OwnedResponses> greetings = new();

    internal void Install()
    {
        // From CommonLib's Actor.h. The game calls this function once for each response an actor begins to say.
        const int updateInDialogueVTableIndex = 0x4C;
        events.OnNewGameOrSaveLoading.Subscribe(_ => ClearGreeters());
        unvoicedLipSync.Install();
        try
        {
            SilentVoices.Write();
            instance = this;
            delegate* unmanaged[Thiscall]<Actor, DialogueResponse, byte, byte> replacement = &OnUpdateInDialogue;
            originalUpdateInDialogue = functionHooks.ReplaceVirtualTableEntry("Character", updateInDialogueVTableIndex, (nint)replacement);
            delegate* unmanaged<nint, nint, nint, nint, nint, nint> constructorReplacement = &OnDialogueItemConstructed;
            originalDialogueItemConstructor = (delegate* unmanaged<nint, nint, nint, nint, nint, nint>)functionHooks.DetourFunction(GameFunction.DialogueItem__Ctor,
                (nint)constructorReplacement);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            log.Write($"{nameof(VoiceSilencer)}.{nameof(Install)} failed: {ex.Message}");
        }
    }

    // A reply with no responses is silenced only on the default record, whose own voice is audible.
    internal void UpdateResponses(TopicInfo info, IReadOnlyList<ReplyResponse> responses, bool isDefaultRecord)
    {
        subtitleSwitch.FindSettings();
        WrittenResponse[] writtenResponses = responses.Count > 0 ? ToWrittenResponses(responses) : isDefaultRecord ? [new WrittenResponse("", SilentVoices.BriefSilencePath, false, ResponseEmotion.Default, null, null)] : [];
        if (writtenResponses.Length > 0)
        {
            topicInfoAddressesToResponses[info.Address] = writtenResponses;
        }
        else
        {
            topicInfoAddressesToResponses.TryRemove(info.Address, out _);
        }
    }

    internal void SilenceGreetings(FormId<Actor> actorId, object owner) => silencedGreeters.GetOrAdd(actorId, _ => []).Add(owner);

    internal void RestoreGreetings(FormId<Actor> actorId, object owner)
    {
        if (silencedGreeters.TryGetValue(actorId, out HashSet<object>? owners) && owners.Remove(owner) && owners.Count == 0)
        {
            silencedGreeters.TryRemove(actorId, out _);
        }
    }

    internal void SilenceRemarks(FormId<Actor> actorId, object owner) => silencedRemarkers.GetOrAdd(actorId, _ => []).Add(owner);

    internal void RestoreRemarks(FormId<Actor> actorId, object owner)
    {
        if (silencedRemarkers.TryGetValue(actorId, out HashSet<object>? owners) && owners.Remove(owner) && owners.Count == 0)
        {
            silencedRemarkers.TryRemove(actorId, out _);
        }
    }

    internal void SetGreeting(FormId<Actor> actorId, object owner, IReadOnlyList<ReplyResponse> responses)
    {
        subtitleSwitch.FindSettings();
        greetings[actorId] = new(owner, ToWrittenResponses(responses));
    }

    internal void RemoveGreeting(FormId<Actor> actorId, object owner)
    {
        if (greetings.TryGetValue(actorId, out OwnedResponses? greeting) && greeting.Owner == owner)
        {
            greetings.TryRemove(new KeyValuePair<FormId<Actor>, OwnedResponses>(actorId, greeting));
        }
    }

    internal void SetGoodbye(FormId<Actor> actorId, object owner, IReadOnlyList<ReplyResponse> responses)
    {
        subtitleSwitch.FindSettings();
        goodbyes[actorId] = new(owner, ToWrittenResponses(responses));
    }

    internal void RemoveGoodbye(FormId<Actor> actorId, object owner)
    {
        if (goodbyes.TryGetValue(actorId, out OwnedResponses? goodbye) && goodbye.Owner == owner)
        {
            goodbyes.TryRemove(new KeyValuePair<FormId<Actor>, OwnedResponses>(actorId, goodbye));
        }
    }

    private void ClearGreeters()
    {
        goodbyes.Clear();
        silencedGreeters.Clear();
        silencedRemarkers.Clear();
        greetings.Clear();
    }

    private static WrittenResponse[] ToWrittenResponses(IReadOnlyList<ReplyResponse> responses) => [.. responses.SelectMany(ToWrittenResponses)];

    // A silence's idles go in its first response only, because an idle of the first plays through the second.
    private static IEnumerable<WrittenResponse> ToWrittenResponses(ReplyResponse response)
    {
        Idle? speakerIdle = response.SpeakerIdleId?.FormOrNull;
        Idle? listenerIdle = response.ListenerIdleId?.FormOrNull;
        if (response.Silence is { } silence)
        {
            int count = TopicResponse.GetSilentResponseCount(silence);
            string voicePath = SilentVoices.GetPathForDuration(silence / count);
            return Enumerable.Range(0, count)
                .Select(i => new WrittenResponse(response.Text, voicePath, false, response.Emotion, i == 0 ? speakerIdle : null, i == 0 ? listenerIdle : null));
        }
        return [new WrittenResponse(response.Text, response.VoicePath ?? SilentVoices.GetPathForText(response.Text), response.VoicePath == null, response.Emotion, speakerIdle, listenerIdle)];
    }

    // The game builds the reply to a chosen topic, and a greeting that opens a conversation, as the menu's last selected dialogue, a copy of the record's responses.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]
    private static byte OnUpdateInDialogue(Actor speaker, DialogueResponse response, byte unused)
    {
        try
        {
            // The game can pass no response.
            if (response.Exists
                && instance != null
                && (!instance.topicInfoAddressesToResponses.IsEmpty || !instance.silencedGreeters.IsEmpty || !instance.greetings.IsEmpty)
                && instance.gameTarget.MenuTopicManager is { MenuOpen: true } menu
                && menu.Speaker?.Address == speaker.Address
                && menu.LastSelectedDialogue is { ParentTopicInfo: { } info } dialogue
                && dialogue.Responses.Get() is { Count: > 0 } responses
                && responses[0].Address == response.Address)
            {
                if (instance.topicInfoAddressesToResponses.TryGetValue(info.Address, out WrittenResponse[]? writtenResponses))
                {
                    instance.WriteInConversation(speaker, info, dialogue, responses, writtenResponses);
                }
                else if (info.ParentTopic?.Data.GetSubtype() == DIALOGUE_DATA.Subtype.kHello)
                {
                    FormId<Actor> speakerId = speaker.FormId;
                    if (instance.greetings.TryGetValue(speakerId, out OwnedResponses? greeting))
                    {
                        instance.WriteInConversation(speaker, info, dialogue, responses, greeting.Responses);
                        Silence(responses.Skip(greeting.Responses.Length));
                    }
                    else if (instance.silencedGreeters.ContainsKey(speakerId))
                    {
                        Silence(responses);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            instance?.log.Write($"Rewriting a reply or a greeting threw, so it plays as the game built it: {ex}");
        }
        return ((delegate* unmanaged[Thiscall]<Actor, DialogueResponse, byte, byte>)originalUpdateInDialogue)(speaker, response, unused);
    }

    // CommonLib declares the speaker as an Actor in one version and as a reference in another, so the speaker is cast to an actor.
    [UnmanagedCallersOnly]
    private static nint OnDialogueItemConstructed(nint item, nint quest, nint topic, nint info, nint speaker)
    {
        nint constructedItem = originalDialogueItemConstructor(item, quest, topic, info, speaker);
        try
        {
            if (instance != null
                && instance.topicInfoAddressesToResponses.TryGetValue(info, out WrittenResponse[]? writtenResponses)
                && DialogueItem.OrNull(item) is { } dialogueItem
                && dialogueItem.Responses.Get() is { Count: > 0 } responses
                && ObjectReference.OrNull(speaker)?.AsActor() is { } actor
                && TopicInfo.OrNull(info) is { } topicInfo)
            {
                instance.Write(actor, topicInfo, responses, dialogueItem.Responses.Add, writtenResponses, false);
                if (writtenResponses.Any(writtenResponse => writtenResponse.Unvoiced))
                {
                    instance.subtitleSwitch.BeginLine(topicInfo.FormId);
                }
            }
            // The game builds a greeting said outside a conversation here. A greeting that opens a conversation is not built here.
            else if (instance != null
                && !instance.silencedGreeters.IsEmpty
                && Topic.OrNull(topic)?.Data.GetSubtype() == DIALOGUE_DATA.Subtype.kHello
                && ObjectReference.OrNull(speaker)?.AsActor() is { } greetingSpeaker
                && instance.silencedGreeters.ContainsKey(greetingSpeaker.FormId)
                && DialogueItem.OrNull(item)?.Responses.Get() is { } greetingResponses)
            {
                Silence(greetingResponses);
            }
            else if (instance != null
                && !instance.silencedRemarkers.IsEmpty
                && Topic.OrNull(topic)?.Data.GetSubtype() is DIALOGUE_DATA.Subtype.kIdle or DIALOGUE_DATA.Subtype.kNoticeCorpse
                && ObjectReference.OrNull(speaker)?.AsActor() is { } remarkSpeaker
                && instance.silencedRemarkers.ContainsKey(remarkSpeaker.FormId)
                && DialogueItem.OrNull(item)?.Responses.Get() is { } remarkResponses)
            {
                Silence(remarkResponses);
            }
            // The game builds a goodbye here, after the dialogue menu has closed.
            else if (instance != null
                && !instance.goodbyes.IsEmpty
                && Topic.OrNull(topic)?.Data.GetSubtype() == DIALOGUE_DATA.Subtype.kGoodBye
                && ObjectReference.OrNull(speaker)?.AsActor() is { } goodbyeSpeaker
                && instance.goodbyes.TryGetValue(goodbyeSpeaker.FormId, out OwnedResponses? goodbye)
                && DialogueItem.OrNull(item) is { } goodbyeItem
                && goodbyeItem.Responses.Get() is { Count: > 0 } goodbyeResponses
                && TopicInfo.OrNull(info) is { } goodbyeInfo)
            {
                if (goodbye.Responses.Length > 0)
                {
                    instance.Write(goodbyeSpeaker, goodbyeInfo, goodbyeResponses, goodbyeItem.Responses.Add, goodbye.Responses, false);
                    if (goodbye.Responses.Any(writtenResponse => writtenResponse.Unvoiced))
                    {
                        instance.subtitleSwitch.BeginLine(goodbyeInfo.FormId);
                    }
                }
                Silence(goodbyeResponses.Skip(goodbye.Responses.Length));
            }
        }
        catch (Exception ex)
        {
            instance?.log.Write($"Rewriting a dialogue item threw, so it plays as the game built it: {ex}");
        }
        return constructedItem;
    }

    private void WriteInConversation(Actor speaker, TopicInfo info, MenuTopicManager.Dialogue dialogue, IReadOnlyList<DialogueResponse> responses, WrittenResponse[] writtenResponses)
    {
        Write(speaker, info, responses, dialogue.Responses.Add, writtenResponses, true);
        if (writtenResponses.Any(writtenResponse => writtenResponse.Unvoiced))
        {
            subtitleSwitch.EnableDialogueSubtitles();
        }
    }

    private void Write(Actor speaker, TopicInfo info, IReadOnlyList<DialogueResponse> responses, Action<DialogueResponse> append, WrittenResponse[] writtenResponses, bool inConversation)
    {
        foreach (var (i, writtenResponse) in writtenResponses.Index())
        {
            DialogueResponse dialogueResponse = i < responses.Count ? responses[i] : Append(append, responses[0]);
            dialogueResponse.Text = writtenResponse.Text;
            dialogueResponse.Voice = writtenResponse.Voice;
            dialogueResponse.AnimFaceArchType = writtenResponse.Emotion.Type;
            dialogueResponse.Percent = (ushort)writtenResponse.Emotion.Value;
            dialogueResponse.UseEmotion = writtenResponse.Emotion.Animated;
            dialogueResponse.SpeakerIdle = writtenResponse.SpeakerIdle;
            dialogueResponse.ListenIdle = writtenResponse.ListenerIdle;
        }
        if (writtenResponses.Length > 0 && writtenResponses.All(writtenResponse => writtenResponse.Unvoiced))
        {
            unvoicedLipSync.Begin(speaker.FormId, info.FormId, inConversation);
        }
    }

    private static void Silence(IEnumerable<DialogueResponse> responses)
    {
        foreach (DialogueResponse response in responses)
        {
            DialogueResponse silencedResponse = response;
            silencedResponse.Text = "";
            silencedResponse.Voice = SilentVoices.BriefSilencePath;
        }
    }

    // Allocated on the game's heap, since the game frees the responses of a reply and of a dialogue item.
    private DialogueResponse Append(Action<DialogueResponse> append, DialogueResponse source)
    {
        // The size CommonLib asserts for DialogueResponse.
        const int dialogueResponseSize = 0x40;
        DialogueResponse copy = DialogueResponse.OrNull(GameHeap.AllocateZeroed(gameTarget, dialogueResponseSize, "DialogueResponse"))!.Value;
        copy.Text = source.Text;
        copy.AnimFaceArchType = source.AnimFaceArchType;
        copy.Percent = source.Percent;
        copy.Voice = source.Voice;
        copy.SpeakerIdle = source.SpeakerIdle;
        copy.ListenIdle = source.ListenIdle;
        copy.VoiceSound = source.VoiceSound;
        copy.UseEmotion = source.UseEmotion;
        copy.SoundLip = source.SoundLip;
        append(copy);
        return copy;
    }

    // The idles are resolved when recorded, since the game enters the replacements from any thread and they look up no form.
    private readonly record struct WrittenResponse(string Text, string Voice, bool Unvoiced, ResponseEmotion Emotion, Idle? SpeakerIdle, Idle? ListenerIdle);

    private sealed record OwnedResponses(object Owner, WrittenResponse[] Responses);
}
