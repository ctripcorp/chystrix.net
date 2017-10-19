using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CHystrix.Threading
{

    #region enums

    internal enum CTaskStatus
    {
        WaitingToRun=0, RanToCompletion=1, Running=2,Faulted=3, Canceled=4
    }

    #endregion

    #region eventargs

    internal class StatusChangeEventArgs: EventArgs
    {
        CTaskStatus _status;
        internal StatusChangeEventArgs(CTaskStatus status)
        {
            _status = status;
        }

        public CTaskStatus Status
        {
            get
            {
                return _status;
            }
        }
    }

    #endregion

    internal sealed class CWorkItem<T>:ICWorkItem
    {
        #region Fields

        private T _result;
        int _realExeMilliseconds = 0;
        int _exeMilliseconds = 0;
        /// <summary>
        /// use this for make sure marking the task only once.
        /// </summary>
        int _isMarkTimeoutStatus = 0;

        CTaskStatus _Status;

        #endregion

        #region Properties

        internal DateTime StartTime { set; get; }

        internal DateTime EndTime { set; get; }

        internal DateTime RealStartTime { set; get; }

      //  internal DateTime RealEndTime { set; get; }
        public Func<T> Action { set; get; }

        public int WorkerThreadID { internal set; get; }


        public T Result
        {
            get
            {
                if (IsCompleted || IsCanceled)
                {
                    return _result;
                }
                else
                {
                    this.Wait();
                    return _result;
                }

            }
        }

        #endregion

        #region status change delegate

        internal EventHandler<StatusChangeEventArgs> StatusChange=null;

        #endregion

        #region ICWorkItem

        public Exception Exception { get; internal set; }

        public bool Wait(int waitMilliseconds = Timeout.Infinite)
        {
            int alreadyWait = 0;
            int waitUnit = 100;

            try
            {
                while (alreadyWait < waitMilliseconds || waitMilliseconds == Timeout.Infinite)
                {

                    if (this.IsCompleted || this.IsCanceled)
                    {

                        return true;
                    }

                    Thread.Sleep(waitUnit);
                    if (waitUnit < 1000)
                    {
                        waitUnit += 100;
                    }
                    alreadyWait += waitUnit;

                }
            }
            catch (Exception ex)
            {
                this.IsCompleted = true;
                this.Status = CTaskStatus.Faulted;
                this.IsFaulted = true;
                this.Exception = ex;

                return false;

            }

            this.Status = CTaskStatus.Canceled;
            this.IsCanceled = true;
            this.EndTime = DateTime.Now;

            return false;

        }


        public bool CanDo(int timeout)
        {
            if (IsCompleted || IsCanceled)
                return false;
            else if (timeout > 0)
            {
                if (timeout > ExeuteMilliseconds)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }


       /// <summary>
       /// Sets the current task for the time-out State
       /// </summary>
       ///<Returns> [true] said the set state, [false] indicates status already being seted </returns>
        public bool MarkTimeout()
        {
            if (Interlocked.CompareExchange(ref _isMarkTimeoutStatus, 1, 0) == 1)
            {

                return false;
            }
            else
            {
                this.Status = CTaskStatus.Canceled;
                this.IsCanceled = true;
                this.EndTime = DateTime.Now;

                return true;
            }
               
        }


        public void Do()
        {
            WorkerThreadID = Thread.CurrentThread.ManagedThreadId;
            this.RealStartTime = DateTime.Now;
            Status = CTaskStatus.Running;
            CTaskStatus _tmpStatus=Status;
            try
            {
                _result = Action();
                _tmpStatus = CTaskStatus.RanToCompletion;

            }
            catch (Exception ex)
            {
                _tmpStatus = CTaskStatus.Faulted;
                this.IsFaulted = true;
                this.Exception = ex;

            }
            finally
            {
                IsCompleted = true;
                EndTime = DateTime.Now;
                Status = _tmpStatus;
            }
        }

        public int ExeuteMilliseconds
        {
            get
            {

                if (IsCompleted || IsCanceled)
                {
                    if (0 == _exeMilliseconds)
                        _exeMilliseconds = (int)(EndTime - StartTime).TotalMilliseconds;
                    return _exeMilliseconds;
                }
                else
                {
                    return (int)(DateTime.Now - StartTime).TotalMilliseconds;
                }
            }
        }

        public int RealExecuteMilliseconds
        {
            get
            {

                if (IsCompleted || IsCanceled)
                {
                    if (0 == _realExeMilliseconds)
                        _realExeMilliseconds = (int)(EndTime - RealStartTime).TotalMilliseconds;

                    return _realExeMilliseconds;
                }
                else
                {
                    return (int)(DateTime.Now - StartTime).TotalMilliseconds;
                }
            }

        }

        public bool IsCompleted {private set; get; }

        public bool IsCanceled { private set; get; }

        public bool IsFaulted { private set; get; }

        public CTaskStatus Status {
            
            internal set{

                _Status = value;

                if (StatusChange != null)
                {
                    StatusChange(this, new StatusChangeEventArgs(value));
                }

            
            }
            get
            {
                return _Status;
            }
        }

        #endregion

    }
}
