using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SkyrimForge;

/// <summary>
/// Maps each token passed to native code to the managed value it identifies. It is concurrent because a native callback can look up one token while another thread adds another.
/// Native code receives only the token, so the registry retains the value until it is removed, and a native callback reads the registry through a static member.
/// </summary>
/// <typeparam name="T">The type of the values the tokens identify.</typeparam>
internal sealed class TokenRegistry<T>
{
    private readonly ConcurrentDictionary<int, T> entries = new();

    private int lastToken;

    internal int Add(T value)
    {
        int token = Interlocked.Increment(ref lastToken);
        entries[token] = value;
        return token;
    }

    internal bool TryGetValue(int token, [MaybeNullWhen(false)] out T value) => entries.TryGetValue(token, out value);

    internal bool TryRemove(int token, [MaybeNullWhen(false)] out T value) => entries.TryRemove(token, out value);
}
