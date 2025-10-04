// original source https://github.com/StephenCleary/AsyncEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zapto.AspNetCore.Utils
{
    /// <summary>
    /// Provides asynchronous wrappers.
    /// </summary>
    internal static class AsyncFactory
    {
        private static AsyncCallback Callback(Action<IAsyncResult> endMethod, TaskCompletionSource<object?> tcs)
        {
            var originalTaskScheduler = TaskScheduler.Current;
            return (asyncResult) =>
            {
                if (!asyncResult.CompletedSynchronously)
                {
                    if (TaskScheduler.Current == originalTaskScheduler)
                        CallEndMethod(asyncResult, endMethod, tcs);
                    else
                        Task.Factory.StartNew(() => CallEndMethod(asyncResult, endMethod, tcs),
                            CancellationToken.None, TaskCreationOptions.None, originalTaskScheduler);
                }
            };
        }

        private static void CallEndMethod(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCompletionSource<object?> tcs)
        {
            try
            {
                endMethod(asyncResult);
                tcs.TrySetResult(null);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        /// <summary>
        /// Wraps a begin/end asynchronous method.
        /// </summary>
        /// <param name="beginMethod">The begin method.</param>
        /// <param name="endMethod">The end method.</param>
        /// <returns></returns>
        public static Task FromApm(Func<AsyncCallback, object?, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod)
        {
            var tcs = new TaskCompletionSource<object?>();
            var asyncResult = beginMethod(Callback(endMethod, tcs), null);
            if (asyncResult.CompletedSynchronously)
                CallEndMethod(asyncResult, endMethod, tcs);
            return tcs.Task;
        }

        #region FromApm arg0 .. arg2

        /// <summary>
        /// Wraps a begin/end asynchronous method.
        /// </summary>
        /// <typeparam name="TArg0">The type of argument 0.</typeparam>
        /// <param name="beginMethod">The begin method.</param>
        /// <param name="endMethod">The end method.</param>
        /// <param name="arg0">Argument 0.</param>
        /// <returns></returns>
        public static Task FromApm<TArg0>(Func<TArg0, AsyncCallback, object?, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg0 arg0)
        {
            var tcs = new TaskCompletionSource<object?>();
            var asyncResult = beginMethod(arg0, Callback(endMethod, tcs), null);
            if (asyncResult.CompletedSynchronously)
                CallEndMethod(asyncResult, endMethod, tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Wraps a begin/end asynchronous method.
        /// </summary>
        /// <typeparam name="TArg0">The type of argument 0.</typeparam>
        /// <typeparam name="TArg1">The type of argument 1.</typeparam>
        /// <param name="beginMethod">The begin method.</param>
        /// <param name="endMethod">The end method.</param>
        /// <param name="arg0">Argument 0.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <returns></returns>
        public static Task FromApm<TArg0, TArg1>(Func<TArg0, TArg1, AsyncCallback, object?, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg0 arg0, TArg1 arg1)
        {
            var tcs = new TaskCompletionSource<object?>();
            var asyncResult = beginMethod(arg0, arg1, Callback(endMethod, tcs), null);
            if (asyncResult.CompletedSynchronously)
                CallEndMethod(asyncResult, endMethod, tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Wraps a begin/end asynchronous method.
        /// </summary>
        /// <typeparam name="TArg0">The type of argument 0.</typeparam>
        /// <typeparam name="TArg1">The type of argument 1.</typeparam>
        /// <typeparam name="TArg2">The type of argument 2.</typeparam>
        /// <param name="beginMethod">The begin method.</param>
        /// <param name="endMethod">The end method.</param>
        /// <param name="arg0">Argument 0.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="arg2">Argument 2.</param>
        /// <returns></returns>
        public static Task FromApm<TArg0, TArg1, TArg2>(Func<TArg0, TArg1, TArg2, AsyncCallback, object?, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            var tcs = new TaskCompletionSource<object?>();
            var asyncResult = beginMethod(arg0, arg1, arg2, Callback(endMethod, tcs), null);
            if (asyncResult.CompletedSynchronously)
                CallEndMethod(asyncResult, endMethod, tcs);
            return tcs.Task;
        }

        #endregion

        /// <summary>
        /// Wraps a <see cref="Task"/> into the Begin method of an APM pattern.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        /// <param name="callback">The callback method passed into the Begin method of the APM pattern.</param>
        /// <param name="state">The state passed into the Begin method of the APM pattern.</param>
        /// <returns>The asynchronous operation, to be returned by the Begin method of the APM pattern.</returns>
        public static IAsyncResult ToBegin(Task task, AsyncCallback callback, object? state)
        {
            if (task.IsCompleted)
                return new AsyncResultCompletedSynchronously(task, state);
            var tcs = new TaskCompletionSource<object?>(state);
            task.ContinueWith(_ =>
            {
                if (task.IsFaulted)
                {
                    if (task.Exception != null)
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    else
                        tcs.TrySetException(new Exception[] { new Exception("Unknown error") });
                }
                else if (task.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(null);

                callback(tcs.Task);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
            return tcs.Task;
        }

        /// <summary>
        /// Wraps a <see cref="Task"/> into the End method of an APM pattern.
        /// </summary>
        /// <param name="asyncResult">The asynchronous operation returned by the matching Begin method of this APM pattern.</param>
        /// <returns>The result of the asynchronous operation, to be returned by the End method of the APM pattern.</returns>
        public static void ToEnd(IAsyncResult asyncResult)
        {
            if (asyncResult is AsyncResultCompletedSynchronously asyncResultCompletedSynchronously)
                ((Task)asyncResultCompletedSynchronously).GetAwaiter().GetResult();
            else
            {
                ((Task)asyncResult).GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>
    /// Provides asynchronous wrappers.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the asychronous operation.</typeparam>
    public static class AsyncFactory<TResult>
    {
        private static AsyncCallback Callback(Func<IAsyncResult, TResult> endMethod, TaskCompletionSource<TResult> tcs)
        {
            var originalTaskScheduler = TaskScheduler.Current;
            return (asyncResult) =>
            {
                if (!asyncResult.CompletedSynchronously)
                {
                    if (TaskScheduler.Current == originalTaskScheduler)
                        CallEndMethod(asyncResult, endMethod, tcs);
                    else
                        Task.Factory.StartNew(() => CallEndMethod(asyncResult, endMethod, tcs),
                            CancellationToken.None, TaskCreationOptions.None, originalTaskScheduler);
                }
            };
        }

        private static void CallEndMethod(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCompletionSource<TResult> tcs)
        {
            try
            {
                tcs.TrySetResult(endMethod(asyncResult));
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        /// <summary>
        /// Wraps a begin/end asynchronous method.
        /// </summary>
        /// <param name="beginMethod">The begin method. May not be <c>null</c>.</param>
        /// <param name="endMethod">The end method. May not be <c>null</c>.</param>
        /// <returns>The result of the asynchronous operation.</returns>
        public static Task<TResult> FromApm(Func<AsyncCallback, object?, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod)
        {
            var tcs = new TaskCompletionSource<TResult>();
            var asyncResult = beginMethod(Callback(endMethod, tcs), null);
            if (asyncResult.CompletedSynchronously)
                CallEndMethod(asyncResult, endMethod, tcs);
            return tcs.Task;
        }

        #region FromApm arg0 .. arg2

        /// <summary>
        /// Wraps a begin/end asynchronous method.
        /// </summary>
        /// <typeparam name="TArg0">The type of argument 0.</typeparam>
        /// <param name="beginMethod">The begin method.</param>
        /// <param name="endMethod">The end method.</param>
        /// <param name="arg0">Argument 0.</param>
        /// <returns>The result of the asynchronous operation.</returns>
        public static Task<TResult> FromApm<TArg0>(Func<TArg0, AsyncCallback, object?, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg0 arg0)
        {
            var tcs = new TaskCompletionSource<TResult>();
            var asyncResult = beginMethod(arg0, Callback(endMethod, tcs), null);
            if (asyncResult.CompletedSynchronously)
                CallEndMethod(asyncResult, endMethod, tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Wraps a begin/end asynchronous method.
        /// </summary>
        /// <typeparam name="TArg0">The type of argument 0.</typeparam>
        /// <typeparam name="TArg1">The type of argument 1.</typeparam>
        /// <param name="beginMethod">The begin method.</param>
        /// <param name="endMethod">The end method.</param>
        /// <param name="arg0">Argument 0.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <returns>The result of the asynchronous operation.</returns>
        public static Task<TResult> FromApm<TArg0, TArg1>(Func<TArg0, TArg1, AsyncCallback, object?, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg0 arg0, TArg1 arg1)
        {
            var tcs = new TaskCompletionSource<TResult>();
            var asyncResult = beginMethod(arg0, arg1, Callback(endMethod, tcs), null);
            if (asyncResult.CompletedSynchronously)
                CallEndMethod(asyncResult, endMethod, tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Wraps a begin/end asynchronous method.
        /// </summary>
        /// <typeparam name="TArg0">The type of argument 0.</typeparam>
        /// <typeparam name="TArg1">The type of argument 1.</typeparam>
        /// <typeparam name="TArg2">The type of argument 2.</typeparam>
        /// <param name="beginMethod">The begin method.</param>
        /// <param name="endMethod">The end method.</param>
        /// <param name="arg0">Argument 0.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="arg2">Argument 2.</param>
        /// <returns>The result of the asynchronous operation.</returns>
        public static Task<TResult> FromApm<TArg0, TArg1, TArg2>(Func<TArg0, TArg1, TArg2, AsyncCallback, object?, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            var tcs = new TaskCompletionSource<TResult>();
            var asyncResult = beginMethod(arg0, arg1, arg2, Callback(endMethod, tcs), null);
            if (asyncResult.CompletedSynchronously)
                CallEndMethod(asyncResult, endMethod, tcs);
            return tcs.Task;
        }

        #endregion

        /// <summary>
        /// Wraps a <see cref="Task{TResult}"/> into the Begin method of an APM pattern.
        /// </summary>
        /// <param name="task">The task to wrap. May not be <c>null</c>.</param>
        /// <param name="callback">The callback method passed into the Begin method of the APM pattern.</param>
        /// <param name="state">The state passed into the Begin method of the APM pattern.</param>
        /// <returns>The asynchronous operation, to be returned by the Begin method of the APM pattern.</returns>
        public static IAsyncResult ToBegin(Task<TResult> task, AsyncCallback callback, object? state)
        {
            if (task.IsCompleted)
                return new AsyncResultCompletedSynchronously<TResult>(task, state);
            var tcs = new TaskCompletionSource<TResult>(state);
            task.ContinueWith(_ =>
            {
                if (task.IsFaulted)
                {
                    if (task.Exception != null)
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    else
                        tcs.TrySetException(new Exception[] { new Exception("Unknown error") });
                }
                else if (task.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(task.Result);

                callback(tcs.Task);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
            return tcs.Task;
        }

        /// <summary>
        /// Wraps a <see cref="Task{TResult}"/> into the End method of an APM pattern.
        /// </summary>
        /// <param name="asyncResult">The asynchronous operation returned by the matching Begin method of this APM pattern.</param>
        /// <returns>The result of the asynchronous operation, to be returned by the End method of the APM pattern.</returns>
        public static TResult ToEnd(IAsyncResult asyncResult)
        {
            if (asyncResult is AsyncResultCompletedSynchronously<TResult> asyncResultCompletedSynchronously)
                return ((Task<TResult>)asyncResultCompletedSynchronously).GetAwaiter().GetResult();
            return ((Task<TResult>)asyncResult).GetAwaiter().GetResult();
        }
    }
}