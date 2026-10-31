using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

internal sealed class SceneConversations(ISceneOwner owner, GameTarget gameTarget)
{
    private readonly ISceneOwner owner = owner;
    private readonly GameTarget gameTarget = gameTarget;

    internal void Show(QuestScene scene, ScenePlay play, int? phase)
    {
        if (phase == play.ConversationPhase)
        {
            return;
        }
        foreach (SceneConversation conversation in Get(scene, play.ConversationPhase))
        {
            conversation.Branch.Hide();
        }
        play.ConversationPhase = phase;
        play.OpenedConversations.Clear();
        foreach (SceneConversation conversation in Get(scene, phase))
        {
            SetPackage(play, conversation, ForceGreet.PackageId);
            if (owner.GetReference(conversation.Actor)?.FormOrNull?.AsActor() is { } actor)
            {
                conversation.Branch.Show(actor);
            }
        }
    }

    // The force-greet package opens a second conversation as the first closes, if the actor chooses a package before the game has ended the phase.
    // The action of an open conversation therefore runs the wait package.
    internal void ReplaceOpenedPackages(QuestScene scene, ScenePlay play)
    {
        foreach (SceneConversation conversation in Get(scene, play.ConversationPhase))
        {
            if (!play.OpenedConversations.Contains(conversation) && owner.GetReference(conversation.Actor) is { } actorId
                && gameTarget.MenuTopicManager.IsConversationOpenWith(actorId))
            {
                play.OpenedConversations.Add(conversation);
                SetPackage(play, conversation, KnownForms.zzzDEFAULTzzzWaitForDialogue);
            }
        }
    }

    private static void SetPackage(ScenePlay play, SceneConversation conversation, FormId<Package> packageId)
    {
        foreach ((SceneConversation candidate, GameList<Package> packages) in play.SceneRecord.ConversationPackages)
        {
            if (candidate == conversation)
            {
                packages.Clear();
                packages.Add(packageId.Form);
            }
        }
    }

    internal static IEnumerable<SceneConversation> Get(QuestScene scene, int? phase) =>
        phase is { } index ? scene.Phases[index].Actions.OfType<SceneConversation>() : [];
}
