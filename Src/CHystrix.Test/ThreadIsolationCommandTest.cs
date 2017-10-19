using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using CHystrix.Utils;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CHystrix.Test
{

    internal static class CommandExt
    {
        internal static int GetRollingCount(this ICommandMetrics metrics, CommandExecutionEventEnum status)
        {
            var rtn = 0;
            var dic = metrics.GetExecutionEventDistribution();

            dic.TryGetValue(status, out rtn);

            return rtn;

        }
    }

    [TestClass]
    public class ThreadIsolationCommandTest
    {
        [TestMethod]
        public void TestExecutionSuccess()
        {
            var command = new Async.SuccessfulTestCommand();


            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            
            Assert.AreEqual(true, command.RunAsync().Result);

            Thread.Sleep(command.ConfigSet.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));


        }


        [TestMethod]
        public void TestExecutionShortCircuted()
        {
            var command = new Async.SuccessfulTestCommand();


            command.ConfigSetForTest.CircuitBreakerEnabled = true;

            command.ConfigSet.CircuitBreakerForceOpen = true;

            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));

            Assert.AreEqual(false, command.RunAsync().Result);

            Thread.Sleep(command.ConfigSet.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));

        }

        [TestMethod]
        public void TestExecutionFailureWithFallback()
        {
            var command = new Async.FaultRuningCommand();

            Assert.AreEqual(command.GetFallback(), command.RunAsync().Result);
        }


        [TestMethod]
        public void TestExecutionTimeoutWithFallback()
        {
            var timeOut = 1000;
            var exeTime = 3000;

            var command = new Async.LongRuningCommand(exeTime, "TestExecutionTimeoutWithFallback");

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();
            pool.WorkItemTimeoutMiliseconds = timeOut;

            Trace.WriteLine("set timeout finish");

            Trace.WriteLine("pool maxthread:"+pool.MaxConcurrentCount);
            Trace.WriteLine("pool running task count:" + pool.NowRunningWorkCount);
            Trace.WriteLine("pool waiting task count:" + pool.NowWaitingWorkCount);
            Assert.AreEqual(command.GetFallback(), command.RunAsync().Result);
            Trace.WriteLine("check whether fire timeout behavior");

            timeOut = 3100;

            pool.WorkItemTimeoutMiliseconds = timeOut;

            Trace.WriteLine("pool maxthread:" + pool.MaxConcurrentCount);
            Trace.WriteLine("pool running task count:" + pool.NowRunningWorkCount);
            Trace.WriteLine("pool waiting task count:" + pool.NowWaitingWorkCount);

            var result = command.RunAsync().Result;

            Assert.AreNotEqual(command.GetFallback(), result);

        }

        [TestMethod]
        public void TestTimedOutCommandDoesNotExecute()
        {

            var timeOut = 1000;
            var exeTime = 3000;

            var command = new Async.LongRuningCommand(exeTime, "TestExecutionTimeoutWithFallback");

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();
            pool.WorkItemTimeoutMiliseconds = timeOut;

            Trace.WriteLine("set timeout finish");


            Trace.WriteLine("pool maxthread:" + pool.MaxConcurrentCount);
            Trace.WriteLine("pool running task count:" + pool.NowRunningWorkCount);
            Trace.WriteLine("pool waiting task count:" + pool.NowWaitingWorkCount);
            Assert.AreEqual(command.GetFallback(), command.RunAsync().Result);
            Trace.WriteLine("check whether fire timeout behavior");

            timeOut = 3100;

            pool.WorkItemTimeoutMiliseconds = timeOut;

            Assert.AreNotEqual(command.GetFallback(), command.RunAsync().Result);

        }


        [TestMethod]
        public void TestUpdateCommandMaxConcurrentCount()
        {
            var command = new Async.LongRuningCommand(0);
            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();
            var maxConcurrentCount = 10;
            command.ConfigSet.CommandMaxConcurrentCount = maxConcurrentCount;
            command.ConfigSet.RaiseConfigChangeEvent();
            Assert.AreEqual(maxConcurrentCount, pool.MaxConcurrentCount);

            maxConcurrentCount = 8;

            command.ConfigSet.CommandMaxConcurrentCount = maxConcurrentCount;
            command.ConfigSet.RaiseConfigChangeEvent();
            Assert.AreEqual(maxConcurrentCount, pool.MaxConcurrentCount);

        }

        [TestMethod]
        public void TestMultiExecute()
        {
            var timeOut = 30000;
            var exeTime = 50000;
            var maxConcurrentCount = 5;
            var totalTaskCount = 4;
            var command = new Async.LongRuningCommand(exeTime);

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();

            command.ConfigSet.CommandTimeoutInMilliseconds = timeOut;
            command.ConfigSet.CommandMaxConcurrentCount = maxConcurrentCount;

            command.ConfigSet.RaiseConfigChangeEvent();

            var tasks = new List<Task<string>>();

            for(int i = 0; i < totalTaskCount; i++)
			{
			    tasks.Add(command.RunAsync()); 
			}

            Assert.AreNotEqual(command.ConfigSet.CommandMaxConcurrentCount, command.Metrics.CurrentConcurrentExecutionCount);


            for (int i = 0; i < totalTaskCount; i++)
            {
                tasks.Add(command.RunAsync());
            }

            Thread.Sleep(100);

            Assert.AreEqual(command.ConfigSet.CommandMaxConcurrentCount, pool.NowRunningWorkCount);
            Assert.AreEqual(command.ConfigSet.CommandMaxConcurrentCount, command.Metrics.CurrentConcurrentExecutionCount);


        }
    }
}
