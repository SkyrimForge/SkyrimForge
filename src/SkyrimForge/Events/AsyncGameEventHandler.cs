using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Handles a game event asynchronously. The game waits for the handler to return its task.</summary>
/// <remarks>A thrown exception (or a faulted task) is logged, and the handler's subscription is removed. A canceled task is ignored.</remarks>
public delegate Task AsyncGameEventHandler<TArgs>(TArgs args) where TArgs : struct;
