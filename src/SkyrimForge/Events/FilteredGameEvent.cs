using System;
using System.Threading.Tasks;

namespace SkyrimForge;

internal sealed class FilteredGameEvent<TArgs>(IGameEvent<TArgs> source, Func<TArgs, bool> predicate) : IGameEvent<TArgs> where TArgs : struct
{
    public ISubscription Subscribe(GameEventHandler<TArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return source.Subscribe(args =>
        {
            if (predicate(args))
            {
                handler(args);
            }
        });
    }

    public ISubscription Subscribe(AsyncGameEventHandler<TArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return source.Subscribe(args => predicate(args) ? handler(args) : Task.CompletedTask);
    }
}
