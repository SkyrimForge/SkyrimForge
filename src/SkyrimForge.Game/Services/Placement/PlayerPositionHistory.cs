using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class PlayerPositionHistory : IPlayerPositionHistory
{
    private static readonly TimeSpan recordInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan groundCheck = TimeSpan.FromSeconds(0.25);

    private readonly GameTarget gameTarget;
    private readonly IUtility utility;
    private readonly Queue<(FormId<WorldSpace> Worldspace, NiPoint3 Position)> recordedPositions = new();

    public PlayerPositionHistory(GameTarget gameTarget, IGameEvents events, IUtility utility)
    {
        this.gameTarget = gameTarget;
        this.utility = utility;
        events.OnFrame.SubscribeEvery(recordInterval, _ => Record());
        events.OnNewGameOrSaveLoading.Subscribe(_ => recordedPositions.Clear());
    }

    public NiPoint3? GroundedPosition
    {
        get
        {
            PlayerCharacter player = gameTarget.Player;
            if (player.IsOnMount() && player.GetMount(out Actor? mount) && mount is { } horse)
            {
                return IsGrounded(horse) ? horse.GetPosition() : null;
            }
            return IsGrounded(player) ? player.GetPosition() : null;
        }
    }

    public async Task<NiPoint3?> WaitForGroundedPositionAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        for (TimeSpan elapsed = TimeSpan.Zero; elapsed < timeout; elapsed += groundCheck)
        {
            if (GroundedPosition is { } position)
            {
                return position;
            }
            await utility.WaitAsync(groundCheck, cancellationToken);
        }
        return GroundedPosition;
    }

    public SpawnPosition? FindSpawnPosition(float minimumDistance, float maximumDistance, IEnumerable<NiPoint3>? candidates = null)
    {
        const float rayStartHeight = 300f;
        const float rayEndDepth = 600f;
        PlayerCharacter player = gameTarget.Player;
        NiPoint3 position = player.GetPosition();
        FormId<WorldSpace>? worldspace = player.GetWorldspace()?.FormId;
        bool IsInRange(NiPoint3 otherPosition)
        {
            float distance = position.GetDistance(otherPosition);
            return distance > minimumDistance && distance < maximumDistance;
        }
        if (recordedPositions.Reverse().Where(recordedPosition => recordedPosition.Worldspace == worldspace && IsInRange(recordedPosition.Position)).Select(recordedPosition => recordedPosition.Position).FirstOrNull() is { } trailPosition)
        {
            return new SpawnPosition(trailPosition, SpawnPositionSource.History);
        }
        if ((candidates ?? []).FirstOrNull(IsInRange) is { } candidatePosition)
        {
            return new SpawnPosition(candidatePosition, SpawnPositionSource.Candidate);
        }
        if (gameTarget.GetMapMarkers().Where(marker => marker.WorldspaceId == worldspace && IsInRange(marker.Position))
            .Select(marker => gameTarget.TES.GetPositionOnGround(marker.Position, rayStartHeight, rayEndDepth))
            .FirstOrDefault(ground => ground != null && IsInRange(ground.Value)) is { } markerPosition)
        {
            return new SpawnPosition(markerPosition, SpawnPositionSource.MapMarker);
        }
        float heading = player.GetAngleZ();
        float distanceBehind = (minimumDistance + maximumDistance) / 2f;
        NiPoint3 behindPlayer = new(position.X - distanceBehind * MathF.Sin(heading), position.Y - distanceBehind * MathF.Cos(heading), position.Z);
        return gameTarget.TES.GetPositionOnGround(behindPlayer, rayStartHeight, rayEndDepth) is { } ground && IsInRange(ground) ? new SpawnPosition(ground, SpawnPositionSource.BehindPlayer) : null;
    }

    private void Record()
    {
        const float spacing = 500f;
        const int length = 60;
        PlayerCharacter player = gameTarget.Player;
        if (!player.Is3DLoaded() || player.GetWorldspace()?.FormId is not { } worldspace || GroundedPosition is not { } position)
        {
            return;
        }
        (FormId<WorldSpace> lastWorldspace, NiPoint3 lastPosition) = recordedPositions.LastOrDefault();
        if (recordedPositions.Count == 0 || lastWorldspace != worldspace || position.GetHorizontalDistance(lastPosition) >= spacing)
        {
            recordedPositions.Enqueue((worldspace, position));
            if (recordedPositions.Count > length)
            {
                recordedPositions.Dequeue();
            }
        }
    }

    private static bool IsGrounded<TActor>(TActor actor) where TActor : IActor => !actor.IsInMidair() && !actor.AsActorState().IsSwimming();
}
