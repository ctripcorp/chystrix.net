using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace CHystrix.Threading
{
    #region delegate declare

    internal delegate void CWorkItemCompleteCallback(ICWorkItem workitem);

    #endregion

    internal sealed class CThreadPool : ICThreadPool
    {
        #region Fields

        ConcurrentDictionary<int, CThread> _threads;
        ConcurrentQueue<CThread> _idleThreads;
        int _nowRunningCount = 0;
        int _finishedCount = 0;
        int _timeoutCount = 0;

        ConcurrentQueue<ICWorkItem> _waitingTasks = new ConcurrentQueue<ICWorkItem>();

        volatile int _MaxConcurrentCount;
        volatile int _largestPoolSize = 0;
        const int _maxTryPopFailTimes = 3;
        CWorkItemCompleteCallback _ThreadWorkCompleteCallback;

        #endregion

        #region Construction
        public CThreadPool(int maxConcurrentCount)
        {
            _threads = new ConcurrentDictionary<int, CThread>();
            _MaxConcurrentCount = maxConcurrentCount;

            _idleThreads = new ConcurrentQueue<CThread>();

            _largestPoolSize = maxConcurrentCount;
        }


        public CThreadPool(int maxConcurrentCount, int workItemTimeoutMiliseconds)
            : this(maxConcurrentCount)
        {
            WorkItemTimeoutMiliseconds = workItemTimeoutMiliseconds;
        }


        public CThreadPool(int maxConcurrentCount, int workItemTimeoutMiliseconds, CWorkItemCompleteCallback workCompleteCallback)
            : this(maxConcurrentCount, workItemTimeoutMiliseconds)
        {
            _ThreadWorkCompleteCallback = workCompleteCallback;
        }

        #endregion

        #region public method

        public static void SetCThreadIdleTimeout(int seconds)
        {

            CThread.ThreadMaxIdleTime = TimeSpan.FromSeconds(seconds);
        }

        public void SetTimeoutWorkStatus()
        {
            if (WorkItemTimeoutMiliseconds > 0)
            {
                var tasks = _threads.Values.Select(x => x.NowTask)
                            .Concat(_waitingTasks.ToArray()).Where(x=>x!=null).ToArray();

                foreach (var item in tasks)
                {
                    if (!item.CanDo(WorkItemTimeoutMiliseconds) && item.MarkTimeout())
                    {
                        Interlocked.Increment(ref _timeoutCount);
                    }
                }

            }
        }

        ~CThreadPool()
        {
            Reset();
        }

        internal void Reset()
        {
            foreach (var item in _threads.Values)
            {
                item.Shutdown();
            }

            CThread worker;
            while (_idleThreads.TryDequeue(out worker))
            {
                worker.Shutdown();                
            }


            _threads.Clear();

            _waitingTasks = new ConcurrentQueue<ICWorkItem>();

            _nowRunningCount = 0;
            _finishedCount = 0;
            _timeoutCount = 0;

        }

        #endregion

        #region ICThreadPool

        public CWorkItem<T> QueueWorkItem<T>(Func<T> act)
        {
            return QueueWorkItem(act, null);
        }

        public CWorkItem<T> QueueWorkItem<T>(Func<T> act, EventHandler<StatusChangeEventArgs> onStatusChange=null)
        {

            var task = new CWorkItem<T>
            {
                StartTime = DateTime.Now,
                Action = act,
                Status = CTaskStatus.WaitingToRun

            };

            task.StatusChange = onStatusChange;

            _waitingTasks.Enqueue(task);

            NotifyThreadPoolOfPendingWork();

            if (_nowRunningCount > _largestPoolSize)
            {
                _largestPoolSize = _nowRunningCount;
            }

            return task;
        }

        public int LargestPoolSize
        {

            get
            {
                return _largestPoolSize;
            }
        }

        public int MaxConcurrentCount
        {
            set
            {
                if (_MaxConcurrentCount != value)
                {
                    var moreWorker = _MaxConcurrentCount < value;

                    _MaxConcurrentCount = value;

                    if (moreWorker)
                        NotifyThreadPoolOfPendingWork();
                }
            }
            get
            {
                return _MaxConcurrentCount;
            }
        }

        public int WorkItemTimeoutMiliseconds
        {
            set;
            get;
        }

        #endregion

        #region private methods

        private void OnWorkComplete(CThread thread, ICWorkItem task)
        {
            Interlocked.Increment(ref _finishedCount);

            ICWorkItem newTask;
            bool hasWorkDo = false;

            try
            {
                if (_ThreadWorkCompleteCallback != null)
                {
                    _ThreadWorkCompleteCallback(task);
                }
            }
            catch (Exception ex)
            {
                CHystrix.Utils.CommonUtils.Log.Log(LogLevelEnum.Error, ex.Message, ex);
            }

            if (_nowRunningCount + _idleThreads.Count > _MaxConcurrentCount)
            {
                thread.Shutdown();
            }

            if (!thread.IsShutdown)
            {
                while (_waitingTasks.TryDequeue(out newTask))
                {
                    if (newTask.CanDo(this.WorkItemTimeoutMiliseconds))
                    {
                        thread.DoWork(newTask);
                        hasWorkDo = true;
                        break;
                    }
                    else if(newTask.MarkTimeout())
                    {
                        Interlocked.Increment(ref _timeoutCount);
                    }

                }
            }

            if (!hasWorkDo)
            {
                CThread tmp;
                if (_threads.TryRemove(thread.ThreadID, out tmp))
                {
                    Interlocked.Decrement(ref _nowRunningCount);
                    _idleThreads.Enqueue(thread);
                }


            }
        }

        private void NotifyThreadPoolOfPendingWork()
        {

            if (_idleThreads.Count == 0 && _nowRunningCount >= this.MaxConcurrentCount)
                return;

            var failPopTimes = 0;

            while (_waitingTasks.Count > 0 && _idleThreads.Count > 0 && failPopTimes < _maxTryPopFailTimes)
            {

                CThread worker;

                if (!_idleThreads.TryDequeue(out worker))
                {
                    Thread.Sleep(10);
                    failPopTimes++;
                    continue;
                }

                if (_nowRunningCount + _idleThreads.Count > this.MaxConcurrentCount)
                {
                    worker.Shutdown();
                    continue;
                }

                if (worker.IsShutdown)
                {
                    continue;
                }

                ICWorkItem task;
                var hasWorkDo = false;
                while (_waitingTasks.TryDequeue(out task))
                {
                    if (task.CanDo(this.WorkItemTimeoutMiliseconds))
                    {
                        worker.DoWork(task);
                        hasWorkDo = true;
                        Interlocked.Increment(ref _nowRunningCount);

                        _threads.TryAdd(worker.ThreadID, worker);

                        break;
                    }
                    else if(task.MarkTimeout())
                    {
                        Interlocked.Increment(ref _timeoutCount);

                    }

                }

                if (!hasWorkDo)
                {
                    _idleThreads.Enqueue(worker);
                    break;
                }

            }


            while (_waitingTasks.Count > 0 && _nowRunningCount + _idleThreads.Count < this.MaxConcurrentCount)
            {

                ICWorkItem task;
                if (_waitingTasks.TryDequeue(out task))
                {
                    if (task.CanDo(this.WorkItemTimeoutMiliseconds))
                    {
                        CThread thread = new CThread(OnWorkComplete);
                        thread.DoWork(task);
                        Interlocked.Increment(ref _nowRunningCount);

                        _threads.TryAdd(thread.ThreadID, thread);
                    }
                    else if (task.MarkTimeout())
                    {
                        Interlocked.Increment(ref _timeoutCount);

                    }

                }

            }
        }

        #endregion

        #region delegates

        public CWorkItemCompleteCallback ThreadWorkCompleteCallback
        {
            set
            {

                _ThreadWorkCompleteCallback = value;
            }
        }

        #endregion

        #region Properties

        public int PoolThreadCount
        {
            get { return _threads.Count; }
        }

        public int IdleThreadCount
        {
            get { return _idleThreads.Count(x => !x.IsShutdown); }
        }

        public int CurrentPoolSize
        {
            get
            {
                return PoolThreadCount + IdleThreadCount;
            }
        }

        public int CurrentTaskCount
        {

            get
            {
                return NowRunningWorkCount + NowWaitingWorkCount + FinishedWorkCount;
            }
        }

        public int NowRunningWorkCount
        {

            get { return _nowRunningCount; }
        }

        public int NowWaitingWorkCount
        {
            get { return _waitingTasks.Count; }
        }

        public int FinishedWorkCount
        {
            get { return _finishedCount; }
        }

        public int TimeoutWorkCount
        {
            get { return _timeoutCount; }
        }

        #endregion


    }
}
