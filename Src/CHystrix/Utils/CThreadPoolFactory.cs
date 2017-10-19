// -----------------------------------------------------------------------
// <copyright file="CThreadPoolFactory.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace CHystrix.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Collections.Concurrent;
    using CHystrix.Threading;
    using System.Threading;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    internal static class CThreadPoolFactory
    {

        static ConcurrentDictionary<string, CThreadPool> _pools = new ConcurrentDictionary<string, CThreadPool>();

        static System.Threading.Timer _timer;

        static CThreadPoolFactory()
        {
            _timer = new Timer((o) =>
            {

                foreach (var item in _pools.Values)
                {
                    item.SetTimeoutWorkStatus();
                }

            }, null, 0, 1000);

        }

        internal static ConcurrentDictionary<string, CThreadPool> AllPools
        {
            get
            {
                return _pools;
            }
        }

        internal static void ResetThreadPoolByCommandKey(string key)
        {
            CThreadPool pool;
            if (_pools.TryGetValue(key, out pool))
            {
                pool.Reset();
            }

        }

        internal static CThreadPool GetCommandPool(HystrixCommandBase command)
        {

            return _pools.GetOrAdd(command.Key.ToLower(),
                new CThreadPool(command.ConfigSet.CommandMaxConcurrentCount
                    , command.ConfigSet.CommandTimeoutInMilliseconds));

        }

        internal static CThreadPool GetPoolByKey(string key)
        {
            CThreadPool pool = null;
            _pools.TryGetValue(key.ToLower(), out pool);

            return pool;

        }

        public static CWorkItem<T> QueueWorkItem<T>(this ThreadIsolationCommand<T> command, Func<T> func,EventHandler<StatusChangeEventArgs> onStatusChange=null)
        {
            var pool = GetCommandPool(command);

            //if (pool.NowRunningWorkCount >= pool.MaxConcurrentCount)
            //{
            //    command.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ThreadMaxActive);
            //}

            //when asyn command's count exceed thread pool's volume, only allow some percentage (reference pool volume) task queue.
            if (pool.NowWaitingWorkCount >= pool.MaxConcurrentCount * command.ConfigSet.MaxAsyncCommandExceedPercentage / 100)
            {

                throw new HystrixException(FailureTypeEnum.ThreadIsolationRejected, command.GetType(), command.Key,
                        "already exceed the max workitem, can't add any more.");
                    //new ExecutionRejectedException("already exceed the max workitem, can't add any more.");
            }

            //command.Metrics.MarkExecutionEvent(CommandExecutionEventEnum.ThreadExecution);

            return pool.QueueWorkItem(func,onStatusChange);
        }

        public static void UpdateMaxConcurrentCount<T>(this ThreadIsolationCommand<T> command, int count)
        {
            GetCommandPool(command).MaxConcurrentCount = count;

        }

        public static void UpdateCommandTimeoutInMilliseconds<T>(this ThreadIsolationCommand<T> command, int seconds)
        {
            GetCommandPool(command).WorkItemTimeoutMiliseconds = seconds;

        }




    }
}
