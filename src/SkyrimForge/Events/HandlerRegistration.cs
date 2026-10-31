using System;

namespace SkyrimForge;

// A class rather than a record, so removing one registration never removes another of the same handler.
internal sealed class HandlerRegistration<THandler>(THandler handler, ILog log) where THandler : Delegate
{
    internal THandler Handler { get; } = handler;

    internal ILog Log { get; } = log;
}
