using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class ForceGreet(GameTarget gameTarget, IUtility utility, IGameSession session) : IForceGreet
{
    // WIDeadBody01GuardForcegreetPlayer: targets the player, has no conditions, and opens the conversation at 300 units.
    internal static readonly FormId<Package> PackageId = new(0x0003877D);

    private readonly GameTarget gameTarget = gameTarget;
    private readonly IUtility utility = utility;
    private readonly IGameSession session = session;

    public async Task StartAsync<TActor>(TActor actor, CancellationToken cancellationToken = default) where TActor : IActor
    {
        const float minimumDistanceToGiveAgain = 600f;
        const float secondsBetweenChecks = 0.25f;
        Package package = PackageId.Form;
        FormId<Actor> actorId = actor.GetFormId<Actor>();
        using (CancellationTokenSource stopSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.NewGameOrSaveLoading))
        {
            stopSource.Token.ThrowIfCancellationRequested();
            bool given = false;
            while (!gameTarget.MenuTopicManager.IsConversationOpenWith(actorId))
            {
                if (actorId.FormOrNull is { } currentActor && currentActor.Is3DLoaded() && currentActor.GetCurrentPackage()?.FormId != PackageId
                    && (!given || currentActor.GetDistance(gameTarget.Player) > minimumDistanceToGiveAgain))
                {
                    currentActor.PutCreatedPackage(package, true, false, true);
                    given = true;
                }
                await utility.WaitSecondsAsync(secondsBetweenChecks, stopSource.Token);
            }
        }
    }
}
