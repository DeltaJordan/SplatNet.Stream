using NLog;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace SplatNet.Stream.Extensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TaskExtensions
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static ConfiguredTaskAwaitable SafeAsync(this Task task)
        {
            return task.ConfigureAwait(false);
        }

        public static ConfiguredTaskAwaitable<T> SafeAsync<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false);
        }

#pragma warning disable AKI003
        public static ConfiguredValueTaskAwaitable SafeAsync(this ValueTask task)
        {
            return task.ConfigureAwait(false);
        }

        public static ConfiguredValueTaskAwaitable<T> SafeAsync<T>(this ValueTask<T> task)
        {
            return task.ConfigureAwait(false);
        }
#pragma warning restore AKI003

#pragma warning disable AKI002
        public static async Task<TOut> Then<TIn, TOut>(
            this Task<TIn> task,
            Func<TIn, TOut> syncFunc
        )
        {
            return syncFunc(await task.SafeAsync());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<TOut> As<TIn, TOut>(this Task<TIn> task) where TIn : TOut
        {
            return await task.SafeAsync();
        }
#pragma warning restore AKI002

        public static async Task<TOut> ThenAsync<TIn, TOut>(
            this Task<TIn> task,
            Func<TIn, Task<TOut>> asyncFunc
        )
        {
            return await asyncFunc(await task.SafeAsync()).SafeAsync();
        }


#pragma warning disable AKI002 // Asynchronous method name does not end in Async
        public static async void ThenDispatch(
            this Task promise,
            Action then = null,
            Action<Exception> @catch = null,
            Action @finally = null
        )
        {
            try
            {
                await promise.SafeAsync();
                then?.SafeDispatch();
            }
            catch (Exception exception)
            {
                if (@catch != null)
                {
                    @catch.SafeDispatch(exception);
                }
                else
                {
                    Logger.Error(exception, "Exception in detached thread");
                }
            }
            finally
            {
                @finally?.SafeDispatch();
            }
        }
        public static async void ThenDispatch<T>(
            this Task<T> promise,
            Action<T> then = null,
            Action<Exception> @catch = null,
            Action @finally = null
        )
        {
            try
            {
                T result = await promise.SafeAsync();
                then?.SafeDispatch(result);
            }
            catch (Exception exception)
            {
                if (@catch != null)
                {
                    @catch.SafeDispatch(exception);
                }
                else
                {
                    Logger.Error(exception, "Exception in detached thread");
                }
            }
            finally
            {
                @finally?.SafeDispatch();
            }
        }
#pragma warning restore AKI002 // Asynchronous method name does not end in Async

        private static void SafeDispatch(this Action action)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(action);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Exception in callback to UI thread");
            }
        }

        private static void SafeDispatch<T>(this Action<T> action, T arg)
        {
            SafeDispatch(() => action(arg));
        }

    }
}
