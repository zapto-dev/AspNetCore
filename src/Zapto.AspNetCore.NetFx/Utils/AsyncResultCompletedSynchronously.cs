using System;
using System.Threading.Tasks;

namespace Zapto.AspNetCore.Utils
{
    internal class AsyncResultCompletedSynchronously(Task task, object? state) : IAsyncResult
    {
        private readonly Task _task = task;

        public static explicit operator Task(AsyncResultCompletedSynchronously @this) => @this._task;

        public object? AsyncState => state;

        public System.Threading.WaitHandle AsyncWaitHandle => (_task as IAsyncResult).AsyncWaitHandle;

        public bool CompletedSynchronously => true;

        public bool IsCompleted => true;
    }

    public class AsyncResultCompletedSynchronously<TResult>(Task<TResult> task, object? state) : IAsyncResult
    {
        private readonly Task<TResult> _task = task;

        public static explicit operator Task<TResult>(AsyncResultCompletedSynchronously<TResult> @this) => @this._task;

        public object? AsyncState => state;

        public System.Threading.WaitHandle AsyncWaitHandle => (_task as IAsyncResult).AsyncWaitHandle;

        public bool CompletedSynchronously => true;

        public bool IsCompleted => true;
    }
}