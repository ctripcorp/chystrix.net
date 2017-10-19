using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CHystrix.Threading
{

    internal delegate void CThreadWorkCompleteCallback(CThread thread, ICWorkItem workItem);

    internal sealed class CThread
    {
        #region Fields

        private ICWorkItem _task=null;

        private Thread _thread=null;

        private ManualResetEvent _newJobWaitHandle;


        private volatile bool _isIdle = true;

        

        private CThreadWorkCompleteCallback _completeCallback;

        private bool _isShutdown = false;

        private int _threadID = -1;

        bool _isStart = false;

        #endregion

        #region static properties

        public static TimeSpan ThreadMaxIdleTime = TimeSpan.FromMinutes(5);

        #endregion

        #region Construction

        public CThread(CThreadWorkCompleteCallback completeCallback)
        {
            _newJobWaitHandle = new ManualResetEvent(false);

           
            _completeCallback = completeCallback;

            _thread = new Thread(DoTask);
            _thread.IsBackground = true;
            _threadID = _thread.ManagedThreadId;


        }

        #endregion

        #region Properties

        public int ThreadID
        {

            get
            {
                return _threadID;
            }
        }


        public bool DoWork(ICWorkItem task)
        {

            if (_isIdle && null == _task)
            {
                _task = task;
                if (!_isStart)
                {

                    _isStart = true;
                    _thread.Start();

                }
                else
                {
                    _newJobWaitHandle.Set();
                }

                return true;
              
     
            }
            else
            {

                return false;
            }


        }

        public ICWorkItem NowTask
        {
            get
            {
                return _task;
            }
        }

        public bool IsIdle
        {
            get { return _isIdle; }
        }

        public bool IsShutdown
        {
            get
            {
                return _isShutdown;
            }
        }

        #endregion

        #region Methods

        void DoTask()
        {

            while (!_isShutdown)
            {
              
                _isIdle = false;

                try
                {
                    while (_task != null)
                    {
                        ICWorkItem task;
                        lock (_task)
                        {
                            task = _task;

                            task.Do();
                        }
                       
                        _task = null;

                        _isIdle = true;
                        _completeCallback(this, task);

                    }
                }catch(Exception ex){

                    CHystrix.Utils.CommonUtils.Log.Log(LogLevelEnum.Error, ex.Message, ex);
                }

                if (!_isShutdown)
                {
                    _newJobWaitHandle.Reset();

                    if (!_newJobWaitHandle.WaitOne(ThreadMaxIdleTime) && _task==null)
                    {

                        _isShutdown = true;
                        _isIdle = false;
                        _thread = null;
                    }
                }
            }
        }

        public void Shutdown()
        {
            _isShutdown = true;
            _isIdle = false;
            _thread = null;
            _newJobWaitHandle.Set();
        }

        #endregion

    } 

}
