using System;

namespace SkyrimForge.Papyrus;

/// <summary>Provides extension members for <see cref="IPapyrusFunctionRegistrar"/>.</summary>
public static class PapyrusFunctionRegistrarExtensions
{
    extension(IPapyrusFunctionRegistrar registrar)
    {
        /// <summary>Registers the implementation of a native static (global) function.</summary>
        /// <remarks>
        /// A lambda whose body is an expression with a value binds the overload that registers a return type,
        /// so register a function declaring no return with a lambda whose body is a block.
        /// </remarks>
        /// <exception cref="TypeInitializationException">Papyrus has no type for a type argument, wrapping the <see cref="NotSupportedException"/> that identifies it.</exception>
        /// <exception cref="InvalidOperationException">The function was not registered. The message states the reason.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="className"/>, <paramref name="functionName"/>, or <paramref name="body"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="className"/> or <paramref name="functionName"/> is empty.</exception>
        public void RegisterStatic<TReturn>(string className, string functionName, Func<TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName, [], call => call.ReturnAs(body()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, TReturn>(string className, string functionName, Func<T0?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>()], call => call.ReturnAs(body(call.GetArgument(0).As<T0>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, TReturn>(string className, string functionName, Func<T0?, T1?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, TReturn>(string className, string functionName, Func<T0?, T1?, T2?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, TReturn>(string className, string functionName, Func<T0?, T1?, T2?, T3?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, TReturn>(string className, string functionName, Func<T0?, T1?, T2?, T3?, T4?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, TReturn>(string className, string functionName, Func<T0?, T1?, T2?, T3?, T4?, T5?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6, TReturn>(string className, string functionName, Func<T0?, T1?, T2?, T3?, T4?, T5?, T6?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6, T7, TReturn>(string className, string functionName, Func<T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn>(string className, string functionName, Func<T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>(string className, string functionName, Func<T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, T9?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>(), PapyrusType.FromType<T9>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>(), call.GetArgument(9).As<T9>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>(string className, string functionName, Func<T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, T9?, T10?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.FromType<TReturn>(), functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>(), PapyrusType.FromType<T9>(), PapyrusType.FromType<T10>()],
                call => call.ReturnAs(body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>(), call.GetArgument(9).As<T9>(), call.GetArgument(10).As<T10>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic(string className, string functionName, Action body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName, [], _ => body());
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0>(string className, string functionName, Action<T0?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName, [PapyrusType.FromType<T0>()], call => body(call.GetArgument(0).As<T0>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1>(string className, string functionName, Action<T0?, T1?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>()], call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2>(string className, string functionName, Action<T0?, T1?, T2?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>()],
                call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3>(string className, string functionName, Action<T0?, T1?, T2?, T3?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>()],
                call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4>(string className, string functionName, Action<T0?, T1?, T2?, T3?, T4?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>()],
                call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5>(string className, string functionName, Action<T0?, T1?, T2?, T3?, T4?, T5?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>()],
                call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6>(string className, string functionName, Action<T0?, T1?, T2?, T3?, T4?, T5?, T6?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>()],
                call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6, T7>(string className, string functionName, Action<T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>()],
                call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6, T7, T8>(string className, string functionName, Action<T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>()],
                call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string className, string functionName, Action<T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, T9?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>(), PapyrusType.FromType<T9>()],
                call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>(), call.GetArgument(9).As<T9>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterStatic{TReturn}(string, string, Func{TReturn})"/>
        public void RegisterStatic<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string className, string functionName, Action<T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, T9?, T10?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterStatic(className, PapyrusType.None, functionName,
                [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>(), PapyrusType.FromType<T9>(), PapyrusType.FromType<T10>()],
                call => body(call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>(), call.GetArgument(9).As<T9>(), call.GetArgument(10).As<T10>()));
        }

        /// <summary>Registers the implementation of a native instance (non-global) function.</summary>
        /// <remarks>
        /// The receiver does not count toward the game's limit of eleven arguments.
        /// A lambda whose body is an expression with a value binds the overload that registers a return type,
        /// so register a function declaring no return with a lambda whose body is a block.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="selfClassName"/>, <paramref name="functionName"/>, or <paramref name="body"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selfClassName"/> or <paramref name="functionName"/> is empty, or <typeparamref name="TSelf"/> is not a form view.
        /// </exception>
        /// <exception cref="TypeInitializationException">Papyrus has no type for a type argument, wrapping the <see cref="NotSupportedException"/> that identifies it.</exception>
        /// <exception cref="InvalidOperationException">The function was not registered. The message states the reason.</exception>
        public void RegisterInstance<TSelf, TReturn>(string selfClassName, string functionName, Func<TSelf?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [],
                call => call.ReturnAs(body(call.Self.As<TSelf>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, T2?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, T2?, T3?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, T2?, T3?, T4?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6, T7, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6, T7, T8, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, T9?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>(), PapyrusType.FromType<T9>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>(), call.GetArgument(9).As<T9>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>(string selfClassName, string functionName, Func<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, T9?, T10?, TReturn> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.FromType<TReturn>(), functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>(), PapyrusType.FromType<T9>(), PapyrusType.FromType<T10>()],
                call => call.ReturnAs(body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>(), call.GetArgument(9).As<T9>(), call.GetArgument(10).As<T10>())));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf>(string selfClassName, string functionName, Action<TSelf?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [],
                call => body(call.Self.As<TSelf>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0>(string selfClassName, string functionName, Action<TSelf?, T0?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?, T2?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?, T2?, T3?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?, T2?, T3?, T4?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6, T7>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6, T7, T8>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, T9?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>(), PapyrusType.FromType<T9>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>(), call.GetArgument(9).As<T9>()));
        }

        /// <inheritdoc cref="extension(IPapyrusFunctionRegistrar).RegisterInstance{TSelf, TReturn}(string, string, Func{TSelf, TReturn})"/>
        public void RegisterInstance<TSelf, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string selfClassName, string functionName, Action<TSelf?, T0?, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, T9?, T10?> body)
        {
            ArgumentNullException.ThrowIfNull(body);
            registrar.RegisterInstance(selfClassName, PapyrusType.GetReceiverType<TSelf>(), PapyrusType.None, functionName, [PapyrusType.FromType<T0>(), PapyrusType.FromType<T1>(), PapyrusType.FromType<T2>(), PapyrusType.FromType<T3>(), PapyrusType.FromType<T4>(), PapyrusType.FromType<T5>(), PapyrusType.FromType<T6>(), PapyrusType.FromType<T7>(), PapyrusType.FromType<T8>(), PapyrusType.FromType<T9>(), PapyrusType.FromType<T10>()],
                call => body(call.Self.As<TSelf>(), call.GetArgument(0).As<T0>(), call.GetArgument(1).As<T1>(), call.GetArgument(2).As<T2>(), call.GetArgument(3).As<T3>(), call.GetArgument(4).As<T4>(), call.GetArgument(5).As<T5>(), call.GetArgument(6).As<T6>(), call.GetArgument(7).As<T7>(), call.GetArgument(8).As<T8>(), call.GetArgument(9).As<T9>(), call.GetArgument(10).As<T10>()));
        }
    }
}
