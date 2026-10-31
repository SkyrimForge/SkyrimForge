using System.Collections.Generic;

namespace SkyrimForge.Game;

internal sealed record Reply(IReadOnlyList<ReplyResponse> Responses, bool IsGoodbye);
