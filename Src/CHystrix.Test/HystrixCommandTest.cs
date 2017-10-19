using System;
using System.Threading;
using CHystrix.CircuitBreaker;
using CHystrix;
using CHystrix.Utils.Atomic;
using CHystrix.Utils;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CHystrix.Metrics;
using CHystrix.Config;
using System.Diagnostics;
using CHystrix.Test.Commands;
using System.Threading.Tasks;
using CHystrix.Threading;
using System.Collections.Concurrent;

namespace CHystrix.Test
{
        
    public static class CommandEx
    {

        public static bool IsExecutionComplete(this HystrixCommandBase command)
        {

            return command.Status == CommandStatusEnum.Failed ||
                command.Status == CommandStatusEnum.Rejected ||
                 command.Status == CommandStatusEnum.ShortCircuited ||
                  command.Status == CommandStatusEnum.Success ||
                   command.Status == CommandStatusEnum.Timeout;
        }

        public static bool IsSuccessfulExecution(this HystrixCommandBase command)
        {

            return command.Status == CommandStatusEnum.Success;
        }

        public static bool IsFailedExecution(this HystrixCommandBase command)
        {

            return command.Status != CommandStatusEnum.Success;
        }

        public static long GetExecutionTimeInMilliseconds(this HystrixCommandBase command)
        {

            return command.Metrics.GetAverageTotalExecutionLatency();
        }

        public static bool IsResponseFromFallback(this HystrixCommandBase command)
        {

            return command.Status == CommandStatusEnum.FallbackSuccess;
        }

        public static bool IsCircuitBreakerOpen(this HystrixCommandBase command)
        {

            return command.CircuitBreaker.IsOpen();
        }

        public static bool IsResponseShortCircuited(this HystrixCommandBase command)
        {

            return command.Status == CommandStatusEnum.ShortCircuited;
        }


    }


    [TestClass]
    public class HystrixCommandTest
    {

        /// <summary>
        /// Test a successful command execution.
        /// </summary>
        [TestMethod]
        public void TestExecutionSuccess()
        {

            var command = new SuccessfulTestCommand();
            command.CircuitBreaker.MarkSuccess();
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(true, command.Run());
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));



            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.BadRequest));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Rejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(0, command.Metrics.GetErrorPercentage());



        }

        /// <summary>
        /// Test that a command can not be executed multiple times.
        /// </summary>
        [TestMethod]
        public void TestExecutionMultipleTimes()
        {
            var command = new SuccessfulTestCommand();
            command.CircuitBreaker.MarkSuccess();

            Assert.IsFalse(command.IsExecutionComplete());
            // first should succeed
            Assert.AreEqual(true, command.Run());
            Assert.IsTrue(command.IsExecutionComplete());
            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsSuccessfulExecution());
            try
            {
                // second should Assert.Fail
                command.Run();
                Assert.Fail("we should not allow this ... it breaks the state of request logs");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                // we want to get here
            }


        }

        /// <summary>
        /// Test a command execution that throws an HystrixException and didn't implement getFallback.
        /// </summary>
        [TestMethod]
        public void TestExecutionKnownFailureWithNoFallback()
        {
            //TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            var command = new KnownFailureTestCommandWithoutFallback();
            command.CircuitBreaker.MarkSuccess();

            command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds = 1;
            try
            {
                command.Run();
                Assert.Fail("we shouldn't get here");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                Thread.Sleep(command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds);
                Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
                Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
                Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
                Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            }

            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsFailedExecution());

            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

        }

        /// <summary>
        /// Test a command execution that throws an unknown exception (not HystrixException) and didn't implement getFallback.
        /// </summary>
        [TestMethod]
        public void TestExecutionUnknownFailureWithNoFallback()
        {
            var command = new UnknownFailureTestCommandWithoutFallback();
            command.CircuitBreaker.MarkSuccess();

            command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds = 1;
            try
            {
                command.Run();
                Assert.Fail("we shouldn't get here");
            }
            catch (HystrixException e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                Assert.IsNotNull(e.FallbackException);
                Assert.IsNotNull(e.CommandType);
                Assert.Fail("We should always get an HystrixException when an error occurs.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
               
            }
            Thread.Sleep(command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsFailedExecution());

            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

        /// <summary>
        /// Test a command execution that Assert.Fails but has a fallback.
        /// </summary>
        [TestMethod]
        public void TestExecutionFailureWithFallback()
        {
            var command = new KnownFailureTestCommandWithFallback();
            command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds = 1;
            command.CircuitBreaker.MarkSuccess();

            try
            {
                Assert.AreEqual(false, command.Run());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                Assert.Fail("We should have received a response from the fallback.");
            }

            //Assert.AreEqual("we Assert.Failed with a simulated issue", command.IsFailedExecution()Exception.Message);
            Thread.Sleep(command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsFailedExecution());

            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

        /// <summary>
        /// Test a command execution that Assert.Fails, has getFallback implemented but that Assert.Fails as well.
        /// </summary>
        [TestMethod]
        public void TestExecutionFailureWithFallbackFailure()
        {
            var command = new KnownFailureTestCommandWithFallbackFailure();
            command.CircuitBreaker.MarkSuccess();

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();

            try
            {
                command.Run();
                Assert.Fail("we shouldn't get here");
            }
            catch (Exception e)
            {
                Console.WriteLine("------------------------------------------------");
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                Console.WriteLine("------------------------------------------------");
                //Assert.IsNotNull(e.FallbackException);
            }
            Thread.Sleep(command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsFailedExecution());

            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

        /// <summary>
        /// Test a successful command execution (asynchronously).
        /// </summary>
        [TestMethod]
        public void TestQueueSuccess()
        {
            var command = new Async.SuccessfulTestCommand();
            command.CircuitBreaker.MarkSuccess();

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();

            try
            {
                var  future = command.RunAsync();
                Assert.AreEqual(true, future.Result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                Assert.Fail("We received an exception.");
            }

            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsSuccessfulExecution());

            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(0, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

        /// <summary>
        /// Test a command execution (asynchronously) that throws an HystrixException and didn't implement getFallback.
        /// </summary>
        [TestMethod]
        public void TestQueueKnownFailureWithNoFallback()
        {
            var command = new Async.KnownFailureTestCommandWithoutFallback();

            command.ConfigSet.CommandTimeoutInMilliseconds = 1000;

            command.ConfigSet.RaiseConfigChangeEvent();

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();

            Console.WriteLine(pool.WorkItemTimeoutMiliseconds);

            try
            {
                 var a =command.RunAsync().Result;
                Assert.Fail("we shouldn't get here");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                
            }

            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsFailedExecution());

            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

        /// <summary>
        /// Test a command execution (asynchronously) that throws an unknown exception (not HystrixException) and didn't implement getFallback.
        /// </summary>
        [TestMethod]
        public void TestQueueUnknownFailureWithNoFallback()
        {
            var command = new Async.UnknownFailureTestCommandWithoutFallback();
            command.ConfigSet.CommandTimeoutInMilliseconds = 1000;

            command.ConfigSet.RaiseConfigChangeEvent();

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();

            try
            {
                command.RunAsync().Wait();
                Assert.Fail("we shouldn't get here");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                //if (e.InnerException is HystrixException)
                //{
                //    HystrixException de = (HystrixException)e.InnerException;
                //    Assert.IsNotNull(de.FallbackException);
                //    Assert.IsNotNull(de.CommandType);
                //}
                //else
                //{
                //    Assert.Fail("the cause should be HystrixException");
                //}
            }
            Thread.Sleep(command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsFailedExecution());

            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

        /// <summary>
        /// Test a command execution (asynchronously) that Assert.Fails but has a fallback.
        /// </summary>
        [TestMethod]
        public void TestQueueFailureWithFallback()
        {
            var command = new  Async.KnownFailureTestCommandWithFallback();

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();

            try
            {
                Assert.AreEqual(false, command.RunAsync().Result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                Assert.Fail("We should have received a response from the fallback.");
            }

            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsFailedExecution());
                
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }



        /// <summary>
        /// Test a command execution (asynchronously) that Assert.Fails, has getFallback implemented but that Assert.Fails as well.
        /// </summary>
        [TestMethod]
        public void TestQueueFailureWithFallbackFailure()
        {
            var command = new  Async.KnownFailureTestCommandWithFallbackFailure();

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();


            try
            {
                command.RunAsync().Wait();
                Assert.Fail("we shouldn't get here");
            }
            catch (Exception e)
            {
                if (!(e is AggregateException))
                {
                    Assert.Fail("the cause should be HystrixException");
                }
            }

            Thread.Sleep(command.ConfigSet.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
            Assert.IsTrue(command.IsFailedExecution());

            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));

            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

        /// <summary>
        /// Test that the circuit-breaker will 'trip' and prevent command execution on subsequent calls.
        /// </summary>
        public void TestCircuitBreakerTripsAfterFailures()
        {
            /* Assert.Fail 3 times and then it should trip the circuit and stop executing */
            // Assert.Failure 1

            var attempt1 = new KnownFailureTestCommandWithFallback();
            attempt1.ConfigSet.CircuitBreakerErrorThresholdPercentage = 50;
            attempt1.ConfigSetForTest.CircuitBreakerSleepWindowInMilliseconds = 100;
            //attempt1.ConfigSet.

            attempt1.Run();

            Assert.IsTrue(attempt1.IsResponseFromFallback());
            Assert.IsFalse(attempt1.IsCircuitBreakerOpen());
            Assert.IsFalse(attempt1.IsResponseShortCircuited());

            // Assert.Failure 2
            var attempt2 = new KnownFailureTestCommandWithFallback();
            attempt2.Run();
            Assert.IsTrue(attempt2.IsResponseFromFallback());
            Assert.IsFalse(attempt2.IsCircuitBreakerOpen());
            Assert.IsFalse(attempt2.IsResponseShortCircuited());

            // Assert.Failure 3
            var attempt3 = new KnownFailureTestCommandWithFallback();
            attempt3.Run();
            Assert.IsTrue(attempt3.IsResponseFromFallback());
            Assert.IsFalse(attempt3.IsResponseShortCircuited());
            // it should now be 'open' and prevent further executions
            Assert.IsTrue(attempt3.IsCircuitBreakerOpen());

            // attempt 4
            KnownFailureTestCommandWithFallback attempt4 = new KnownFailureTestCommandWithFallback();
            attempt4.Run();
            Assert.IsTrue(attempt4.IsResponseFromFallback());
            // this should now be true as the response will be short-circuited
            Assert.IsTrue(attempt4.IsResponseShortCircuited());
            // this should remain open
            Assert.IsTrue(attempt4.IsCircuitBreakerOpen());

            Assert.AreEqual(0, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(3, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(4, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, attempt1.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(1, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, attempt1.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, attempt1.Metrics.GetErrorPercentage());

            //Assert.AreEqual(4, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

        /// <summary>
        /// Test that the circuit-breaker will 'trip' and prevent command execution on subsequent calls.
        /// </summary>
        public void TestCircuitBreakerTripsAfterFailuresViaQueue()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            try
            {
                /* Assert.Fail 3 times and then it should trip the circuit and stop executing */
                // Assert.Failure 1
                Async.KnownFailureTestCommandWithFallback attempt1 = new Async.KnownFailureTestCommandWithFallback();
                attempt1.RunAsync().Wait();
                Assert.IsTrue(attempt1.IsResponseFromFallback());
                Assert.IsFalse(attempt1.IsCircuitBreakerOpen());
                Assert.IsFalse(attempt1.IsResponseShortCircuited());

                // Assert.Failure 2
                Async.KnownFailureTestCommandWithFallback attempt2 = new Async.KnownFailureTestCommandWithFallback();
                attempt2.RunAsync().Wait();
                Assert.IsTrue(attempt2.IsResponseFromFallback());
                Assert.IsFalse(attempt2.IsCircuitBreakerOpen());
                Assert.IsFalse(attempt2.IsResponseShortCircuited());

                // Assert.Failure 3
                Async.KnownFailureTestCommandWithFallback attempt3 = new Async.KnownFailureTestCommandWithFallback();
                attempt3.RunAsync().Wait();
                Assert.IsTrue(attempt3.IsResponseFromFallback());
                Assert.IsFalse(attempt3.IsResponseShortCircuited());
                // it should now be 'open' and prevent further executions
                Assert.IsTrue(attempt3.IsCircuitBreakerOpen());

                // attempt 4
                Async.KnownFailureTestCommandWithFallback attempt4 = new Async.KnownFailureTestCommandWithFallback();
                attempt4.RunAsync().Wait();
                Assert.IsTrue(attempt4.IsResponseFromFallback());
                // this should now be true as the response will be short-circuited
                Assert.IsTrue(attempt4.IsResponseShortCircuited());
                // this should remain open
                Assert.IsTrue(attempt4.IsCircuitBreakerOpen());

                Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
                Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
                Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
                Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
                Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
                Assert.AreEqual(4, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
                //Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
                Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
                //Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
                Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
                //Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

                Assert.AreEqual(100, circuitBreaker.Metrics.GetErrorPercentage());

                //Assert.AreEqual(4, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                Assert.Fail("We should have received fallbacks.");
            }
        }



        /// <summary>
        /// Test that the circuit-breaker is shared across HystrixCommand objects with the same CommandKey.
        /// <para>
        /// This will test HystrixCommand objects with a single circuit-breaker (as if each injected with same CommandKey)
        /// </para>
        /// <para>
        /// Multiple HystrixCommand objects with the same dependency use the same circuit-breaker.
        /// </para>
        /// </summary>

        public void TestCircuitBreakerAcrossMultipleCommandsButSameCircuitBreaker()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            /* Assert.Fail 3 times and then it should trip the circuit and stop executing */
            // Assert.Failure 1
            KnownFailureTestCommandWithFallback attempt1 = new KnownFailureTestCommandWithFallback();
            attempt1.Run();
            Assert.IsTrue(attempt1.IsResponseFromFallback());
            Assert.IsFalse(attempt1.IsCircuitBreakerOpen());
            Assert.IsFalse(attempt1.IsResponseShortCircuited());

            // Assert.Failure 2 with a different command, same circuit breaker
            KnownFailureTestCommandWithoutFallback attempt2 = new KnownFailureTestCommandWithoutFallback();
            try
            {
                attempt2.Run();
            }
            catch (Exception)
            {
                // ignore ... this doesn't have a fallback so will throw an exception
            }
            Assert.IsTrue(attempt2.IsFailedExecution());
            Assert.IsFalse(attempt2.IsResponseFromFallback()); // false because no fallback
            Assert.IsFalse(attempt2.IsCircuitBreakerOpen());
            Assert.IsFalse(attempt2.IsResponseShortCircuited());

            // Assert.Failure 3 of the Hystrix, 2nd for this particular HystrixCommand
            KnownFailureTestCommandWithFallback attempt3 = new KnownFailureTestCommandWithFallback();
            attempt3.Run();
            Assert.IsTrue(attempt2.IsFailedExecution());
            Assert.IsTrue(attempt3.IsResponseFromFallback());
            Assert.IsFalse(attempt3.IsResponseShortCircuited());

            // it should now be 'open' and prevent further executions
            // after having 3 Assert.Failures on the Hystrix that these 2 different HystrixCommand objects are for
            Assert.IsTrue(attempt3.IsCircuitBreakerOpen());

            // attempt 4
            KnownFailureTestCommandWithFallback attempt4 = new KnownFailureTestCommandWithFallback();
            attempt4.Run();
            Assert.IsTrue(attempt4.IsResponseFromFallback());
            // this should now be true as the response will be short-circuited
            Assert.IsTrue(attempt4.IsResponseShortCircuited());
            // this should remain open
            Assert.IsTrue(attempt4.IsCircuitBreakerOpen());

            Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, circuitBreaker.Metrics.GetErrorPercentage());

            //Assert.AreEqual(4, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

        /// <summary>
        /// Test that the circuit-breaker is different between HystrixCommand objects with a different Hystrix.
        /// </summary>


        //public void testCircuitBreakerAcrossMultipleCommandsAndDifferentDependency()
        //{
        //    TestCircuitBreaker circuitBreaker_one = new TestCircuitBreaker();
        //    TestCircuitBreaker circuitBreaker_two = new TestCircuitBreaker();
        //    /* Assert.Fail 3 times, twice on one Hystrix, once on a different Hystrix ... circuit-breaker should NOT open */

        //     Assert.Failure 1
        //    KnownFailureTestCommandWithFallback attempt1 = new KnownFailureTestCommandWithFallback(circuitBreaker_one);
        //    attempt1.execute();
        //    Assert.IsTrue(attempt1.IsResponseFromFallback());
        //    Assert.IsFalse(attempt1.IsCircuitBreakerOpen());
        //    Assert.IsFalse(attempt1.IsResponseShortCircuited());

        //     Assert.Failure 2 with a different HystrixCommand implementation and different Hystrix
        //    KnownFailureTestCommandWithFallback attempt2 = new KnownFailureTestCommandWithFallback(circuitBreaker_two);
        //    attempt2.execute();
        //    Assert.IsTrue(attempt2.IsResponseFromFallback());
        //    Assert.IsFalse(attempt2.IsCircuitBreakerOpen());
        //    Assert.IsFalse(attempt2.IsResponseShortCircuited());

        //     Assert.Failure 3 but only 2nd of the Hystrix.ONE
        //    KnownFailureTestCommandWithFallback attempt3 = new KnownFailureTestCommandWithFallback(circuitBreaker_one);
        //    attempt3.execute();
        //    Assert.IsTrue(attempt3.IsResponseFromFallback());
        //    Assert.IsFalse(attempt3.IsResponseShortCircuited());

        //     it should remain 'closed' since we have only had 2 Assert.Failures on Hystrix.ONE
        //    Assert.IsFalse(attempt3.IsCircuitBreakerOpen());

        //     this one should also remain closed as it only had 1 Assert.Failure for Hystrix.TWO
        //    Assert.IsFalse(attempt2.IsCircuitBreakerOpen());

        //     attempt 4 (3rd attempt for Hystrix.ONE)
        //    KnownFailureTestCommandWithFallback attempt4 = new KnownFailureTestCommandWithFallback(circuitBreaker_one);
        //    attempt4.execute();
        //     this should NOW flip to true as this is the 3rd Assert.Failure for Hystrix.ONE
        //    Assert.IsTrue(attempt3.IsCircuitBreakerOpen());
        //    Assert.IsTrue(attempt3.IsResponseFromFallback());
        //    Assert.IsFalse(attempt3.IsResponseShortCircuited());

        //     Hystrix.TWO should still remain closed
        //    Assert.IsFalse(attempt2.IsCircuitBreakerOpen());

        //    Assert.AreEqual(0, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
        //    Assert.AreEqual(0, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
        //    Assert.AreEqual(3, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
        //    Assert.AreEqual(0, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
        //    Assert.AreEqual(0, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
        //    Assert.AreEqual(3, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
        //    Assert.AreEqual(0, circuitBreaker_one.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
        //    Assert.AreEqual(0, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
        //    Assert.AreEqual(0, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
        //    Assert.AreEqual(0, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
        //    Assert.AreEqual(0, circuitBreaker_one.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

        //    Assert.AreEqual(100, circuitBreaker_one.Metrics.GetErrorPercentage());

        //    Assert.AreEqual(0, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
        //    Assert.AreEqual(0, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
        //    Assert.AreEqual(1, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
        //    Assert.AreEqual(0, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
        //    Assert.AreEqual(0, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
        //    Assert.AreEqual(1, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
        //    Assert.AreEqual(0, circuitBreaker_two.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
        //    Assert.AreEqual(0, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
        //    Assert.AreEqual(0, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
        //    Assert.AreEqual(0, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
        //    Assert.AreEqual(0, circuitBreaker_two.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

        //    Assert.AreEqual(100, circuitBreaker_two.Metrics.GetErrorPercentage());

        //    Assert.AreEqual(4, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        //}

         ///<summary>
         ///Test that the circuit-breaker being disabled doesn't wreak havoc.
         ///</summary>
        [TestMethod]
        public void TestExecutionSuccessWithCircuitBreakerDisabled()
        {
            var command = new TestCommandWithoutCircuitBreaker();

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();


            try
            {
                Assert.AreEqual(true, command.Run());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
                Assert.Fail("We received an exception.");
            }

            // we'll still get metrics ... just not the circuit breaker opening/closing
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(0, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }

         ///<summary>
         ///Test a command execution timeout where the command didn't implement getFallback.
         ///</summary>
        [TestMethod]
        public void TestExecutionTimeoutWithNoFallback()
        {
            var command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_NOT_IMPLEMENTED);

            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();

            try
            {
                command.Run();
                Assert.Fail("we shouldn't get here");
            }
            catch (Exception e)
            {
                //if (e is HystrixException)
                //{
                //    HystrixException de = (HystrixException)e;
                //    Assert.IsNotNull(de.FallbackException);
                //    Assert.IsTrue(de.FallbackException is System.NotSupportedException);
                //    Assert.IsNotNull(de.CommandType);
                //    Assert.IsNotNull(de.InnerException);
                //    Assert.IsTrue(de.InnerException is TimeoutException);
                //}
                //else
                //{
                //    Assert.Fail("the exception should be HystrixException");
                //}
            }
           //  the time should be 50+ since we timeout at 50ms
            Trace.WriteLine("Execution Time is: " + command.GetExecutionTimeInMilliseconds());
            Assert.IsTrue(command.GetExecutionTimeInMilliseconds() >= 50);

            //Assert.IsTrue(command.ResponseTimedOut);
            Assert.IsFalse(command.IsResponseFromFallback());
            //Assert.IsFalse(command.ResponseRejected);

            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            //Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

            Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }



        ///// <summary>
        ///// Test a command execution timeout where the command implemented getFallback.
        ///// </summary>
        //[TestMethod]
        //public void testExecutionTimeoutWithFallback()
        //{
        //    var command = new TestCommandWithTimeout(2000, TestCommandWithTimeout.FALLBACK_SUCCESS);

        //    var pool = CThreadPoolFactory.GetCommandPool(command);

        //    pool.Reset();
        //    //pool.WorkItemTimeoutMiliseconds = 50;
        //    try
        //    {
        //        Assert.AreEqual(false, command.Run());
        //        // the time should be 50+ since we timeout at 50ms
        //        Trace.WriteLine("Execution Time is: " + command.GetExecutionTimeInMilliseconds());
        //        Assert.IsTrue(command.GetExecutionTimeInMilliseconds() >= 50);
        //        //Assert.IsTrue(command.ResponseTimedOut);
        //        Assert.IsTrue(command.IsResponseFromFallback());
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //        Console.Write(e.StackTrace);
        //        Assert.Fail("We should have received a response from the fallback.");
        //    }

        //    Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
        //    Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
        //    Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
        //    Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
        //    Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
        //    Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
        //    //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
        //    Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
        //    //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
        //    Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
        //    //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

        //    Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

        //    //Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        //}

        [TestMethod]
        public void TestMultiAsyncCommand()
        {
            var command1 = new Async.AsyncCommand("test");

            var pool = CThreadPoolFactory.GetCommandPool(command1);

            pool.Reset();

            var tasks = new List<Task<string>>();
            var items = new List<ICWorkItem>();
            var count = 100;

            for (int i = 0; i < count; i++)
            {

                var command = new Async.AsyncCommand("test"+i);

                tasks.Add(command.RunAsync());
                
            }


            var index=0;
            foreach (var item in tasks)
            {
                try
                {
                    Assert.AreEqual("test" + index, item.Result);
                    index++;
                }
                catch(Exception ex)
                {
                    Assert.Fail(ex.Message);
                }
                
            }


           

        }


        [TestMethod]
        public void TestMultiAsyncInThreadsCommand()
        {
            var command1 = new Async.AsyncCommand("test");

            var pool = CThreadPoolFactory.GetCommandPool(command1);

            pool.Reset();

            var tasks = new ConcurrentDictionary<string,Task<string>>();
            var items = new List<ICWorkItem>();
            var count = 100;


            Parallel.For(0, count, (i) =>
            {
                var command = new Async.AsyncCommand("test" + i);
                var newTask = command.RunAsync();
                
                tasks.TryAdd("test"+i, newTask);

            });


            foreach (var item in tasks)
            {
                Assert.IsTrue(item.Key == item.Value.Result || item.Value.Result == command1.GetFallback());
   
            }

        }

        /// <summary>
        /// Test when a command Assert.Fails to get queued up in the threadpool where the command implemented getFallback.
        /// <para>
        /// We specifically want to protect against developers getting random thread exceptions and instead just correctly receives a fallback.
        /// </para>
        /// </summary>
        [TestMethod]
        public void TestRejectedThreadWithFallback()
        {
            var command = new Async.LongRuningCommand(10000);
            var pool = CThreadPoolFactory.GetCommandPool(command);

            pool.Reset();

            command.ConfigSet.CommandMaxConcurrentCount = 2;

            command.ConfigSet.MaxAsyncCommandExceedPercentage = 50;
            command.ConfigSet.RaiseConfigChangeEvent();


            var c1 = command.RunAsync();
            var c2 = command.RunAsync();
            var c3 = command.RunAsync();
            var c4 = command.RunAsync();

            Thread.Sleep(command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Rejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));

            var c5 = command.RunAsync();

           //Assert.AreEqual(2, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Rejected));
            Thread.Sleep(command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
            Thread.Sleep(command.ConfigSetForTest.MetricsHealthSnapshotIntervalInMilliseconds);

            Assert.AreEqual(2, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
            Assert.AreEqual(2, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Rejected));
            Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
            //Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

           // Assert.AreEqual(100, command.Metrics.GetErrorPercentage());

            //Assert.AreEqual(2, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
        }


        [TestMethod]
        public void TestRejectedThreadWhenExceedMaxExceedPercentage()
        {
            var key = CommonUtils.GenerateTypeKey(typeof(Async.LongRuningCommand));
            CThreadPoolFactory.ResetThreadPoolByCommandKey(key);

            int maxConcurrent = 2;
            int maxExceedPercentage =50;
            int count =10;

            var command = new Async.LongRuningCommand(10);

            command.ConfigSet.CommandMaxConcurrentCount = maxConcurrent;

            command.ConfigSet.MaxAsyncCommandExceedPercentage = maxExceedPercentage;
            command.ConfigSet.RaiseConfigChangeEvent();



            for (int i = 0; i < count; i++)
            {
                var command1 = new Async.LongRuningCommand(10000);

                command1.RunAsync();

                if (i > maxConcurrent + (maxConcurrent * maxExceedPercentage / 100))
                {
                    Assert.AreEqual(command1.Status, CommandStatusEnum.FallbackSuccess);
                }
            }


        }


    }

    //
}


//
//		/// <summary>
//		/// Test a command execution timeout where the command implemented getFallback but it Assert.Fails.
//		/// </summary>
//
//
//		public void testExecutionTimeoutFallbackFailure()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_FAILURE);
//			try
//			{
//				command.Run();
//				Assert.Fail("we shouldn't get here");
//			}
//			catch (Exception e)
//			{
//				if (e is HystrixException)
//				{
//					HystrixException de = (HystrixException) e;
//					Assert.IsNotNull(de.FallbackException);
//					Assert.IsFalse(de.FallbackException is System.NotSupportedException);
//					Assert.IsNotNull(de.CommandType);
//					Assert.IsNotNull(de.InnerException);
//					Assert.IsTrue(de.InnerException is TimeoutException);
//				}
//				else
//				{
//					Assert.Fail("the exception should be HystrixException");
//				}
//			}
//			// the time should be 50+ since we timeout at 50ms
//			Assert.IsTrue("Execution Time is: " + command.GetExecutionTimeInMilliseconds(), command.GetExecutionTimeInMilliseconds() >= 50);
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test that the circuit-breaker counts a command execution timeout as a 'timeout' and not just Assert.Failure.
//		/// </summary>
//
//
//		public void testCircuitBreakerOnExecutionTimeout()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_SUCCESS);
//			try
//			{
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//
//				command.Run();
//
//				Assert.IsTrue(command.IsResponseFromFallback());
//				Assert.IsFalse(command.IsCircuitBreakerOpen());
//				Assert.IsFalse(command.IsResponseShortCircuited());
//
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//				Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We should have received a response from the fallback.");
//			}
//
//			Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(command.ResponseTimedOut);
//
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test that the command finishing AFTER a timeout (because thread continues in background) does not register a SUCCESS
//		/// </summary>
//
//
//		public void testCountersOnExecutionTimeout()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_SUCCESS);
//			try
//			{
//				command.Run();
//
//				/* wait long enough for the command to have finished */
//				Thread.Sleep(200);
//
//				/* response should still be the same as 'testCircuitBreakerOnExecutionTimeout' */
//				Assert.IsTrue(command.IsResponseFromFallback());
//				Assert.IsFalse(command.IsCircuitBreakerOpen());
//				Assert.IsFalse(command.IsResponseShortCircuited());
//
//				Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//				Assert.IsTrue(command.ResponseTimedOut);
//				Assert.IsFalse(command.IsSuccessfulExecution());
//
//				/* Assert.Failure and timeout count should be the same as 'testCircuitBreakerOnExecutionTimeout' */
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//				Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//
//				/* we should NOT have a 'success' counter */
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We should have received a response from the fallback.");
//			}
//
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test a queued command execution timeout where the command didn't implement getFallback.
//		/// <para>
//		/// We specifically want to protect against developers queuing commands and using queue().get() without a timeout (such as queue().get(3000, TimeUnit.Milliseconds)) and ending up blocking
//		/// indefinitely by skipping the timeout protection of the execute() command.
//		/// </para>
//		/// </summary>
//
//
//		public void testQueuedExecutionTimeoutWithNoFallback()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_NOT_IMPLEMENTED);
//			try
//			{
//				command.queue().get();
//				Assert.Fail("we shouldn't get here");
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				if (e is ExecutionException && e.InnerException is HystrixException)
//				{
//					HystrixException de = (HystrixException) e.InnerException;
//					Assert.IsNotNull(de.FallbackException);
//					Assert.IsTrue(de.FallbackException is System.NotSupportedException);
//					Assert.IsNotNull(de.CommandType);
//					Assert.IsNotNull(de.InnerException);
//					Assert.IsTrue(de.InnerException is TimeoutException);
//				}
//				else
//				{
//					Assert.Fail("the exception should be ExecutionException with cause as HystrixException");
//				}
//			}
//
//			Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(command.ResponseTimedOut);
//
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test a queued command execution timeout where the command implemented getFallback.
//		/// <para>
//		/// We specifically want to protect against developers queuing commands and using queue().get() without a timeout (such as queue().get(3000, TimeUnit.Milliseconds)) and ending up blocking
//		/// indefinitely by skipping the timeout protection of the execute() command.
//		/// </para>
//		/// </summary>
//
//
//		public void testQueuedExecutionTimeoutWithFallback()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_SUCCESS);
//			try
//			{
//				Assert.AreEqual(false, command.queue().get());
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We should have received a response from the fallback.");
//			}
//
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test a queued command execution timeout where the command implemented getFallback but it Assert.Fails.
//		/// <para>
//		/// We specifically want to protect against developers queuing commands and using queue().get() without a timeout (such as queue().get(3000, TimeUnit.Milliseconds)) and ending up blocking
//		/// indefinitely by skipping the timeout protection of the execute() command.
//		/// </para>
//		/// </summary>
//
//
//		public void testQueuedExecutionTimeoutFallbackFailure()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_FAILURE);
//			try
//			{
//				command.queue().get();
//				Assert.Fail("we shouldn't get here");
//			}
//			catch (Exception e)
//			{
//				if (e is ExecutionException && e.InnerException is HystrixException)
//				{
//					HystrixException de = (HystrixException) e.InnerException;
//					Assert.IsNotNull(de.FallbackException);
//					Assert.IsFalse(de.FallbackException is System.NotSupportedException);
//					Assert.IsNotNull(de.CommandType);
//					Assert.IsNotNull(de.InnerException);
//					Assert.IsTrue(de.InnerException is TimeoutException);
//				}
//				else
//				{
//					Assert.Fail("the exception should be ExecutionException with cause as HystrixException");
//				}
//			}
//
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}




/// <summary>
/// Test a successful command execution.
/// </summary>
//[TestMethod]
////public void TestObserveSuccess()
//{
//    try
//    {
//        var command = new SuccessfulTestCommand();
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//        //Assert.AreEqual(true, command.observe().toBlockingObservable().single());
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//        Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));

//        Assert.AreEqual(false, command.IsFailedExecution());

//        Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//        Assert.IsTrue(command.IsSuccessfulExecution());

//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//        Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));

//        Assert.AreEqual(0, command.Metrics.GetErrorPercentage());

//        Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());

//    }
//    catch (Exception e)
//    {
//        Console.WriteLine(e.ToString());
//        Console.Write(e.StackTrace);
//        Assert.Fail("We received an exception.");
//    }
//}

//

//

//
//		/// <summary>
//		/// Test a successful command execution.
//		/// </summary>
//
//
//
//		public void testObserveOnScheduler()
//		{
//			for (int i = 0; i < 5; i++)
//			{
//
//
//				AtomicReference<Thread> commandThread = new AtomicReference<Thread>();
//
//
//				AtomicReference<Thread> subscribeThread = new AtomicReference<Thread>();
//
//				TestHystrixCommand<bool?> command = new TestHystrixCommandAnonymousInnerClassHelper(this, TestHystrixCommand.testPropsBuilder(), commandThread);
//
//
//
//				CountDownLatch latch = new CountDownLatch(1);
//
//				command.toObservable(Schedulers.newThread()).subscribe(new ObserverAnonymousInnerClassHelper(this, subscribeThread, latch));
//
//				if (!latch.@await(2000, TimeUnit.MILLISECONDS))
//				{
//					Assert.Fail("timed out");
//				}
//
//				Assert.IsNotNull(commandThread.get());
//				Assert.IsNotNull(subscribeThread.get());
//
//				Console.WriteLine("HystrixCommand Thread: " + commandThread.get());
//				Console.WriteLine("Subscribe Thread: " + subscribeThread.get());
//
//				Assert.IsTrue(commandThread.get().Name.StartsWith("hystrix-"));
//				Assert.IsFalse(subscribeThread.get().Name.StartsWith("hystrix-"));
//				Assert.IsTrue(subscribeThread.get().Name.StartsWith("Rx"));
//			}
//		}
//
//		private class TestHystrixCommandAnonymousInnerClassHelper : TestHystrixCommand<bool?>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private AtomicReference<Thread> commandThread;
//
//			public TestHystrixCommandAnonymousInnerClassHelper(HystrixCommandTest outerInstance, com.netflix.hystrix.HystrixCommandTest.TestHystrixCommand.TestCommandBuilder testPropsBuilder, AtomicReference<Thread> commandThread) : base(testPropsBuilder)
//			{
//				this.outerInstance = outerInstance;
//				this.commandThread = commandThread;
//			}
//
//
//			protected internal override bool? run()
//			{
//				commandThread.set(Thread.CurrentThread);
//				return true;
//			}
//		}
//
//		private class ObserverAnonymousInnerClassHelper : Observer<bool?>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private AtomicReference<Thread> subscribeThread;
//			private CountDownLatch latch;
//
//			public ObserverAnonymousInnerClassHelper(HystrixCommandTest outerInstance, AtomicReference<Thread> subscribeThread, CountDownLatch latch)
//			{
//				this.outerInstance = outerInstance;
//				this.subscribeThread = subscribeThread;
//				this.latch = latch;
//			}
//
//
//			public override void onCompleted()
//			{
//				latch.countDown();
//
//			}
//
//			public override void onError(Exception e)
//			{
//				latch.countDown();
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//
//			}
//
//			public override void onNext(bool? args)
//			{
//				subscribeThread.set(Thread.CurrentThread);
//			}
//		}
//
//		/// <summary>
//		/// Test a successful command execution.
//		/// </summary>
//
//
//
//		public void testObserveOnComputationSchedulerByDefaultForThreadIsolation()
//		{
//
//
//
//			AtomicReference<Thread> commandThread = new AtomicReference<Thread>();
//
//
//			AtomicReference<Thread> subscribeThread = new AtomicReference<Thread>();
//
//			TestHystrixCommand<bool?> command = new TestHystrixCommandAnonymousInnerClassHelper2(this, TestHystrixCommand.testPropsBuilder(), commandThread);
//
//
//
//			CountDownLatch latch = new CountDownLatch(1);
//
//			command.toObservable().subscribe(new ObserverAnonymousInnerClassHelper2(this, subscribeThread, latch));
//
//			if (!latch.@await(2000, TimeUnit.MILLISECONDS))
//			{
//				Assert.Fail("timed out");
//			}
//
//			Assert.IsNotNull(commandThread.get());
//			Assert.IsNotNull(subscribeThread.get());
//
//			Console.WriteLine("HystrixCommand Thread: " + commandThread.get());
//			Console.WriteLine("Subscribe Thread: " + subscribeThread.get());
//
//			Assert.IsTrue(commandThread.get().Name.StartsWith("hystrix-"));
//			Assert.IsTrue(subscribeThread.get().Name.StartsWith("RxComputationThreadPool"));
//		}
//
//		private class TestHystrixCommandAnonymousInnerClassHelper2 : TestHystrixCommand<bool?>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private AtomicReference<Thread> commandThread;
//
//			public TestHystrixCommandAnonymousInnerClassHelper2(HystrixCommandTest outerInstance, com.netflix.hystrix.HystrixCommandTest.TestHystrixCommand.TestCommandBuilder testPropsBuilder, AtomicReference<Thread> commandThread) : base(testPropsBuilder)
//			{
//				this.outerInstance = outerInstance;
//				this.commandThread = commandThread;
//			}
//
//
//			protected internal override bool? run()
//			{
//				commandThread.set(Thread.CurrentThread);
//				return true;
//			}
//		}
//
//		private class ObserverAnonymousInnerClassHelper2 : Observer<bool?>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private AtomicReference<Thread> subscribeThread;
//			private CountDownLatch latch;
//
//			public ObserverAnonymousInnerClassHelper2(HystrixCommandTest outerInstance, AtomicReference<Thread> subscribeThread, CountDownLatch latch)
//			{
//				this.outerInstance = outerInstance;
//				this.subscribeThread = subscribeThread;
//				this.latch = latch;
//			}
//
//
//			public override void onCompleted()
//			{
//				latch.countDown();
//
//			}
//
//			public override void onError(Exception e)
//			{
//				latch.countDown();
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//
//			}
//
//			public override void onNext(bool? args)
//			{
//				subscribeThread.set(Thread.CurrentThread);
//			}
//		}
//
//		/// <summary>
//		/// Test a successful command execution.
//		/// </summary>
//
//
//
//		public void testObserveOnImmediateSchedulerByDefaultForSemaphoreIsolation()
//		{
//
//
//
//			AtomicReference<Thread> commandThread = new AtomicReference<Thread>();
//
//
//			AtomicReference<Thread> subscribeThread = new AtomicReference<Thread>();
//
//			TestHystrixCommand<bool?> command = new TestHystrixCommandAnonymousInnerClassHelper3(this, TestHystrixCommand.testPropsBuilder().setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationStrategy(ExecutionIsolationStrategy.SEMAPHORE)), commandThread);
//
//
//
//			CountDownLatch latch = new CountDownLatch(1);
//
//			command.toObservable().subscribe(new ObserverAnonymousInnerClassHelper3(this, subscribeThread, latch));
//
//			if (!latch.@await(2000, TimeUnit.MILLISECONDS))
//			{
//				Assert.Fail("timed out");
//			}
//
//			Assert.IsNotNull(commandThread.get());
//			Assert.IsNotNull(subscribeThread.get());
//
//			Console.WriteLine("HystrixCommand Thread: " + commandThread.get());
//			Console.WriteLine("Subscribe Thread: " + subscribeThread.get());
//
//			string mainThreadName = Thread.CurrentThread.Name;
//
//			// semaphore should be on the calling thread
//			Assert.IsTrue(commandThread.get().Name.Equals(mainThreadName));
//			Assert.IsTrue(subscribeThread.get().Name.Equals(mainThreadName));
//		}
//
//		private class TestHystrixCommandAnonymousInnerClassHelper3 : TestHystrixCommand<bool?>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private AtomicReference<Thread> commandThread;
//
//			public TestHystrixCommandAnonymousInnerClassHelper3(HystrixCommandTest outerInstance, com.netflix.hystrix.HystrixCommandTest.TestHystrixCommand.TestCommandBuilder setCommandPropertiesDefaults, AtomicReference<Thread> commandThread) : base(setCommandPropertiesDefaults)
//			{
//				this.outerInstance = outerInstance;
//				this.commandThread = commandThread;
//			}
//
//
//			protected internal override bool? run()
//			{
//				commandThread.set(Thread.CurrentThread);
//				return true;
//			}
//		}
//
//		private class ObserverAnonymousInnerClassHelper3 : Observer<bool?>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private AtomicReference<Thread> subscribeThread;
//			private CountDownLatch latch;
//
//			public ObserverAnonymousInnerClassHelper3(HystrixCommandTest outerInstance, AtomicReference<Thread> subscribeThread, CountDownLatch latch)
//			{
//				this.outerInstance = outerInstance;
//				this.subscribeThread = subscribeThread;
//				this.latch = latch;
//			}
//
//
//			public override void onCompleted()
//			{
//				latch.countDown();
//
//			}
//
//			public override void onError(Exception e)
//			{
//				latch.countDown();
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//
//			}
//
//			public override void onNext(bool? args)
//			{
//				subscribeThread.set(Thread.CurrentThread);
//			}
//		}
//
//
//		/// <summary>
//		/// Test a queued command execution timeout where the command didn't implement getFallback.
//		/// <para>
//		/// We specifically want to protect against developers queuing commands and using queue().get() without a timeout (such as queue().get(3000, TimeUnit.Milliseconds)) and ending up blocking
//		/// indefinitely by skipping the timeout protection of the execute() command.
//		/// </para>
//		/// </summary>
//
//
//		public void testObservedExecutionTimeoutWithNoFallback()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_NOT_IMPLEMENTED);
//			try
//			{
//				command.observe().toBlockingObservable().single();
//				Assert.Fail("we shouldn't get here");
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				if (e is HystrixException)
//				{
//					HystrixException de = (HystrixException) e;
//					Assert.IsNotNull(de.FallbackException);
//					Assert.IsTrue(de.FallbackException is System.NotSupportedException);
//					Assert.IsNotNull(de.CommandType);
//					Assert.IsNotNull(de.InnerException);
//					Assert.IsTrue(de.InnerException is TimeoutException);
//				}
//				else
//				{
//					Assert.Fail("the exception should be ExecutionException with cause as HystrixException");
//				}
//			}
//
//			Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(command.ResponseTimedOut);
//
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test a queued command execution timeout where the command implemented getFallback.
//		/// <para>
//		/// We specifically want to protect against developers queuing commands and using queue().get() without a timeout (such as queue().get(3000, TimeUnit.Milliseconds)) and ending up blocking
//		/// indefinitely by skipping the timeout protection of the execute() command.
//		/// </para>
//		/// </summary>
//
//
//		public void testObservedExecutionTimeoutWithFallback()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_SUCCESS);
//			try
//			{
//				Assert.AreEqual(false, command.observe().toBlockingObservable().single());
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We should have received a response from the fallback.");
//			}
//
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test a queued command execution timeout where the command implemented getFallback but it Assert.Fails.
//		/// <para>
//		/// We specifically want to protect against developers queuing commands and using queue().get() without a timeout (such as queue().get(3000, TimeUnit.Milliseconds)) and ending up blocking
//		/// indefinitely by skipping the timeout protection of the execute() command.
//		/// </para>
//		/// </summary>
//
//
//		public void testObservedExecutionTimeoutFallbackFailure()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_FAILURE);
//			try
//			{
//				command.observe().toBlockingObservable().single();
//				Assert.Fail("we shouldn't get here");
//			}
//			catch (Exception e)
//			{
//				if (e is HystrixException)
//				{
//					HystrixException de = (HystrixException) e;
//					Assert.IsNotNull(de.FallbackException);
//					Assert.IsFalse(de.FallbackException is System.NotSupportedException);
//					Assert.IsNotNull(de.CommandType);
//					Assert.IsNotNull(de.InnerException);
//					Assert.IsTrue(de.InnerException is TimeoutException);
//				}
//				else
//				{
//					Assert.Fail("the exception should be ExecutionException with cause as HystrixException");
//				}
//			}
//
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test that the circuit-breaker counts a command execution timeout as a 'timeout' and not just Assert.Failure.
//		/// </summary>
//
//
//		public void testShortCircuitFallbackCounter()
//		{
//			TestCircuitBreaker circuitBreaker = (new TestCircuitBreaker()).setForceShortCircuit(true);
//			try
//			{
//				(new KnownFailureTestCommandWithFallback(circuitBreaker)).execute();
//
//				Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//
//				KnownFailureTestCommandWithFallback command = new KnownFailureTestCommandWithFallback(circuitBreaker);
//				command.Run();
//				Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//
//				// will be -1 because it never attempted execution
//				Assert.IsTrue(command.GetExecutionTimeInMilliseconds() == -1);
//				Assert.IsTrue(command.IsResponseShortCircuited());
//				Assert.IsFalse(command.ResponseTimedOut);
//
//				// because it was short-circuited to a fallback we don't count an error
//				Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//				Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//				Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We should have received a response from the fallback.");
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(2, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test when a command Assert.Fails to get queued up in the threadpool where the command didn't implement getFallback.
//		/// <para>
//		/// We specifically want to protect against developers getting random thread exceptions and instead just correctly receiving HystrixException when no fallback exists.
//		/// </para>
//		/// </summary>
//
//
//		public void testRejectedThreadWithNoFallback()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SingleThreadedPool pool = new SingleThreadedPool(1);
//			// fill up the queue
//			pool.queue.add(new RunnableAnonymousInnerClassHelper(this));
//
//			Future<bool?> f = null;
//			TestCommandRejection command = null;
//			try
//			{
//				f = (new TestCommandRejection(circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_NOT_IMPLEMENTED)).queue();
//				command = new TestCommandRejection(circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_NOT_IMPLEMENTED);
//				command.queue();
//				Assert.Fail("we shouldn't get here");
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Console.WriteLine("command.getExecutionTimeInMilliseconds(): " + command.GetExecutionTimeInMilliseconds());
//				// will be -1 because it never attempted execution
//				Assert.IsTrue(command.GetExecutionTimeInMilliseconds() == -1);
//				Assert.IsTrue(command.ResponseRejected);
//				Assert.IsFalse(command.IsResponseShortCircuited());
//				Assert.IsFalse(command.ResponseTimedOut);
//
//				Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//				Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//				Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//				Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//				if (e is HystrixException && e.InnerException is RejectedExecutionException)
//				{
//					HystrixException de = (HystrixException) e;
//					Assert.IsNotNull(de.FallbackException);
//					Assert.IsTrue(de.FallbackException is System.NotSupportedException);
//					Assert.IsNotNull(de.CommandType);
//					Assert.IsNotNull(de.InnerException);
//					Assert.IsTrue(de.InnerException is RejectedExecutionException);
//				}
//				else
//				{
//					Assert.Fail("the exception should be HystrixException with cause as RejectedExecutionException");
//				}
//			}
//
//			try
//			{
//				f.get();
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("The first one should succeed.");
//			}
//
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(50, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(2, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		private class RunnableAnonymousInnerClassHelper : Runnable
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			public RunnableAnonymousInnerClassHelper(HystrixCommandTest outerInstance)
//			{
//				this.outerInstance = outerInstance;
//			}
//
//
//			public override void run()
//			{
//				Console.WriteLine("**** queue filler1 ****");
//				try
//				{
//					Thread.Sleep(500);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//			}
//
//		}
//

//
//		private class RunnableAnonymousInnerClassHelper2 : Runnable
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			public RunnableAnonymousInnerClassHelper2(HystrixCommandTest outerInstance)
//			{
//				this.outerInstance = outerInstance;
//			}
//
//
//			public override void run()
//			{
//				Console.WriteLine("**** queue filler1 ****");
//				try
//				{
//					Thread.Sleep(500);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//			}
//
//		}
//
//		/// <summary>
//		/// Test when a command Assert.Fails to get queued up in the threadpool where the command implemented getFallback but it Assert.Fails.
//		/// <para>
//		/// We specifically want to protect against developers getting random thread exceptions and instead just correctly receives an HystrixException.
//		/// </para>
//		/// </summary>
//
//
//		public void testRejectedThreadWithFallbackFailure()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SingleThreadedPool pool = new SingleThreadedPool(1);
//			// fill up the queue
//			pool.queue.add(new RunnableAnonymousInnerClassHelper3(this));
//
//			try
//			{
//				(new TestCommandRejection(circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_FAILURE)).queue();
//				Assert.AreEqual(false, (new TestCommandRejection(circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_FAILURE)).queue().get());
//				Assert.Fail("we shouldn't get here");
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//				if (e is HystrixException && e.InnerException is RejectedExecutionException)
//				{
//					HystrixException de = (HystrixException) e;
//					Assert.IsNotNull(de.FallbackException);
//					Assert.IsFalse(de.FallbackException is System.NotSupportedException);
//					Assert.IsNotNull(de.CommandType);
//					Assert.IsNotNull(de.InnerException);
//					Assert.IsTrue(de.InnerException is RejectedExecutionException);
//				}
//				else
//				{
//					Assert.Fail("the exception should be HystrixException with cause as RejectedExecutionException");
//				}
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(2, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		private class RunnableAnonymousInnerClassHelper3 : Runnable
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			public RunnableAnonymousInnerClassHelper3(HystrixCommandTest outerInstance)
//			{
//				this.outerInstance = outerInstance;
//			}
//
//
//			public override void run()
//			{
//				Console.WriteLine("**** queue filler1 ****");
//				try
//				{
//					Thread.Sleep(500);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//			}
//
//		}
//
//		/// <summary>
//		/// Test that we can reject a thread using isQueueSpaceAvailable() instead of just when the pool rejects.
//		/// <para>
//		/// For example, we have queue size set to 100 but want to reject when we hit 10.
//		/// </para>
//		/// <para>
//		/// This allows us to use FastProperties to control our rejection point whereas we can't resize a queue after it's created.
//		/// </para>
//		/// </summary>
//
//
//		public void testRejectedThreadUsingQueueSize()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SingleThreadedPool pool = new SingleThreadedPool(10, 1);
//			// put 1 item in the queue
//			// the thread pool won't pick it up because we're bypassing the pool and adding to the queue directly so this will keep the queue full
//			pool.queue.add(new RunnableAnonymousInnerClassHelper4(this));
//
//			TestCommandRejection command = null;
//			try
//			{
//				// this should Assert.Fail as we already have 1 in the queue
//				command = new TestCommandRejection(circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_NOT_IMPLEMENTED);
//				command.queue();
//				Assert.Fail("we shouldn't get here");
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//
//				// will be -1 because it never attempted execution
//				Assert.IsTrue(command.GetExecutionTimeInMilliseconds() == -1);
//				Assert.IsTrue(command.ResponseRejected);
//				Assert.IsFalse(command.IsResponseShortCircuited());
//				Assert.IsFalse(command.ResponseTimedOut);
//
//				Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//				if (e is HystrixException && e.InnerException is RejectedExecutionException)
//				{
//					HystrixException de = (HystrixException) e;
//					Assert.IsNotNull(de.FallbackException);
//					Assert.IsTrue(de.FallbackException is System.NotSupportedException);
//					Assert.IsNotNull(de.CommandType);
//					Assert.IsNotNull(de.InnerException);
//					Assert.IsTrue(de.InnerException is RejectedExecutionException);
//				}
//				else
//				{
//					Assert.Fail("the exception should be HystrixException with cause as RejectedExecutionException");
//				}
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		private class RunnableAnonymousInnerClassHelper4 : Runnable
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			public RunnableAnonymousInnerClassHelper4(HystrixCommandTest outerInstance)
//			{
//				this.outerInstance = outerInstance;
//			}
//
//
//			public override void run()
//			{
//				Console.WriteLine("**** queue filler1 ****");
//				try
//				{
//					Thread.Sleep(500);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//			}
//
//		}
//
//		/// <summary>
//		/// If it has been sitting in the queue, it should not execute if timed out by the time it hits the queue.
//		/// </summary>
//
//
//		public void testTimedOutCommandDoesNotExecute()
//		{
//			SingleThreadedPool pool = new SingleThreadedPool(5);
//
//			TestCircuitBreaker s1 = new TestCircuitBreaker();
//			TestCircuitBreaker s2 = new TestCircuitBreaker();
//
//			// execution will take 100ms, thread pool has a 600ms timeout
//			CommandWithCustomThreadPool c1 = new CommandWithCustomThreadPool(s1, pool, 500, HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationThreadTimeoutInMilliseconds(600));
//			// execution will take 200ms, thread pool has a 20ms timeout
//			CommandWithCustomThreadPool c2 = new CommandWithCustomThreadPool(s2, pool, 200, HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationThreadTimeoutInMilliseconds(20));
//			// queue up c1 first
//			Future<bool?> c1f = c1.queue();
//			// now queue up c2 and wait on it
//			bool receivedException = false;
//			try
//			{
//				c2.queue().get();
//			}
//			catch (Exception)
//			{
//				// we expect to get an exception here
//				receivedException = true;
//			}
//
//			if (!receivedException)
//			{
//				Assert.Fail("We expect to receive an exception for c2 as it's supposed to timeout.");
//			}
//
//			// c1 will complete after 100ms
//			try
//			{
//				c1f.get();
//			}
//			catch (Exception e1)
//			{
//				Console.WriteLine(e1.ToString());
//				Console.Write(e1.StackTrace);
//				Assert.Fail("we should not have Assert.Failed while getting c1");
//			}
//			Assert.IsTrue("c1 is expected to executed but didn't", c1.didExecute);
//
//			// c2 will timeout after 20 ms ... we'll wait longer than the 200ms time to make sure
//			// the thread doesn't keep running in the background and execute
//			try
//			{
//				Thread.Sleep(400);
//			}
//			catch (Exception)
//			{
//				throw new Exception("Failed to sleep");
//			}
//			Assert.IsFalse("c2 is not expected to execute, but did", c2.didExecute);
//
//			Assert.AreEqual(1, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, s1.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, s1.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(0, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, s2.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, s2.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, s2.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(2, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//
//
//		public void testFallbackSemaphore()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			// single thread should work
//			try
//			{
//				bool result = (new TestSemaphoreCommandWithSlowFallback(circuitBreaker, 1, 200)).queue().get();
//				Assert.IsTrue(result);
//			}
//			catch (Exception e)
//			{
//				// we shouldn't Assert.Fail on this one
//				throw new Exception(e);
//			}
//
//			// 2 threads, the second should be rejected by the fallback semaphore
//			bool exceptionReceived = false;
//			Future<bool?> result = null;
//			try
//			{
//				Console.WriteLine("c2 start: " + DateTimeHelperClass.CurrentUnixTimeMillis());
//				result = (new TestSemaphoreCommandWithSlowFallback(circuitBreaker, 1, 800)).queue();
//				Console.WriteLine("c2 after queue: " + DateTimeHelperClass.CurrentUnixTimeMillis());
//				// make sure that thread gets a chance to run before queuing the next one
//				Thread.Sleep(50);
//				Console.WriteLine("c3 start: " + DateTimeHelperClass.CurrentUnixTimeMillis());
//				Future<bool?> result2 = (new TestSemaphoreCommandWithSlowFallback(circuitBreaker, 1, 200)).queue();
//				Console.WriteLine("c3 after queue: " + DateTimeHelperClass.CurrentUnixTimeMillis());
//				result2.get();
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				exceptionReceived = true;
//			}
//
//			try
//			{
//				Assert.IsTrue(result.get());
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			if (!exceptionReceived)
//			{
//				Assert.Fail("We expected an exception on the 2nd get");
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			// TestSemaphoreCommandWithSlowFallback always Assert.Fails so all 3 should show Assert.Failure
//			Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			// the 1st thread executes single-threaded and gets a fallback, the next 2 are concurrent so only 1 of them is permitted by the fallback semaphore so 1 is rejected
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			// whenever a fallback_rejection occurs it is also a fallback_Assert.Failure
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			// we should not have rejected any via the "execution semaphore" but instead via the "fallback semaphore"
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			// the rest should not be involved in this test
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(3, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//
//
//		public void testExecutionSemaphoreWithQueue()
//		{
//
//
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			// single thread should work
//			try
//			{
//				bool result = (new TestSemaphoreCommand(circuitBreaker, 1, 200)).queue().get();
//				Assert.IsTrue(result);
//			}
//			catch (Exception e)
//			{
//				// we shouldn't Assert.Fail on this one
//				throw new Exception(e);
//			}
//
//
//
//			AtomicBoolean exceptionReceived = new AtomicBoolean();
//
//
//
//			TryableSemaphore semaphore = new TryableSemaphoreActual(HystrixProperty.Factory.asProperty(1));
//
//			Runnable r = new HystrixContextRunnable(HystrixPlugins.Instance.ConcurrencyStrategy, new RunnableAnonymousInnerClassHelper5(this, circuitBreaker, e, exceptionReceived, semaphore));
//			// 2 threads, the second should be rejected by the semaphore
//			Thread t1 = new Thread(r);
//			Thread t2 = new Thread(r);
//
//			t1.Start();
//			t2.Start();
//			try
//			{
//				t1.Join();
//				t2.Join();
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("Assert.Failed waiting on threads");
//			}
//
//			if (!exceptionReceived.get())
//			{
//				Assert.Fail("We expected an exception on the 2nd get");
//			}
//
//			Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			// we don't have a fallback so threw an exception when rejected
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			// not a Assert.Failure as the command never executed so can't Assert.Fail
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			// no fallback Assert.Failure as there isn't a fallback implemented
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			// we should have rejected via semaphore
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			// the rest should not be involved in this test
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(3, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		private class RunnableAnonymousInnerClassHelper5 : Runnable
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private TestCircuitBreaker circuitBreaker;
//			private Exception e;
//			private AtomicBoolean exceptionReceived;
//			private TryableSemaphore semaphore;
//
//			public RunnableAnonymousInnerClassHelper5(HystrixCommandTest outerInstance, TestCircuitBreaker circuitBreaker, Exception e, AtomicBoolean exceptionReceived, TryableSemaphore semaphore)
//			{
//				this.outerInstance = outerInstance;
//				this.circuitBreaker = circuitBreaker;
//				this.e = e;
//				this.exceptionReceived = exceptionReceived;
//				this.semaphore = semaphore;
//			}
//
//
//			public override void run()
//			{
//				try
//				{
//					(new TestSemaphoreCommand(circuitBreaker, semaphore, 200)).queue().get();
//				}
//				catch (Exception e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//					exceptionReceived.set(true);
//				}
//			}
//
//		}
//
//
//
//		public void testExecutionSemaphoreWithExecution()
//		{
//
//
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			// single thread should work
//			try
//			{
//				TestSemaphoreCommand command = new TestSemaphoreCommand(circuitBreaker, 1, 200);
//				bool result = command.Run();
//				Assert.IsFalse(command.ExecutedInThread);
//				Assert.IsTrue(result);
//			}
//			catch (Exception e)
//			{
//				// we shouldn't Assert.Fail on this one
//				throw new Exception(e);
//			}
//
//
//
//			ArrayBlockingQueue<bool?> results = new ArrayBlockingQueue<bool?>(2);
//
//
//
//			AtomicBoolean exceptionReceived = new AtomicBoolean();
//
//
//
//			TryableSemaphore semaphore = new TryableSemaphoreActual(HystrixProperty.Factory.asProperty(1));
//
//			Runnable r = new HystrixContextRunnable(HystrixPlugins.Instance.ConcurrencyStrategy, new RunnableAnonymousInnerClassHelper6(this, circuitBreaker, e, results, exceptionReceived, semaphore));
//			// 2 threads, the second should be rejected by the semaphore
//			Thread t1 = new Thread(r);
//			Thread t2 = new Thread(r);
//
//			t1.Start();
//			t2.Start();
//			try
//			{
//				t1.Join();
//				t2.Join();
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("Assert.Failed waiting on threads");
//			}
//
//			if (!exceptionReceived.get())
//			{
//				Assert.Fail("We expected an exception on the 2nd get");
//			}
//
//			// only 1 value is expected as the other should have thrown an exception
//			Assert.AreEqual(1, results.size());
//			// should contain only a true result
//			Assert.IsTrue(results.contains(true));
//			Assert.IsFalse(results.contains(false));
//
//			Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			// no Assert.Failure ... we throw an exception because of rejection but the command does not Assert.Fail execution
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			// there is no fallback implemented so no Assert.Failure can occur on it
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			// we rejected via semaphore
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			// the rest should not be involved in this test
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(3, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		private class RunnableAnonymousInnerClassHelper6 : Runnable
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private TestCircuitBreaker circuitBreaker;
//			private Exception e;
//
//
//			private ArrayBlockingQueue<bool?> results;
//			private AtomicBoolean exceptionReceived;
//			private TryableSemaphore semaphore;
//
//			public RunnableAnonymousInnerClassHelper6<T1>(HystrixCommandTest outerInstance, TestCircuitBreaker circuitBreaker, Exception e, ArrayBlockingQueue<T1> results, AtomicBoolean exceptionReceived, TryableSemaphore semaphore)
//			{
//				this.outerInstance = outerInstance;
//				this.circuitBreaker = circuitBreaker;
//				this.e = e;
//				this.results = results;
//				this.exceptionReceived = exceptionReceived;
//				this.semaphore = semaphore;
//			}
//
//
//			public override void run()
//			{
//				try
//				{
//					results.add((new TestSemaphoreCommand(circuitBreaker, semaphore, 200)).execute());
//				}
//				catch (Exception e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//					exceptionReceived.set(true);
//				}
//			}
//
//		}
//
//
//
//		public void testRejectedExecutionSemaphoreWithFallback()
//		{
//
//
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//
//
//			ArrayBlockingQueue<bool?> results = new ArrayBlockingQueue<bool?>(2);
//
//
//
//			AtomicBoolean exceptionReceived = new AtomicBoolean();
//
//			Runnable r = new HystrixContextRunnable(HystrixPlugins.Instance.ConcurrencyStrategy, new RunnableAnonymousInnerClassHelper7(this, circuitBreaker, results, exceptionReceived));
//
//			// 2 threads, the second should be rejected by the semaphore and return fallback
//			Thread t1 = new Thread(r);
//			Thread t2 = new Thread(r);
//
//			t1.Start();
//			t2.Start();
//			try
//			{
//				t1.Join();
//				t2.Join();
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("Assert.Failed waiting on threads");
//			}
//
//			if (exceptionReceived.get())
//			{
//				Assert.Fail("We should have received a fallback response");
//			}
//
//			// both threads should have returned values
//			Assert.AreEqual(2, results.size());
//			// should contain both a true and false result
//			Assert.IsTrue(results.contains(true));
//			Assert.IsTrue(results.contains(false));
//
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			// the rest should not be involved in this test
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Console.WriteLine("**** DONE");
//
//			Assert.AreEqual(2, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		private class RunnableAnonymousInnerClassHelper7 : Runnable
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private TestCircuitBreaker circuitBreaker;
//
//
//			private ArrayBlockingQueue<bool?> results;
//			private AtomicBoolean exceptionReceived;
//
//			public RunnableAnonymousInnerClassHelper7<T1>(HystrixCommandTest outerInstance, TestCircuitBreaker circuitBreaker, ArrayBlockingQueue<T1> results, AtomicBoolean exceptionReceived)
//			{
//				this.outerInstance = outerInstance;
//				this.circuitBreaker = circuitBreaker;
//				this.results = results;
//				this.exceptionReceived = exceptionReceived;
//			}
//
//
//			public override void run()
//			{
//				try
//				{
//					results.add((new TestSemaphoreCommandWithFallback(circuitBreaker, 1, 200, false)).execute());
//				}
//				catch (Exception e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//					exceptionReceived.set(true);
//				}
//			}
//
//		}
//
//		/// <summary>
//		/// Tests that semaphores are counted separately for commands with unique keys
//		/// </summary>
//
//
//		public void testSemaphorePermitsInUse()
//		{
//
//
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//
//			// this semaphore will be shared across multiple command instances
//
//
//			TryableSemaphoreActual sharedSemaphore = new TryableSemaphoreActual(HystrixProperty.Factory.asProperty(3));
//
//			// used to wait until all commands have started
//
//
//			CountDownLatch startLatch = new CountDownLatch(sharedSemaphore.numberOfPermits.get() + 1);
//
//			// used to signal that all command can finish
//
//
//			CountDownLatch sharedLatch = new CountDownLatch(1);
//
//
//
//			Runnable sharedSemaphoreRunnable = new HystrixContextRunnable(HystrixPlugins.Instance.ConcurrencyStrategy, new RunnableAnonymousInnerClassHelper8(this, circuitBreaker, sharedSemaphore, startLatch, sharedLatch));
//
//			// creates group of threads each using command sharing a single semaphore
//
//			// I create extra threads and commands so that I can verify that some of them Assert.Fail to obtain a semaphore
//
//
//			int sharedThreadCount = sharedSemaphore.numberOfPermits.get() * 2;
//
//
//			Thread[] sharedSemaphoreThreads = new Thread[sharedThreadCount];
//			for (int i = 0; i < sharedThreadCount; i++)
//			{
//				sharedSemaphoreThreads[i] = new Thread(sharedSemaphoreRunnable);
//			}
//
//			// creates thread using isolated semaphore
//
//
//			TryableSemaphoreActual isolatedSemaphore = new TryableSemaphoreActual(HystrixProperty.Factory.asProperty(1));
//
//
//
//			CountDownLatch isolatedLatch = new CountDownLatch(1);
//
//			// tracks Assert.Failures to obtain semaphores
//
//
//			AtomicInteger Assert.FailureCount = new AtomicInteger();
//
//
//
//			Thread isolatedThread = new Thread(new HystrixContextRunnable(HystrixPlugins.Instance.ConcurrencyStrategy, new RunnableAnonymousInnerClassHelper9(this, circuitBreaker, startLatch, isolatedSemaphore, isolatedLatch, Assert.FailureCount)));
//
//			// verifies no permits in use before starting threads
//			Assert.AreEqual("wrong number of permits for shared semaphore", 0, sharedSemaphore.NumberOfPermitsUsed);
//			Assert.AreEqual("wrong number of permits for isolated semaphore", 0, isolatedSemaphore.NumberOfPermitsUsed);
//
//			for (int i = 0; i < sharedThreadCount; i++)
//			{
//				sharedSemaphoreThreads[i].Start();
//			}
//			isolatedThread.Start();
//
//			// waits until all commands have started
//			try
//			{
//				startLatch.@await(1000, TimeUnit.MILLISECONDS);
//			}
//			catch (InterruptedException e)
//			{
//				throw new Exception(e);
//			}
//
//			// verifies that all semaphores are in use
//			Assert.AreEqual("wrong number of permits for shared semaphore", (long)sharedSemaphore.numberOfPermits.get(), sharedSemaphore.NumberOfPermitsUsed);
//			Assert.AreEqual("wrong number of permits for isolated semaphore", (long)isolatedSemaphore.numberOfPermits.get(), isolatedSemaphore.NumberOfPermitsUsed);
//
//			// signals commands to finish
//			sharedLatch.countDown();
//			isolatedLatch.countDown();
//
//			try
//			{
//				for (int i = 0; i < sharedThreadCount; i++)
//				{
//					sharedSemaphoreThreads[i].Join();
//				}
//				isolatedThread.Join();
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("Assert.Failed waiting on threads");
//			}
//
//			// verifies no permits in use after finishing threads
//			Assert.AreEqual("wrong number of permits for shared semaphore", 0, sharedSemaphore.NumberOfPermitsUsed);
//			Assert.AreEqual("wrong number of permits for isolated semaphore", 0, isolatedSemaphore.NumberOfPermitsUsed);
//
//			// verifies that some executions Assert.Failed
//
//
//			int expectedFailures = sharedSemaphore.NumberOfPermitsUsed;
//			Assert.AreEqual("Assert.Failures expected but did not happen", expectedFailures, Assert.FailureCount.get());
//		}
//
//		private class RunnableAnonymousInnerClassHelper8 : Runnable
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private TestCircuitBreaker circuitBreaker;
//			private TryableSemaphoreActual sharedSemaphore;
//			private CountDownLatch startLatch;
//			private CountDownLatch sharedLatch;
//
//			public RunnableAnonymousInnerClassHelper8(HystrixCommandTest outerInstance, TestCircuitBreaker circuitBreaker, TryableSemaphoreActual sharedSemaphore, CountDownLatch startLatch, CountDownLatch sharedLatch)
//			{
//				this.outerInstance = outerInstance;
//				this.circuitBreaker = circuitBreaker;
//				this.sharedSemaphore = sharedSemaphore;
//				this.startLatch = startLatch;
//				this.sharedLatch = sharedLatch;
//			}
//
//			public void run()
//			{
//				try
//				{
//					(new LatchedSemaphoreCommand(circuitBreaker, sharedSemaphore, startLatch, sharedLatch)).execute();
//				}
//				catch (Exception e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//			}
//		}
//
//		private class RunnableAnonymousInnerClassHelper9 : Runnable
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private TestCircuitBreaker circuitBreaker;
//			private CountDownLatch startLatch;
//			private TryableSemaphoreActual isolatedSemaphore;
//			private CountDownLatch isolatedLatch;
//			private AtomicInteger Assert.FailureCount;
//
//			public RunnableAnonymousInnerClassHelper9(HystrixCommandTest outerInstance, TestCircuitBreaker circuitBreaker, CountDownLatch startLatch, TryableSemaphoreActual isolatedSemaphore, CountDownLatch isolatedLatch, AtomicInteger Assert.FailureCount)
//			{
//				this.outerInstance = outerInstance;
//				this.circuitBreaker = circuitBreaker;
//				this.startLatch = startLatch;
//				this.isolatedSemaphore = isolatedSemaphore;
//				this.isolatedLatch = isolatedLatch;
//				this.Assert.FailureCount = Assert.FailureCount;
//			}
//
//			public void run()
//			{
//				try
//				{
//					(new LatchedSemaphoreCommand(circuitBreaker, isolatedSemaphore, startLatch, isolatedLatch)).execute();
//				}
//				catch (Exception e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//					Assert.FailureCount.incrementAndGet();
//				}
//			}
//		}
//
//		/// <summary>
//		/// Test that HystrixOwner can be passed in dynamically.
//		/// </summary>
//
//
//		public void testDynamicOwner()
//		{
//			try
//			{
//				TestHystrixCommand<bool?> command = new DynamicOwnerTestCommand(CommandGroupForUnitTest.OWNER_ONE);
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//				Assert.AreEqual(true, command.Run());
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//				Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We received an exception.");
//			}
//		}
//
//		/// <summary>
//		/// Test a successful command execution.
//		/// </summary>
//
//
//		public void testDynamicOwnerFails()
//		{
//			try
//			{
//				TestHystrixCommand<bool?> command = new DynamicOwnerTestCommand(null);
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//				Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//				Assert.AreEqual(true, command.Run());
//				Assert.Fail("we should have thrown an exception as we need an owner");
//			}
//			catch (Exception)
//			{
//				// success if we get here
//			}
//		}
//
//		/// <summary>
//		/// Test that HystrixCommandKey can be passed in dynamically.
//		/// </summary>
//
//
//		public void testDynamicKey()
//		{
//			try
//			{
//				DynamicOwnerAndKeyTestCommand command1 = new DynamicOwnerAndKeyTestCommand(CommandGroupForUnitTest.OWNER_ONE, CommandKeyForUnitTest.KEY_ONE);
//				Assert.AreEqual(true, command1.execute());
//				DynamicOwnerAndKeyTestCommand command2 = new DynamicOwnerAndKeyTestCommand(CommandGroupForUnitTest.OWNER_ONE, CommandKeyForUnitTest.KEY_TWO);
//				Assert.AreEqual(true, command2.execute());
//
//				// 2 different circuit breakers should be created
//				assertNotSame(command1.CircuitBreaker, command2.CircuitBreaker);
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We received an exception.");
//			}
//		}
//
//		/// <summary>
//		/// Test Request scoped caching of commands so that a 2nd duplicate call doesn't execute but returns the previous Future
//		/// </summary>
//
//
//		public void testRequestCache1()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SuccessfulCacheableCommand command1 = new SuccessfulCacheableCommand(circuitBreaker, true, "A");
//			SuccessfulCacheableCommand command2 = new SuccessfulCacheableCommand(circuitBreaker, true, "A");
//
//			Assert.IsTrue(command1.CommandRunningInThread);
//
//			Future<string> f1 = command1.queue();
//			Future<string> f2 = command2.queue();
//
//			try
//			{
//				Assert.AreEqual("A", f1.get());
//				Assert.AreEqual("A", f2.get());
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			Assert.IsTrue(command1.executed);
//			// the second one should not have executed as it should have received the cached value instead
//			Assert.IsFalse(command2.executed);
//
//			// the execution log for command1 should show a SUCCESS
//			Assert.AreEqual(1, command1.ExecutionEvents.size());
//			Assert.IsTrue(command1.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//			Assert.IsTrue(command1.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsFalse(command1.ResponseFromCache);
//
//			// the execution log for command2 should show it came from cache
//			Assert.AreEqual(2, command2.ExecutionEvents.size()); // it will include the SUCCESS + RESPONSE_FROM_CACHE
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.RESPONSE_FROM_CACHE));
//			Assert.IsTrue(command2.GetExecutionTimeInMilliseconds() == -1);
//			Assert.IsTrue(command2.ResponseFromCache);
//
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(2, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test Request scoped caching doesn't prevent different ones from executing
//		/// </summary>
//
//
//		public void testRequestCache2()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SuccessfulCacheableCommand command1 = new SuccessfulCacheableCommand(circuitBreaker, true, "A");
//			SuccessfulCacheableCommand command2 = new SuccessfulCacheableCommand(circuitBreaker, true, "B");
//
//			Assert.IsTrue(command1.CommandRunningInThread);
//
//			Future<string> f1 = command1.queue();
//			Future<string> f2 = command2.queue();
//
//			try
//			{
//				Assert.AreEqual("A", f1.get());
//				Assert.AreEqual("B", f2.get());
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			Assert.IsTrue(command1.executed);
//			// both should execute as they are different
//			Assert.IsTrue(command2.executed);
//
//			// the execution log for command1 should show a SUCCESS
//			Assert.AreEqual(1, command1.ExecutionEvents.size());
//			Assert.IsTrue(command1.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command2 should show a SUCCESS
//			Assert.AreEqual(1, command2.ExecutionEvents.size());
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//			Assert.IsTrue(command2.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsFalse(command2.ResponseFromCache);
//
//			Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(2, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test Request scoped caching with a mixture of commands
//		/// </summary>
//
//
//		public void testRequestCache3()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SuccessfulCacheableCommand command1 = new SuccessfulCacheableCommand(circuitBreaker, true, "A");
//			SuccessfulCacheableCommand command2 = new SuccessfulCacheableCommand(circuitBreaker, true, "B");
//			SuccessfulCacheableCommand command3 = new SuccessfulCacheableCommand(circuitBreaker, true, "A");
//
//			Assert.IsTrue(command1.CommandRunningInThread);
//
//			Future<string> f1 = command1.queue();
//			Future<string> f2 = command2.queue();
//			Future<string> f3 = command3.queue();
//
//			try
//			{
//				Assert.AreEqual("A", f1.get());
//				Assert.AreEqual("B", f2.get());
//				Assert.AreEqual("A", f3.get());
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			Assert.IsTrue(command1.executed);
//			// both should execute as they are different
//			Assert.IsTrue(command2.executed);
//			// but the 3rd should come from cache
//			Assert.IsFalse(command3.executed);
//
//			// the execution log for command1 should show a SUCCESS
//			Assert.AreEqual(1, command1.ExecutionEvents.size());
//			Assert.IsTrue(command1.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command2 should show a SUCCESS
//			Assert.AreEqual(1, command2.ExecutionEvents.size());
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command3 should show it came from cache
//			Assert.AreEqual(2, command3.ExecutionEvents.size()); // it will include the SUCCESS + RESPONSE_FROM_CACHE
//			Assert.IsTrue(command3.ExecutionEvents.contains(HystrixEventType.RESPONSE_FROM_CACHE));
//			Assert.IsTrue(command3.GetExecutionTimeInMilliseconds() == -1);
//			Assert.IsTrue(command3.ResponseFromCache);
//
//			Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(3, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test Request scoped caching of commands so that a 2nd duplicate call doesn't execute but returns the previous Future
//		/// </summary>
//
//
//		public void testRequestCacheWithSlowExecution()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SlowCacheableCommand command1 = new SlowCacheableCommand(circuitBreaker, "A", 200);
//			SlowCacheableCommand command2 = new SlowCacheableCommand(circuitBreaker, "A", 100);
//			SlowCacheableCommand command3 = new SlowCacheableCommand(circuitBreaker, "A", 100);
//			SlowCacheableCommand command4 = new SlowCacheableCommand(circuitBreaker, "A", 100);
//
//			Future<string> f1 = command1.queue();
//			Future<string> f2 = command2.queue();
//			Future<string> f3 = command3.queue();
//			Future<string> f4 = command4.queue();
//
//			try
//			{
//				Assert.AreEqual("A", f2.get());
//				Assert.AreEqual("A", f3.get());
//				Assert.AreEqual("A", f4.get());
//
//				Assert.AreEqual("A", f1.get());
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			Assert.IsTrue(command1.executed);
//			// the second one should not have executed as it should have received the cached value instead
//			Assert.IsFalse(command2.executed);
//			Assert.IsFalse(command3.executed);
//			Assert.IsFalse(command4.executed);
//
//			// the execution log for command1 should show a SUCCESS
//			Assert.AreEqual(1, command1.ExecutionEvents.size());
//			Assert.IsTrue(command1.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//			Assert.IsTrue(command1.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsFalse(command1.ResponseFromCache);
//
//			// the execution log for command2 should show it came from cache
//			Assert.AreEqual(2, command2.ExecutionEvents.size()); // it will include the SUCCESS + RESPONSE_FROM_CACHE
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.RESPONSE_FROM_CACHE));
//			Assert.IsTrue(command2.GetExecutionTimeInMilliseconds() == -1);
//			Assert.IsTrue(command2.ResponseFromCache);
//
//			Assert.IsTrue(command3.ResponseFromCache);
//			Assert.IsTrue(command3.GetExecutionTimeInMilliseconds() == -1);
//			Assert.IsTrue(command4.ResponseFromCache);
//			Assert.IsTrue(command4.GetExecutionTimeInMilliseconds() == -1);
//
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(4, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//
//			Console.WriteLine("HystrixRequestLog: " + HystrixRequestLog.CurrentRequest.ExecutedCommandsAsString);
//		}
//
//		/// <summary>
//		/// Test Request scoped caching with a mixture of commands
//		/// </summary>
//
//
//		public void testNoRequestCache3()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SuccessfulCacheableCommand command1 = new SuccessfulCacheableCommand(circuitBreaker, false, "A");
//			SuccessfulCacheableCommand command2 = new SuccessfulCacheableCommand(circuitBreaker, false, "B");
//			SuccessfulCacheableCommand command3 = new SuccessfulCacheableCommand(circuitBreaker, false, "A");
//
//			Assert.IsTrue(command1.CommandRunningInThread);
//
//			Future<string> f1 = command1.queue();
//			Future<string> f2 = command2.queue();
//			Future<string> f3 = command3.queue();
//
//			try
//			{
//				Assert.AreEqual("A", f1.get());
//				Assert.AreEqual("B", f2.get());
//				Assert.AreEqual("A", f3.get());
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			Assert.IsTrue(command1.executed);
//			// both should execute as they are different
//			Assert.IsTrue(command2.executed);
//			// this should also execute since we disabled the cache
//			Assert.IsTrue(command3.executed);
//
//			// the execution log for command1 should show a SUCCESS
//			Assert.AreEqual(1, command1.ExecutionEvents.size());
//			Assert.IsTrue(command1.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command2 should show a SUCCESS
//			Assert.AreEqual(1, command2.ExecutionEvents.size());
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command3 should show a SUCCESS
//			Assert.AreEqual(1, command3.ExecutionEvents.size());
//			Assert.IsTrue(command3.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(3, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test Request scoped caching with a mixture of commands
//		/// </summary>
//
//
//		public void testRequestCacheViaQueueSemaphore1()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SuccessfulCacheableCommandViaSemaphore command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");
//			SuccessfulCacheableCommandViaSemaphore command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "B");
//			SuccessfulCacheableCommandViaSemaphore command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");
//
//			Assert.IsFalse(command1.CommandRunningInThread);
//
//			Future<string> f1 = command1.queue();
//			Future<string> f2 = command2.queue();
//			Future<string> f3 = command3.queue();
//
//			try
//			{
//				Assert.AreEqual("A", f1.get());
//				Assert.AreEqual("B", f2.get());
//				Assert.AreEqual("A", f3.get());
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			Assert.IsTrue(command1.executed);
//			// both should execute as they are different
//			Assert.IsTrue(command2.executed);
//			// but the 3rd should come from cache
//			Assert.IsFalse(command3.executed);
//
//			// the execution log for command1 should show a SUCCESS
//			Assert.AreEqual(1, command1.ExecutionEvents.size());
//			Assert.IsTrue(command1.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command2 should show a SUCCESS
//			Assert.AreEqual(1, command2.ExecutionEvents.size());
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command3 should show it comes from cache
//			Assert.AreEqual(2, command3.ExecutionEvents.size()); // it will include the SUCCESS + RESPONSE_FROM_CACHE
//			Assert.IsTrue(command3.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//			Assert.IsTrue(command3.ExecutionEvents.contains(HystrixEventType.RESPONSE_FROM_CACHE));
//
//			Assert.IsTrue(command3.ResponseFromCache);
//			Assert.IsTrue(command3.GetExecutionTimeInMilliseconds() == -1);
//
//			Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(3, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test Request scoped caching with a mixture of commands
//		/// </summary>
//
//
//		public void testNoRequestCacheViaQueueSemaphore1()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SuccessfulCacheableCommandViaSemaphore command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");
//			SuccessfulCacheableCommandViaSemaphore command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "B");
//			SuccessfulCacheableCommandViaSemaphore command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");
//
//			Assert.IsFalse(command1.CommandRunningInThread);
//
//			Future<string> f1 = command1.queue();
//			Future<string> f2 = command2.queue();
//			Future<string> f3 = command3.queue();
//
//			try
//			{
//				Assert.AreEqual("A", f1.get());
//				Assert.AreEqual("B", f2.get());
//				Assert.AreEqual("A", f3.get());
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			Assert.IsTrue(command1.executed);
//			// both should execute as they are different
//			Assert.IsTrue(command2.executed);
//			// this should also execute because caching is disabled
//			Assert.IsTrue(command3.executed);
//
//			// the execution log for command1 should show a SUCCESS
//			Assert.AreEqual(1, command1.ExecutionEvents.size());
//			Assert.IsTrue(command1.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command2 should show a SUCCESS
//			Assert.AreEqual(1, command2.ExecutionEvents.size());
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command3 should show a SUCCESS
//			Assert.AreEqual(1, command3.ExecutionEvents.size());
//			Assert.IsTrue(command3.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(3, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test Request scoped caching with a mixture of commands
//		/// </summary>
//
//
//		public void testRequestCacheViaExecuteSemaphore1()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SuccessfulCacheableCommandViaSemaphore command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");
//			SuccessfulCacheableCommandViaSemaphore command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "B");
//			SuccessfulCacheableCommandViaSemaphore command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");
//
//			Assert.IsFalse(command1.CommandRunningInThread);
//
//			string f1 = command1.execute();
//			string f2 = command2.execute();
//			string f3 = command3.execute();
//
//			Assert.AreEqual("A", f1);
//			Assert.AreEqual("B", f2);
//			Assert.AreEqual("A", f3);
//
//			Assert.IsTrue(command1.executed);
//			// both should execute as they are different
//			Assert.IsTrue(command2.executed);
//			// but the 3rd should come from cache
//			Assert.IsFalse(command3.executed);
//
//			// the execution log for command1 should show a SUCCESS
//			Assert.AreEqual(1, command1.ExecutionEvents.size());
//			Assert.IsTrue(command1.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command2 should show a SUCCESS
//			Assert.AreEqual(1, command2.ExecutionEvents.size());
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command3 should show it comes from cache
//			Assert.AreEqual(2, command3.ExecutionEvents.size()); // it will include the SUCCESS + RESPONSE_FROM_CACHE
//			Assert.IsTrue(command3.ExecutionEvents.contains(HystrixEventType.RESPONSE_FROM_CACHE));
//
//			Assert.AreEqual(2, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(3, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test Request scoped caching with a mixture of commands
//		/// </summary>
//
//
//		public void testNoRequestCacheViaExecuteSemaphore1()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SuccessfulCacheableCommandViaSemaphore command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");
//			SuccessfulCacheableCommandViaSemaphore command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "B");
//			SuccessfulCacheableCommandViaSemaphore command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");
//
//			Assert.IsFalse(command1.CommandRunningInThread);
//
//			string f1 = command1.execute();
//			string f2 = command2.execute();
//			string f3 = command3.execute();
//
//			Assert.AreEqual("A", f1);
//			Assert.AreEqual("B", f2);
//			Assert.AreEqual("A", f3);
//
//			Assert.IsTrue(command1.executed);
//			// both should execute as they are different
//			Assert.IsTrue(command2.executed);
//			// this should also execute because caching is disabled
//			Assert.IsTrue(command3.executed);
//
//			// the execution log for command1 should show a SUCCESS
//			Assert.AreEqual(1, command1.ExecutionEvents.size());
//			Assert.IsTrue(command1.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command2 should show a SUCCESS
//			Assert.AreEqual(1, command2.ExecutionEvents.size());
//			Assert.IsTrue(command2.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			// the execution log for command3 should show a SUCCESS
//			Assert.AreEqual(1, command3.ExecutionEvents.size());
//			Assert.IsTrue(command3.ExecutionEvents.contains(HystrixEventType.SUCCESS));
//
//			Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(3, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//
//
//
//		public void testNoRequestCacheOnTimeoutThrowsException()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			NoRequestCacheTimeoutWithoutFallback r1 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);
//			try
//			{
//				Console.WriteLine("r1 value: " + r1.execute());
//				// we should have thrown an exception
//				Assert.Fail("expected a timeout");
//			}
//			catch (HystrixException)
//			{
//				Assert.IsTrue(r1.ResponseTimedOut);
//				// what we want
//			}
//
//			NoRequestCacheTimeoutWithoutFallback r2 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);
//			try
//			{
//				r2.execute();
//				// we should have thrown an exception
//				Assert.Fail("expected a timeout");
//			}
//			catch (HystrixException)
//			{
//				Assert.IsTrue(r2.ResponseTimedOut);
//				// what we want
//			}
//
//			NoRequestCacheTimeoutWithoutFallback r3 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);
//			Future<bool?> f3 = r3.queue();
//			try
//			{
//				f3.get();
//				// we should have thrown an exception
//				Assert.Fail("expected a timeout");
//			}
//			catch (ExecutionException e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.IsTrue(r3.ResponseTimedOut);
//				// what we want
//			}
//
//			Thread.Sleep(500); // timeout on command is set to 200ms
//
//			NoRequestCacheTimeoutWithoutFallback r4 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);
//			try
//			{
//				r4.execute();
//				// we should have thrown an exception
//				Assert.Fail("expected a timeout");
//			}
//			catch (HystrixException)
//			{
//				Assert.IsTrue(r4.ResponseTimedOut);
//				Assert.IsFalse(r4.IsResponseFromFallback());
//				// what we want
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(4, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(4, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(4, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//
//
//
//		public void testRequestCacheOnTimeoutCausesNullPointerException()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			// Expect it to time out - all results should be false
//			Assert.IsFalse((new RequestCacheNullPointerExceptionCase(circuitBreaker)).execute());
//			Assert.IsFalse((new RequestCacheNullPointerExceptionCase(circuitBreaker)).execute()); // return from cache #1
//			Assert.IsFalse((new RequestCacheNullPointerExceptionCase(circuitBreaker)).execute()); // return from cache #2
//			Thread.Sleep(500); // timeout on command is set to 200ms
//			bool? value = (new RequestCacheNullPointerExceptionCase(circuitBreaker)).execute(); // return from cache #3
//			Assert.IsFalse(value);
//			RequestCacheNullPointerExceptionCase c = new RequestCacheNullPointerExceptionCase(circuitBreaker);
//			Future<bool?> f = c.queue(); // return from cache #4
//			// the bug is that we're getting a null Future back, rather than a Future that returns false
//			Assert.IsNotNull(f);
//			Assert.IsFalse(f.get());
//
//			Assert.IsTrue(c.IsResponseFromFallback());
//			Assert.IsTrue(c.ResponseTimedOut);
//			Assert.IsFalse(c.IsFailedExecution());
//			Assert.IsFalse(c.IsResponseShortCircuited());
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(4, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(5, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//
//
//
//			HystrixCommand<?>[] executeCommands = HystrixRequestLog.CurrentRequest.ExecutedCommands.toArray(new HystrixCommand<?>[] {});
//
//			Console.WriteLine(":executeCommands[0].getExecutionEvents()" + executeCommands[0].ExecutionEvents);
//			Assert.AreEqual(2, executeCommands[0].ExecutionEvents.size());
//			Assert.IsTrue(executeCommands[0].ExecutionEvents.contains(HystrixEventType.FALLBACK_SUCCESS));
//			Assert.IsTrue(executeCommands[0].ExecutionEvents.contains(HystrixEventType.TIMEOUT));
//			Assert.IsTrue(executeCommands[0].GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(executeCommands[0].ResponseTimedOut);
//			Assert.IsTrue(executeCommands[0].IsResponseFromFallback());
//			Assert.IsFalse(executeCommands[0].ResponseFromCache);
//
//			Assert.AreEqual(3, executeCommands[1].ExecutionEvents.size()); // it will include FALLBACK_SUCCESS/TIMEOUT + RESPONSE_FROM_CACHE
//			Assert.IsTrue(executeCommands[1].ExecutionEvents.contains(HystrixEventType.RESPONSE_FROM_CACHE));
//			Assert.IsTrue(executeCommands[1].GetExecutionTimeInMilliseconds() == -1);
//			Assert.IsTrue(executeCommands[1].ResponseFromCache);
//			Assert.IsTrue(executeCommands[1].ResponseTimedOut);
//			Assert.IsTrue(executeCommands[1].IsResponseFromFallback());
//		}
//
//
//
//
//		public void testRequestCacheOnTimeoutThrowsException()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			RequestCacheTimeoutWithoutFallback r1 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);
//			try
//			{
//				Console.WriteLine("r1 value: " + r1.execute());
//				// we should have thrown an exception
//				Assert.Fail("expected a timeout");
//			}
//			catch (HystrixException)
//			{
//				Assert.IsTrue(r1.ResponseTimedOut);
//				// what we want
//			}
//
//			RequestCacheTimeoutWithoutFallback r2 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);
//			try
//			{
//				r2.execute();
//				// we should have thrown an exception
//				Assert.Fail("expected a timeout");
//			}
//			catch (HystrixException)
//			{
//				Assert.IsTrue(r2.ResponseTimedOut);
//				// what we want
//			}
//
//			RequestCacheTimeoutWithoutFallback r3 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);
//			Future<bool?> f3 = r3.queue();
//			try
//			{
//				f3.get();
//				// we should have thrown an exception
//				Assert.Fail("expected a timeout");
//			}
//			catch (ExecutionException e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.IsTrue(r3.ResponseTimedOut);
//				// what we want
//			}
//
//			Thread.Sleep(500); // timeout on command is set to 200ms
//
//			RequestCacheTimeoutWithoutFallback r4 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);
//			try
//			{
//				r4.execute();
//				// we should have thrown an exception
//				Assert.Fail("expected a timeout");
//			}
//			catch (HystrixException)
//			{
//				Assert.IsTrue(r4.ResponseTimedOut);
//				Assert.IsFalse(r4.IsResponseFromFallback());
//				// what we want
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(4, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//
//
//
//		public void testRequestCacheOnThreadRejectionThrowsException()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			CountDownLatch completionLatch = new CountDownLatch(1);
//			RequestCacheThreadRejectionWithoutFallback r1 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);
//			try
//			{
//				Console.WriteLine("r1: " + r1.execute());
//				// we should have thrown an exception
//				Assert.Fail("expected a rejection");
//			}
//			catch (HystrixException)
//			{
//				Assert.IsTrue(r1.ResponseRejected);
//				// what we want
//			}
//
//			RequestCacheThreadRejectionWithoutFallback r2 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);
//			try
//			{
//				Console.WriteLine("r2: " + r2.execute());
//				// we should have thrown an exception
//				Assert.Fail("expected a rejection");
//			}
//			catch (HystrixException)
//			{
//				//                e.printStackTrace();
//				Assert.IsTrue(r2.ResponseRejected);
//				// what we want
//			}
//
//			RequestCacheThreadRejectionWithoutFallback r3 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);
//			try
//			{
//				Console.WriteLine("f3: " + r3.queue().get());
//				// we should have thrown an exception
//				Assert.Fail("expected a rejection");
//			}
//			catch (HystrixException)
//			{
//				//                e.printStackTrace();
//				Assert.IsTrue(r3.ResponseRejected);
//				// what we want
//			}
//
//			// let the command finish (only 1 should actually be blocked on this due to the response cache)
//			completionLatch.countDown();
//
//			// then another after the command has completed
//			RequestCacheThreadRejectionWithoutFallback r4 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);
//			try
//			{
//				Console.WriteLine("r4: " + r4.execute());
//				// we should have thrown an exception
//				Assert.Fail("expected a rejection");
//			}
//			catch (HystrixException)
//			{
//				//                e.printStackTrace();
//				Assert.IsTrue(r4.ResponseRejected);
//				Assert.IsFalse(r4.IsResponseFromFallback());
//				// what we want
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(3, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, circuitBreaker.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(4, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//		/// <summary>
//		/// Test that we can do basic execution without a RequestVariable being initialized.
//		/// </summary>
//
//
//		public void testBasicExecutionWorksWithoutRequestVariable()
//		{
//			try
//			{
//				/* force the RequestVariable to not be initialized */
//				HystrixRequestContext.ContextOnCurrentThread = null;
//
//				TestHystrixCommand<bool?> command = new SuccessfulTestCommand();
//				Assert.AreEqual(true, command.Run());
//
//				TestHystrixCommand<bool?> command2 = new SuccessfulTestCommand();
//				Assert.AreEqual(true, command2.queue().get());
//
//				// we should be able to execute without a RequestVariable if ...
//				// 1) We don't have a cacheKey
//				// 2) We don't ask for the RequestLog
//				// 3) We don't do collapsing
//
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We received an exception => " + e.Message);
//			}
//		}
//
//		/// <summary>
//		/// Test that if we try and execute a command with a cacheKey without initializing RequestVariable that it gives an error.
//		/// </summary>
//
//
//		public void testCacheKeyExecutionRequiresRequestVariable()
//		{
//			try
//			{
//				/* force the RequestVariable to not be initialized */
//				HystrixRequestContext.ContextOnCurrentThread = null;
//
//				TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//
//				SuccessfulCacheableCommand command = new SuccessfulCacheableCommand(circuitBreaker, true, "one");
//				Assert.AreEqual(true, command.Run());
//
//				SuccessfulCacheableCommand command2 = new SuccessfulCacheableCommand(circuitBreaker, true, "two");
//				Assert.AreEqual(true, command2.queue().get());
//
//				Assert.Fail("We expect an exception because cacheKey requires RequestVariable.");
//
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//			}
//		}
//
//		/// <summary>
//		/// Test that a BadRequestException can be thrown and not count towards errors and bypasses fallback.
//		/// </summary>
//
//
//		public void testBadRequestExceptionViaExecuteInThread()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			try
//			{
//				(new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.THREAD)).execute();
//				Assert.Fail("we expect to receive a " + typeof(HystrixBadRequestException).Name);
//			}
//			catch (HystrixBadRequestException e)
//			{
//				// success
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We expect a " + typeof(HystrixBadRequestException).Name + " but got a " + e.GetType().Name);
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//		}
//
//		/// <summary>
//		/// Test that a BadRequestException can be thrown and not count towards errors and bypasses fallback.
//		/// </summary>
//
//
//		public void testBadRequestExceptionViaQueueInThread()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			try
//			{
//				(new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.THREAD)).queue().get();
//				Assert.Fail("we expect to receive a " + typeof(HystrixBadRequestException).Name);
//			}
//			catch (ExecutionException e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				if (e.InnerException is HystrixBadRequestException)
//				{
//					// success    
//				}
//				else
//				{
//					Assert.Fail("We expect a " + typeof(HystrixBadRequestException).Name + " but got a " + e.GetType().Name);
//				}
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail();
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//		}
//
//		/// <summary>
//		/// Test that BadRequestException behavior works the same on a cached response.
//		/// </summary>
//
//
//		public void testBadRequestExceptionViaQueueInThreadOnResponseFromCache()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//
//			// execute once to cache the value
//			try
//			{
//				(new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.THREAD)).execute();
//			}
//			catch (Exception)
//			{
//				// ignore
//			}
//
//			try
//			{
//				(new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.THREAD)).queue().get();
//				Assert.Fail("we expect to receive a " + typeof(HystrixBadRequestException).Name);
//			}
//			catch (ExecutionException e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				if (e.InnerException is HystrixBadRequestException)
//				{
//					// success    
//				}
//				else
//				{
//					Assert.Fail("We expect a " + typeof(HystrixBadRequestException).Name + " but got a " + e.GetType().Name);
//				}
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail();
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//		}
//
//		/// <summary>
//		/// Test that a BadRequestException can be thrown and not count towards errors and bypasses fallback.
//		/// </summary>
//
//
//		public void testBadRequestExceptionViaExecuteInSemaphore()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			try
//			{
//				(new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.SEMAPHORE)).execute();
//				Assert.Fail("we expect to receive a " + typeof(HystrixBadRequestException).Name);
//			}
//			catch (HystrixBadRequestException e)
//			{
//				// success
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We expect a " + typeof(HystrixBadRequestException).Name + " but got a " + e.GetType().Name);
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//		}
//
//		/// <summary>
//		/// Test that a BadRequestException can be thrown and not count towards errors and bypasses fallback.
//		/// </summary>
//
//
//		public void testBadRequestExceptionViaQueueInSemaphore()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			try
//			{
//				(new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.SEMAPHORE)).queue().get();
//				Assert.Fail("we expect to receive a " + typeof(HystrixBadRequestException).Name);
//			}
//			catch (ExecutionException e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				if (e.InnerException is HystrixBadRequestException)
//				{
//					// success    
//				}
//				else
//				{
//					Assert.Fail("We expect a " + typeof(HystrixBadRequestException).Name + " but got a " + e.GetType().Name);
//				}
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail();
//			}
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//		}
//
//		/// <summary>
//		/// Test a checked Exception being thrown
//		/// </summary>
//
//
//		public void testCheckedExceptionViaExecute()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			CommandWithCheckedException command = new CommandWithCheckedException(circuitBreaker);
//			try
//			{
//				command.Run();
//				Assert.Fail("we expect to receive a " + typeof(Exception).Name);
//			}
//			catch (Exception e)
//			{
//				Assert.AreEqual("simulated checked exception message", e.InnerException.Message);
//			}
//
//			Assert.AreEqual("simulated checked exception message", command.IsFailedExecution()Exception.Message);
//
//			Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(command.IsFailedExecution());
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//		}
//
//		/// <summary>
//		/// Test a java.lang.Error being thrown
//		/// </summary>
//		/// <exception cref="InterruptedException"> </exception>
//
//
//
//		public void testCheckedExceptionViaObserve()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			CommandWithCheckedException command = new CommandWithCheckedException(circuitBreaker);
//
//
//			AtomicReference<Exception> t = new AtomicReference<Exception>();
//
//
//			CountDownLatch latch = new CountDownLatch(1);
//			try
//			{
//				command.observe().subscribe(new ObserverAnonymousInnerClassHelper4(this, t, latch));
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("we should not get anything thrown, it should be emitted via the Observer#onError method");
//			}
//
//			latch.@await(1, TimeUnit.SECONDS);
//			Assert.IsNotNull(t.get());
//			t.get().printStackTrace();
//
//			Assert.IsTrue(t.get() is HystrixException);
//			Assert.AreEqual("simulated checked exception message", t.get().Cause.Message);
//			Assert.AreEqual("simulated checked exception message", command.IsFailedExecution()Exception.Message);
//
//			Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(command.IsFailedExecution());
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//		}
//
//		private class ObserverAnonymousInnerClassHelper4 : Observer<bool?>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private AtomicReference<Exception> t;
//			private CountDownLatch latch;
//
//			public ObserverAnonymousInnerClassHelper4(HystrixCommandTest outerInstance, AtomicReference<Exception> t, CountDownLatch latch)
//			{
//				this.outerInstance = outerInstance;
//				this.t = t;
//				this.latch = latch;
//			}
//
//
//			public override void onCompleted()
//			{
//				latch.countDown();
//			}
//
//			public override void onError(Exception e)
//			{
//				t.set(e);
//				latch.countDown();
//			}
//
//			public override void onNext(bool? args)
//			{
//
//			}
//
//		}
//
//		/// <summary>
//		/// Test a java.lang.Error being thrown
//		/// </summary>
//
//
//		public void testErrorThrownViaExecute()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			CommandWithErrorThrown command = new CommandWithErrorThrown(circuitBreaker);
//			try
//			{
//				command.Run();
//				Assert.Fail("we expect to receive a " + typeof(Exception).Name);
//			}
//			catch (Exception e)
//			{
//				// the actual error is an extra cause level deep because Hystrix needs to wrap Throwable/Error as it's public
//				// methods only support Exception and it's not a strong enough reason to break backwards compatibility and jump to version 2.x
//				// so HystrixException -> wrapper Exception -> actual Error
//				Assert.AreEqual("simulated java.lang.Error message", e.InnerException.InnerException.Message);
//			}
//
//			Assert.AreEqual("simulated java.lang.Error message", command.IsFailedExecution()Exception.Cause.Message);
//
//			Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(command.IsFailedExecution());
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//		}
//
//		/// <summary>
//		/// Test a java.lang.Error being thrown
//		/// </summary>
//
//
//		public void testErrorThrownViaQueue()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			CommandWithErrorThrown command = new CommandWithErrorThrown(circuitBreaker);
//			try
//			{
//				command.queue().get();
//				Assert.Fail("we expect to receive an Exception");
//			}
//			catch (Exception e)
//			{
//				// one cause down from ExecutionException to HystrixRuntime
//				// then the actual error is an extra cause level deep because Hystrix needs to wrap Throwable/Error as it's public
//				// methods only support Exception and it's not a strong enough reason to break backwards compatibility and jump to version 2.x
//				// so ExecutionException -> HystrixException -> wrapper Exception -> actual Error
//				Assert.AreEqual("simulated java.lang.Error message", e.InnerException.InnerException.Cause.Message);
//			}
//
//			Assert.AreEqual("simulated java.lang.Error message", command.IsFailedExecution()Exception.Cause.Message);
//
//			Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(command.IsFailedExecution());
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//		}
//
//		/// <summary>
//		/// Test a java.lang.Error being thrown
//		/// </summary>
//		/// <exception cref="InterruptedException"> </exception>
//
//
//
//		public void testErrorThrownViaObserve()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			CommandWithErrorThrown command = new CommandWithErrorThrown(circuitBreaker);
//
//
//			AtomicReference<Exception> t = new AtomicReference<Exception>();
//
//
//			CountDownLatch latch = new CountDownLatch(1);
//			try
//			{
//				command.observe().subscribe(new ObserverAnonymousInnerClassHelper5(this, t, latch));
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("we should not get anything thrown, it should be emitted via the Observer#onError method");
//			}
//
//			latch.@await(1, TimeUnit.SECONDS);
//			Assert.IsNotNull(t.get());
//			t.get().printStackTrace();
//
//			Assert.IsTrue(t.get() is HystrixException);
//			// the actual error is an extra cause level deep because Hystrix needs to wrap Throwable/Error as it's public
//			// methods only support Exception and it's not a strong enough reason to break backwards compatibility and jump to version 2.x
//			Assert.AreEqual("simulated java.lang.Error message", t.get().Cause.Cause.Message);
//			Assert.AreEqual("simulated java.lang.Error message", command.IsFailedExecution()Exception.Cause.Message);
//
//			Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(command.IsFailedExecution());
//
//			Assert.AreEqual(0, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(1, circuitBreaker.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//		}
//
//		private class ObserverAnonymousInnerClassHelper5 : Observer<bool?>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private AtomicReference<Exception> t;
//			private CountDownLatch latch;
//
//			public ObserverAnonymousInnerClassHelper5(HystrixCommandTest outerInstance, AtomicReference<Exception> t, CountDownLatch latch)
//			{
//				this.outerInstance = outerInstance;
//				this.t = t;
//				this.latch = latch;
//			}
//
//
//			public override void onCompleted()
//			{
//				latch.countDown();
//			}
//
//			public override void onError(Exception e)
//			{
//				t.set(e);
//				latch.countDown();
//			}
//
//			public override void onNext(bool? args)
//			{
//
//			}
//
//		}
//
//		/// <summary>
//		/// Execution hook on successful execution
//		/// </summary>
//
//
//		public void testExecutionHookSuccessfulCommand()
//		{
//			/* test with execute() */
//			TestHystrixCommand<bool?> command = new SuccessfulTestCommand();
//			command.Run();
//
//			Console.WriteLine("hook: " + command.executionHook);
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we expect a successful response from run()
//			Assert.IsNotNull(command.executionHook.runSuccessResponse);
//			// we do not expect an exception
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should not be run as we were successful
//			Assert.AreEqual(0, command.executionHook.startFallback.get());
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// the execute() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response from execute() since run() succeeded
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception since run() succeeded
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//
//			/* test with queue() */
//			command = new SuccessfulTestCommand();
//			try
//			{
//				command.queue().get();
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we expect a successful response from run()
//			Assert.IsNotNull(command.executionHook.runSuccessResponse);
//			// we do not expect an exception
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should not be run as we were successful
//			Assert.AreEqual(0, command.executionHook.startFallback.get());
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// the queue() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response from queue() since run() succeeded
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception since run() succeeded
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on successful execution with "fire and forget" approach
//		/// </summary>
//
//
//		public void testExecutionHookSuccessfulCommandViaFireAndForget()
//		{
//			TestHystrixCommand<bool?> command = new SuccessfulTestCommand();
//			try
//			{
//				// do not block on "get()" ... fire this asynchronously
//				command.queue();
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			// wait for command to execute without calling get on the future
//			while (!command.IsExecutionComplete())
//			{
//				try
//				{
//					Thread.Sleep(10);
//				}
//				catch (InterruptedException)
//				{
//					throw new Exception("interrupted");
//				}
//			}
//
//			/*
//			 * All the hooks should still work even though we didn't call get() on the future
//			 */
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we expect a successful response from run()
//			Assert.IsNotNull(command.executionHook.runSuccessResponse);
//			// we do not expect an exception
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should not be run as we were successful
//			Assert.AreEqual(0, command.executionHook.startFallback.get());
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// the queue() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response from queue() since run() succeeded
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception since run() succeeded
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on successful execution with multiple get() calls to Future
//		/// </summary>
//
//
//		public void testExecutionHookSuccessfulCommandWithMultipleGetsOnFuture()
//		{
//			TestHystrixCommand<bool?> command = new SuccessfulTestCommand();
//			try
//			{
//				Future<bool?> f = command.queue();
//				f.get();
//				f.get();
//				f.get();
//				f.get();
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			/*
//			 * Despite multiple calls to get() we should only have 1 call to the hooks.
//			 */
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we expect a successful response from run()
//			Assert.IsNotNull(command.executionHook.runSuccessResponse);
//			// we do not expect an exception
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should not be run as we were successful
//			Assert.AreEqual(0, command.executionHook.startFallback.get());
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// the queue() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response from queue() since run() succeeded
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception since run() succeeded
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on Assert.Failed execution without a fallback
//		/// </summary>
//
//
//		public void testExecutionHookRunFailureWithoutFallback()
//		{
//			/* test with execute() */
//			TestHystrixCommand<bool?> command = new UnknownFailureTestCommandWithoutFallback();
//			try
//			{
//				command.Run();
//				Assert.Fail("Expecting exception");
//			}
//			catch (Exception)
//			{
//				// ignore
//			}
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we should not have a response
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should have an exception
//			Assert.IsNotNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run since run() Assert.Failed
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// no response since fallback is not implemented
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// not null since it's not implemented and throws an exception
//			Assert.IsNotNull(command.executionHook.fallbackFailureException);
//
//			// the execute() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should not have a response from execute() since we do not have a fallback and run() Assert.Failed
//			assertNull(command.executionHook.endExecuteSuccessResponse);
//			// we should have an exception since run() Assert.Failed
//			Assert.IsNotNull(command.executionHook.endExecuteFailureException);
//			// run() Assert.Failure
//			Assert.AreEqual(HystrixException.FailureType.COMMAND_EXCEPTION, command.executionHook.endExecuteFailureType);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//
//			/* test with queue() */
//			command = new UnknownFailureTestCommandWithoutFallback();
//			try
//			{
//				command.queue().get();
//				Assert.Fail("Expecting exception");
//			}
//			catch (Exception)
//			{
//				// ignore
//			}
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we should not have a response
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should have an exception
//			Assert.IsNotNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run since run() Assert.Failed
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// no response since fallback is not implemented
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// not null since it's not implemented and throws an exception
//			Assert.IsNotNull(command.executionHook.fallbackFailureException);
//
//			// the queue() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should not have a response from queue() since we do not have a fallback and run() Assert.Failed
//			assertNull(command.executionHook.endExecuteSuccessResponse);
//			// we should have an exception since run() Assert.Failed
//			Assert.IsNotNull(command.executionHook.endExecuteFailureException);
//			// run() Assert.Failure
//			Assert.AreEqual(HystrixException.FailureType.COMMAND_EXCEPTION, command.executionHook.endExecuteFailureType);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//
//		}
//
//		/// <summary>
//		/// Execution hook on Assert.Failed execution with a fallback
//		/// </summary>
//
//
//		public void testExecutionHookRunFailureWithFallback()
//		{
//			/* test with execute() */
//			TestHystrixCommand<bool?> command = new KnownFailureTestCommandWithFallback(new TestCircuitBreaker());
//			command.Run();
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we should not have a response from run since run() Assert.Failed
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should have an exception since run() Assert.Failed
//			Assert.IsNotNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run since run() Assert.Failed
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// a response since fallback is implemented
//			Assert.IsNotNull(command.executionHook.fallbackSuccessResponse);
//			// null since it's implemented and succeeds
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// the execute() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response from execute() since we expect a fallback despite Assert.Failure of run()
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception because we expect a fallback
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//
//			/* test with queue() */
//			command = new KnownFailureTestCommandWithFallback(new TestCircuitBreaker());
//			try
//			{
//				command.queue().get();
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we should not have a response from run since run() Assert.Failed
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should have an exception since run() Assert.Failed
//			Assert.IsNotNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run since run() Assert.Failed
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// a response since fallback is implemented
//			Assert.IsNotNull(command.executionHook.fallbackSuccessResponse);
//			// null since it's implemented and succeeds
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// the queue() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response from queue() since we expect a fallback despite Assert.Failure of run()
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception because we expect a fallback
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on Assert.Failed execution with a fallback Assert.Failure
//		/// </summary>
//
//
//		public void testExecutionHookRunFailureWithFallbackFailure()
//		{
//			/* test with execute() */
//			TestHystrixCommand<bool?> command = new KnownFailureTestCommandWithFallbackFailure();
//			try
//			{
//				command.Run();
//				Assert.Fail("Expecting exception");
//			}
//			catch (Exception)
//			{
//				// ignore
//			}
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we should not have a response because run() and fallback Assert.Fail
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should have an exception because run() and fallback Assert.Fail
//			Assert.IsNotNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run since run() Assert.Failed
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// no response since fallback Assert.Fails
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// not null since it's implemented but Assert.Fails
//			Assert.IsNotNull(command.executionHook.fallbackFailureException);
//
//			// the execute() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should not have a response because run() and fallback Assert.Fail
//			assertNull(command.executionHook.endExecuteSuccessResponse);
//			// we should have an exception because run() and fallback Assert.Fail
//			Assert.IsNotNull(command.executionHook.endExecuteFailureException);
//			// run() Assert.Failure
//			Assert.AreEqual(HystrixException.FailureType.COMMAND_EXCEPTION, command.executionHook.endExecuteFailureType);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//
//			/* test with queue() */
//			command = new KnownFailureTestCommandWithFallbackFailure();
//			try
//			{
//				command.queue().get();
//				Assert.Fail("Expecting exception");
//			}
//			catch (Exception)
//			{
//				// ignore
//			}
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we should not have a response because run() and fallback Assert.Fail
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should have an exception because run() and fallback Assert.Fail
//			Assert.IsNotNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run since run() Assert.Failed
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// no response since fallback Assert.Fails
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// not null since it's implemented but Assert.Fails
//			Assert.IsNotNull(command.executionHook.fallbackFailureException);
//
//			// the queue() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should not have a response because run() and fallback Assert.Fail
//			assertNull(command.executionHook.endExecuteSuccessResponse);
//			// we should have an exception because run() and fallback Assert.Fail
//			Assert.IsNotNull(command.executionHook.endExecuteFailureException);
//			// run() Assert.Failure
//			Assert.AreEqual(HystrixException.FailureType.COMMAND_EXCEPTION, command.executionHook.endExecuteFailureType);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on timeout without a fallback
//		/// </summary>
//
//
//		public void testExecutionHookTimeoutWithoutFallback()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_NOT_IMPLEMENTED);
//			try
//			{
//				command.queue().get();
//				Assert.Fail("Expecting exception");
//			}
//			catch (Exception)
//			{
//				// ignore
//			}
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we should not have a response because of timeout and no fallback
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should not have an exception because run() didn't Assert.Fail, it timed out
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run due to timeout
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// no response since no fallback
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// not null since no fallback implementation
//			Assert.IsNotNull(command.executionHook.fallbackFailureException);
//
//			// execution occurred
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should not have a response because of timeout and no fallback
//			assertNull(command.executionHook.endExecuteSuccessResponse);
//			// we should have an exception because of timeout and no fallback
//			Assert.IsNotNull(command.executionHook.endExecuteFailureException);
//			// timeout Assert.Failure
//			Assert.AreEqual(HystrixException.FailureType.TIMEOUT, command.executionHook.endExecuteFailureType);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//
//			// we need to wait for the thread to complete before the onThreadComplete hook will be called
//			try
//			{
//				Thread.Sleep(2000);
//			}
//			catch (InterruptedException)
//			{
//				// ignore
//			}
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on timeout with a fallback
//		/// </summary>
//
//
//		public void testExecutionHookTimeoutWithFallback()
//		{
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_SUCCESS);
//			try
//			{
//				command.queue().get();
//			}
//			catch (Exception e)
//			{
//				throw new Exception("not expecting", e);
//			}
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we should not have a response because of timeout
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should not have an exception because run() didn't Assert.Fail, it timed out
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run due to timeout
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// response since we have a fallback
//			Assert.IsNotNull(command.executionHook.fallbackSuccessResponse);
//			// null since fallback succeeds
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// execution occurred
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response because of fallback
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception because of fallback
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(1, command.executionHook.threadStart.get());
//
//			// we need to wait for the thread to complete before the onThreadComplete hook will be called
//			try
//			{
//				Thread.Sleep(2000);
//			}
//			catch (InterruptedException)
//			{
//				// ignore
//			}
//			Assert.AreEqual(1, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on rejected with a fallback
//		/// </summary>
//
//
//		public void testExecutionHookRejectedWithFallback()
//		{
//			TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
//			SingleThreadedPool pool = new SingleThreadedPool(1);
//
//			try
//			{
//				// fill the queue
//				(new TestCommandRejection(circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_SUCCESS)).queue();
//				(new TestCommandRejection(circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_SUCCESS)).queue();
//			}
//			catch (Exception)
//			{
//				// ignore
//			}
//
//			TestCommandRejection command = new TestCommandRejection(circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_SUCCESS);
//			try
//			{
//				// now execute one that will be rejected
//				command.queue().get();
//			}
//			catch (Exception e)
//			{
//				throw new Exception("not expecting", e);
//			}
//
//			Assert.IsTrue(command.ResponseRejected);
//
//			// the run() method should not run as we're rejected
//			Assert.AreEqual(0, command.executionHook.startRun.get());
//			// we should not have a response because of rejection
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should not have an exception because we didn't run
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run due to rejection
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// response since we have a fallback
//			Assert.IsNotNull(command.executionHook.fallbackSuccessResponse);
//			// null since fallback succeeds
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// execution occurred
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response because of fallback
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception because of fallback
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(0, command.executionHook.threadStart.get());
//			Assert.AreEqual(0, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on short-circuit with a fallback
//		/// </summary>
//
//
//		public void testExecutionHookShortCircuitedWithFallbackViaQueue()
//		{
//			TestCircuitBreaker circuitBreaker = (new TestCircuitBreaker()).setForceShortCircuit(true);
//			KnownFailureTestCommandWithoutFallback command = new KnownFailureTestCommandWithoutFallback(circuitBreaker);
//			try
//			{
//				// now execute one that will be short-circuited
//				command.queue().get();
//				Assert.Fail("we expect an error as there is no fallback");
//			}
//			catch (Exception)
//			{
//				// expecting
//			}
//
//			Assert.IsTrue(command.IsResponseShortCircuited());
//
//			// the run() method should not run as we're rejected
//			Assert.AreEqual(0, command.executionHook.startRun.get());
//			// we should not have a response because of rejection
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should not have an exception because we didn't run
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run due to rejection
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// no response since we don't have a fallback
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// not null since fallback Assert.Fails and throws an exception
//			Assert.IsNotNull(command.executionHook.fallbackFailureException);
//
//			// execution occurred
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should not have a response because fallback Assert.Fails
//			assertNull(command.executionHook.endExecuteSuccessResponse);
//			// we won't have an exception because short-circuit doesn't have one
//			assertNull(command.executionHook.endExecuteFailureException);
//			// but we do expect to receive a onError call with FailureType.SHORTCIRCUIT
//			Assert.AreEqual(HystrixException.FailureType.SHORTCIRCUIT, command.executionHook.endExecuteFailureType);
//
//			// thread execution
//			Assert.AreEqual(0, command.executionHook.threadStart.get());
//			Assert.AreEqual(0, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on short-circuit with a fallback
//		/// </summary>
//
//
//		public void testExecutionHookShortCircuitedWithFallbackViaExecute()
//		{
//			TestCircuitBreaker circuitBreaker = (new TestCircuitBreaker()).setForceShortCircuit(true);
//			KnownFailureTestCommandWithoutFallback command = new KnownFailureTestCommandWithoutFallback(circuitBreaker);
//			try
//			{
//				// now execute one that will be short-circuited
//				command.Run();
//				Assert.Fail("we expect an error as there is no fallback");
//			}
//			catch (Exception)
//			{
//				// expecting
//			}
//
//			Assert.IsTrue(command.IsResponseShortCircuited());
//
//			// the run() method should not run as we're rejected
//			Assert.AreEqual(0, command.executionHook.startRun.get());
//			// we should not have a response because of rejection
//			assertNull(command.executionHook.runSuccessResponse);
//			// we should not have an exception because we didn't run
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should be run due to rejection
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// no response since we don't have a fallback
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// not null since fallback Assert.Fails and throws an exception
//			Assert.IsNotNull(command.executionHook.fallbackFailureException);
//
//			// execution occurred
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should not have a response because fallback Assert.Fails
//			assertNull(command.executionHook.endExecuteSuccessResponse);
//			// we won't have an exception because short-circuit doesn't have one
//			assertNull(command.executionHook.endExecuteFailureException);
//			// but we do expect to receive a onError call with FailureType.SHORTCIRCUIT
//			Assert.AreEqual(HystrixException.FailureType.SHORTCIRCUIT, command.executionHook.endExecuteFailureType);
//
//			// thread execution
//			Assert.AreEqual(0, command.executionHook.threadStart.get());
//			Assert.AreEqual(0, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on successful execution with semaphore isolation
//		/// </summary>
//
//
//		public void testExecutionHookSuccessfulCommandWithSemaphoreIsolation()
//		{
//			/* test with execute() */
//			TestSemaphoreCommand command = new TestSemaphoreCommand(new TestCircuitBreaker(), 1, 10);
//			command.Run();
//
//			Assert.IsFalse(command.ExecutedInThread);
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we expect a successful response from run()
//			Assert.IsNotNull(command.executionHook.runSuccessResponse);
//			// we do not expect an exception
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should not be run as we were successful
//			Assert.AreEqual(0, command.executionHook.startFallback.get());
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// the execute() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response from execute() since run() succeeded
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception since run() succeeded
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(0, command.executionHook.threadStart.get());
//			Assert.AreEqual(0, command.executionHook.threadComplete.get());
//
//			/* test with queue() */
//			command = new TestSemaphoreCommand(new TestCircuitBreaker(), 1, 10);
//			try
//			{
//				command.queue().get();
//			}
//			catch (Exception e)
//			{
//				throw new Exception(e);
//			}
//
//			Assert.IsFalse(command.ExecutedInThread);
//
//			// the run() method should run as we're not short-circuited or rejected
//			Assert.AreEqual(1, command.executionHook.startRun.get());
//			// we expect a successful response from run()
//			Assert.IsNotNull(command.executionHook.runSuccessResponse);
//			// we do not expect an exception
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should not be run as we were successful
//			Assert.AreEqual(0, command.executionHook.startFallback.get());
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// null since it didn't run
//			assertNull(command.executionHook.fallbackFailureException);
//
//			// the queue() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should have a response from queue() since run() succeeded
//			Assert.IsNotNull(command.executionHook.endExecuteSuccessResponse);
//			// we should not have an exception since run() succeeded
//			assertNull(command.executionHook.endExecuteFailureException);
//
//			// thread execution
//			Assert.AreEqual(0, command.executionHook.threadStart.get());
//			Assert.AreEqual(0, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Execution hook on successful execution with semaphore isolation
//		/// </summary>
//
//
//		public void testExecutionHookFailureWithSemaphoreIsolation()
//		{
//			/* test with execute() */
//
//
//			TryableSemaphoreActual semaphore = new TryableSemaphoreActual(HystrixProperty.Factory.asProperty(0));
//
//			TestSemaphoreCommand command = new TestSemaphoreCommand(new TestCircuitBreaker(), semaphore, 200);
//			try
//			{
//				command.Run();
//				Assert.Fail("we expect a Assert.Failure");
//			}
//			catch (Exception)
//			{
//				// expected
//			}
//
//			Assert.IsFalse(command.ExecutedInThread);
//			Assert.IsTrue(command.ResponseRejected);
//
//			// the run() method should not run as we are rejected
//			Assert.AreEqual(0, command.executionHook.startRun.get());
//			// null as run() does not get invoked
//			assertNull(command.executionHook.runSuccessResponse);
//			// null as run() does not get invoked
//			assertNull(command.executionHook.runFailureException);
//
//			// the fallback() method should run because of rejection
//			Assert.AreEqual(1, command.executionHook.startFallback.get());
//			// null since there is no fallback
//			assertNull(command.executionHook.fallbackSuccessResponse);
//			// not null since the fallback is not implemented
//			Assert.IsNotNull(command.executionHook.fallbackFailureException);
//
//			// the execute() method was used
//			Assert.AreEqual(1, command.executionHook.startExecute.get());
//			// we should not have a response since fallback has nothing
//			assertNull(command.executionHook.endExecuteSuccessResponse);
//			// we won't have an exception because rejection doesn't have one
//			assertNull(command.executionHook.endExecuteFailureException);
//			// but we do expect to receive a onError call with FailureType.SHORTCIRCUIT
//			Assert.AreEqual(HystrixException.FailureType.REJECTED_SEMAPHORE_EXECUTION, command.executionHook.endExecuteFailureType);
//
//			// thread execution
//			Assert.AreEqual(0, command.executionHook.threadStart.get());
//			Assert.AreEqual(0, command.executionHook.threadComplete.get());
//		}
//
//		/// <summary>
//		/// Test a command execution that Assert.Fails but has a fallback.
//		/// </summary>
//
//
//		public void testExecutionFailureWithFallbackImplementedButDisabled()
//		{
//			TestHystrixCommand<bool?> commandEnabled = new KnownFailureTestCommandWithFallback(new TestCircuitBreaker(), true);
//			try
//			{
//				Assert.AreEqual(false, commandEnabled.execute());
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.ToString());
//				Console.Write(e.StackTrace);
//				Assert.Fail("We should have received a response from the fallback.");
//			}
//
//			TestHystrixCommand<bool?> commandDisabled = new KnownFailureTestCommandWithFallback(new TestCircuitBreaker(), false);
//			try
//			{
//				Assert.AreEqual(false, commandDisabled.execute());
//				Assert.Fail("expect exception thrown");
//			}
//			catch (Exception)
//			{
//				// expected
//			}
//
//			Assert.AreEqual("we Assert.Failed with a simulated issue", commandDisabled.IsFailedExecution()Exception.Message);
//
//			Assert.IsTrue(commandDisabled.IsFailedExecution());
//
//			Assert.AreEqual(0, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(1, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, commandDisabled.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(0, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, commandDisabled.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, commandDisabled.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(2, HystrixRequestLog.CurrentRequest.ExecutedCommands.size());
//		}
//
//
//
//		public void testExecutionTimeoutValue()
//		{
//			HystrixCommand.Setter properties = HystrixCommand.Setter.withGroupKey(HystrixCommandGroupKey.Factory.asKey("TestKey")).andCommandPropertiesDefaults(HystrixCommandProperties.Setter().withExecutionIsolationThreadTimeoutInMilliseconds(50));
//
//			HystrixCommand<string> command = new HystrixCommandAnonymousInnerClassHelper(this, properties);
//
//			string value = command.Run();
//			Assert.IsTrue(command.ResponseTimedOut);
//			Assert.AreEqual("expected fallback value", "timed-out", value);
//
//		}
//
//		private class HystrixCommandAnonymousInnerClassHelper : HystrixCommand<string>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			public HystrixCommandAnonymousInnerClassHelper(HystrixCommandTest outerInstance, HystrixCommand.Setter properties) : base(properties)
//			{
//				this.outerInstance = outerInstance;
//			}
//
//
//
//			protected internal override string run()
//			{
//				Thread.Sleep(3000);
//				// should never reach here
//				return "hello";
//			}
//
//			protected internal override string Fallback
//			{
//				get
//				{
//					if (ResponseTimedOut)
//					{
//						return "timed-out";
//					}
//					else
//					{
//						return "abc";
//					}
//				}
//			}
//		}
//
//		/// <summary>
//		/// See https://github.com/Netflix/Hystrix/issues/212
//		/// </summary>
//
//
//		public void testObservableTimeoutNoFallbackThreadContext()
//		{
//			TestSubscriber<bool?> ts = new TestSubscriber<bool?>();
//
//
//
//			AtomicReference<Thread> onErrorThread = new AtomicReference<Thread>();
//
//
//			AtomicBoolean isRequestContextInitialized = new AtomicBoolean();
//
//			TestHystrixCommand<bool?> command = new TestCommandWithTimeout(50, TestCommandWithTimeout.FALLBACK_NOT_IMPLEMENTED);
//			command.toObservable().doOnError(new Action1AnonymousInnerClassHelper(this, onErrorThread, isRequestContextInitialized)).subscribe(ts);
//
//			ts.awaitTerminalEvent();
//
//			Assert.IsTrue(isRequestContextInitialized.get());
//			Assert.IsTrue(onErrorThread.get().Name.StartsWith("RxComputationThreadPool"));
//
//			IList<Exception> errors = ts.OnErrorEvents;
//			Assert.AreEqual(1, errors.Count);
//			Exception e = errors[0];
//			if (errors[0] is HystrixException)
//			{
//				HystrixException de = (HystrixException) e;
//				Assert.IsNotNull(de.FallbackException);
//				Assert.IsTrue(de.FallbackException is System.NotSupportedException);
//				Assert.IsNotNull(de.CommandType);
//				Assert.IsNotNull(de.InnerException);
//				Assert.IsTrue(de.InnerException is TimeoutException);
//			}
//			else
//			{
//				Assert.Fail("the exception should be ExecutionException with cause as HystrixException");
//			}
//
//			Assert.IsTrue(command.GetExecutionTimeInMilliseconds() > -1);
//			Assert.IsTrue(command.ResponseTimedOut);
//
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Success));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ExceptionThrown));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Failed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackRejected));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackFailed));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.FallbackSuccess));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(HystrixRollingNumberEvent.SEMAPHORE_REJECTED));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ShortCircuited));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ThreadPoolRejected));
//			Assert.AreEqual(1, command.Metrics.GetRollingCount(CommandExecutionEventEnum.Timeout));
//			Assert.AreEqual(0, command.Metrics.GetRollingCount(CommandExecutionEventEnum.ResponseFromCache));
//
//			Assert.AreEqual(100, command.Metrics.GetErrorPercentage());
//
//			Assert.AreEqual(1, HystrixRequestLog.CurrentRequest.AllExecutedCommands.size());
//		}
//
//		private class Action1AnonymousInnerClassHelper : Action1<Exception>
//		{
//			private readonly HystrixCommandTest outerInstance;
//
//			private AtomicReference<Thread> onErrorThread;
//			private AtomicBoolean isRequestContextInitialized;
//
//			public Action1AnonymousInnerClassHelper(HystrixCommandTest outerInstance, AtomicReference<Thread> onErrorThread, AtomicBoolean isRequestContextInitialized)
//			{
//				this.outerInstance = outerInstance;
//				this.onErrorThread = onErrorThread;
//				this.isRequestContextInitialized = isRequestContextInitialized;
//			}
//
//
//			public override void call(Exception t1)
//			{
//				Console.WriteLine("onError: " + t1);
//				Console.WriteLine("onError Thread: " + Thread.CurrentThread);
//				Console.WriteLine("ThreadContext in onError: " + HystrixRequestContext.CurrentThreadInitialized);
//				onErrorThread.set(Thread.CurrentThread);
//				isRequestContextInitialized.set(HystrixRequestContext.CurrentThreadInitialized);
//			}
//
//		}
//
//		/* ******************************************************************************** */
//		/* ******************************************************************************** */
//		/* private HystrixCommand class implementations for unit testing */
//		/* ******************************************************************************** */
//		/* ******************************************************************************** */
//
//		/// <summary>
//		/// Used by UnitTest command implementations to provide base defaults for constructor and a builder pattern for the arguments being passed in.
//		/// </summary>
//		/* package */	internal abstract class TestHystrixCommand<K> : HystrixCommand<K>
//	{
//
//			internal readonly TestCommandBuilder builder;
//
//			internal TestHystrixCommand(TestCommandBuilder builder) : base(owner, dependencyKey, threadPoolKey, circuitBreaker, threadPool, commandPropertiesDefaults, threadPoolPropertiesDefaults, metrics, fallbackSemaphore, executionSemaphore, TEST_PROPERTIES_FACTORY, executionHook)
//			{
//				this.builder = builder;
//			}
//
//			internal static TestCommandBuilder testPropsBuilder()
//			{
//				return new TestCommandBuilder();
//			}
//
//			internal class TestCommandBuilder
//			{
//				internal bool InstanceFieldsInitialized = false;
//
//				public TestCommandBuilder()
//				{
//					if (!InstanceFieldsInitialized)
//					{
//						InitializeInstanceFields();
//						InstanceFieldsInitialized = true;
//					}
//				}
//
//				internal void InitializeInstanceFields()
//				{
//					circuitBreaker = _cb;
//					metrics = _cb.Metrics;
//				}
//
//				internal TestCircuitBreaker _cb = new TestCircuitBreaker();
//				internal HystrixCommandGroupKey owner = CommandGroupForUnitTest.OWNER_ONE;
//				internal HystrixCommandKey dependencyKey = null;
//				internal HystrixThreadPoolKey threadPoolKey = null;
//				internal HystrixCircuitBreaker circuitBreaker;
//				internal HystrixThreadPool threadPool = null;
//				internal HystrixCommandProperties.Setter commandPropertiesDefaults = HystrixCommandPropertiesTest.UnitTestPropertiesSetter;
//				internal HystrixThreadPoolProperties.Setter threadPoolPropertiesDefaults = HystrixThreadPoolProperties.Setter.UnitTestPropertiesBuilder;
//				internal HystrixCommandMetrics metrics;
//				internal TryableSemaphore fallbackSemaphore = null;
//				internal TryableSemaphore executionSemaphore = null;
//				internal TestExecutionHook executionHook = new TestExecutionHook();
//
//				internal TestCommandBuilder setOwner(HystrixCommandGroupKey owner)
//				{
//					this.owner = owner;
//					return this;
//				}
//
//				internal TestCommandBuilder setCommandKey(HystrixCommandKey dependencyKey)
//				{
//					this.dependencyKey = dependencyKey;
//					return this;
//				}
//
//				internal TestCommandBuilder setThreadPoolKey(HystrixThreadPoolKey threadPoolKey)
//				{
//					this.threadPoolKey = threadPoolKey;
//					return this;
//				}
//
//				internal TestCommandBuilder setCircuitBreaker(HystrixCircuitBreaker circuitBreaker)
//				{
//					this.circuitBreaker = circuitBreaker;
//					return this;
//				}
//
//				internal TestCommandBuilder setThreadPool(HystrixThreadPool threadPool)
//				{
//					this.threadPool = threadPool;
//					return this;
//				}
//
//				internal TestCommandBuilder setCommandPropertiesDefaults(HystrixCommandProperties.Setter commandPropertiesDefaults)
//				{
//					this.commandPropertiesDefaults = commandPropertiesDefaults;
//					return this;
//				}
//
//				internal TestCommandBuilder setThreadPoolPropertiesDefaults(HystrixThreadPoolProperties.Setter threadPoolPropertiesDefaults)
//				{
//					this.threadPoolPropertiesDefaults = threadPoolPropertiesDefaults;
//					return this;
//				}
//
//				internal TestCommandBuilder setMetrics(HystrixCommandMetrics metrics)
//				{
//					this.Metrics = metrics;
//					return this;
//				}
//
//				internal TestCommandBuilder setFallbackSemaphore(TryableSemaphore fallbackSemaphore)
//				{
//					this.fallbackSemaphore = fallbackSemaphore;
//					return this;
//				}
//
//				internal TestCommandBuilder setExecutionSemaphore(TryableSemaphore executionSemaphore)
//				{
//					this.executionSemaphore = executionSemaphore;
//					return this;
//				}
//
//			}
//
//	}
//
//		/// <summary>
//		/// Successful execution - no fallback implementation.
//		/// </summary>
//		private class SuccessfulTestCommand : TestHystrixCommand<bool?>
//		{
//
//			public SuccessfulTestCommand() : this(HystrixCommandPropertiesTest.UnitTestPropertiesSetter)
//			{
//			}
//
//			public SuccessfulTestCommand(HystrixCommandProperties.Setter properties) : base(testPropsBuilder().setCommandPropertiesDefaults(properties))
//			{
//			}
//
//			protected internal override bool? run()
//			{
//				return true;
//			}
//
//		}
//
//		/// <summary>
//		/// Successful execution - no fallback implementation.
//		/// </summary>
//		private class DynamicOwnerTestCommand : TestHystrixCommand<bool?>
//		{
//
//			public DynamicOwnerTestCommand(HystrixCommandGroupKey owner) : base(testPropsBuilder().setOwner(owner))
//			{
//			}
//
//			protected internal override bool? run()
//			{
//				Console.WriteLine("successfully executed");
//				return true;
//			}
//
//		}
//
//		/// <summary>
//		/// Successful execution - no fallback implementation.
//		/// </summary>
//		private class DynamicOwnerAndKeyTestCommand : TestHystrixCommand<bool?>
//		{
//
//			public DynamicOwnerAndKeyTestCommand(HystrixCommandGroupKey owner, HystrixCommandKey key) : base(testPropsBuilder().setOwner(owner).setCommandKey(key).setCircuitBreaker(null).setMetrics(null))
//			{
//				// we specifically are NOT passing in a circuit breaker here so we test that it creates a new one correctly based on the dynamic key
//			}
//
//			protected internal override bool? run()
//			{
//				Console.WriteLine("successfully executed");
//				return true;
//			}
//
//		}
//


//

//

//
//		/// <summary>
//		/// A HystrixCommand implementation that supports caching.
//		/// </summary>
//		private class SuccessfulCacheableCommand : TestHystrixCommand<string>
//		{
//
//			internal readonly bool cacheEnabled;
//			internal volatile bool executed = false;
//			internal readonly string value;
//
//			public SuccessfulCacheableCommand(TestCircuitBreaker circuitBreaker, bool cacheEnabled, string value) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics))
//			{
//				this.value = value;
//				this.cacheEnabled = cacheEnabled;
//			}
//
//			protected internal override string run()
//			{
//				executed = true;
//				Console.WriteLine("successfully executed");
//				return value;
//			}
//
//			public bool CommandRunningInThread
//			{
//				get
//				{
//					return base.Properties.executionIsolationStrategy().get().Equals(ExecutionIsolationStrategy.THREAD);
//				}
//			}
//
//			public override string CacheKey
//			{
//				get
//				{
//					if (cacheEnabled)
//					{
//						return value;
//					}
//					else
//					{
//						return null;
//					}
//				}
//			}
//		}
//
//		/// <summary>
//		/// A HystrixCommand implementation that supports caching.
//		/// </summary>
//		private class SuccessfulCacheableCommandViaSemaphore : TestHystrixCommand<string>
//		{
//
//			internal readonly bool cacheEnabled;
//			internal volatile bool executed = false;
//			internal readonly string value;
//
//			public SuccessfulCacheableCommandViaSemaphore(TestCircuitBreaker circuitBreaker, bool cacheEnabled, string value) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationStrategy(ExecutionIsolationStrategy.SEMAPHORE)))
//			{
//				this.value = value;
//				this.cacheEnabled = cacheEnabled;
//			}
//
//			protected internal override string run()
//			{
//				executed = true;
//				Console.WriteLine("successfully executed");
//				return value;
//			}
//
//			public bool CommandRunningInThread
//			{
//				get
//				{
//					return base.Properties.executionIsolationStrategy().get().Equals(ExecutionIsolationStrategy.THREAD);
//				}
//			}
//
//			public override string CacheKey
//			{
//				get
//				{
//					if (cacheEnabled)
//					{
//						return value;
//					}
//					else
//					{
//						return null;
//					}
//				}
//			}
//		}
//
//		/// <summary>
//		/// A HystrixCommand implementation that supports caching and execution takes a while.
//		/// <para>
//		/// Used to test scenario where Futures are returned with a backing call still executing.
//		/// </para>
//		/// </summary>
//		private class SlowCacheableCommand : TestHystrixCommand<string>
//		{
//
//			internal readonly string value;
//			internal readonly int duration;
//			internal volatile bool executed = false;
//
//			public SlowCacheableCommand(TestCircuitBreaker circuitBreaker, string value, int duration) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics))
//			{
//				this.value = value;
//				this.duration = duration;
//			}
//
//			protected internal override string run()
//			{
//				executed = true;
//				try
//				{
//					Thread.Sleep(duration);
//				}
//				catch (Exception e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//				Console.WriteLine("successfully executed");
//				return value;
//			}
//
//			public override string CacheKey
//			{
//				get
//				{
//					return value;
//				}
//			}
//		}
//
//		/// <summary>
//		/// Successful execution - no fallback implementation, circuit-breaker disabled.
//		/// </summary>
//		private class TestCommandWithoutCircuitBreaker : TestHystrixCommand<bool?>
//		{
//
//			internal TestCommandWithoutCircuitBreaker() : base(testPropsBuilder().setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withCircuitBreakerEnabled(false)))
//			{
//			}
//
//			protected internal override bool? run()
//			{
//				Console.WriteLine("successfully executed");
//				return true;
//			}
//
//		}
//
//		/// <summary>
//		/// Threadpool with 1 thread, queue of size 1
//		/// </summary>
//		private class SingleThreadedPool : HystrixThreadPool
//		{
//
//			internal readonly LinkedBlockingQueue<Runnable> queue;
//			internal readonly ThreadPoolExecutor pool;
//			internal readonly int rejectionQueueSizeThreshold;
//
//			public SingleThreadedPool(int queueSize) : this(queueSize, 100)
//			{
//			}
//
//			public SingleThreadedPool(int queueSize, int rejectionQueueSizeThreshold)
//			{
//				queue = new LinkedBlockingQueue<Runnable>(queueSize);
//				pool = new ThreadPoolExecutor(1, 1, 1, TimeUnit.MINUTES, queue);
//				this.rejectionQueueSizeThreshold = rejectionQueueSizeThreshold;
//			}
//
//			public override ThreadPoolExecutor Executor
//			{
//				get
//				{
//					return pool;
//				}
//			}
//
//			public override Scheduler Scheduler
//			{
//				get
//				{
//					return new HystrixContextScheduler(HystrixPlugins.Instance.ConcurrencyStrategy, this);
//				}
//			}
//
//			public override void markThreadExecution()
//			{
//				// not used for this test
//			}
//
//			public override void markThreadCompletion()
//			{
//				// not used for this test
//			}
//
//			public override bool QueueSpaceAvailable
//			{
//				get
//				{
//					return queue.size() < rejectionQueueSizeThreshold;
//				}
//			}
//
//		}
//
//		/// <summary>
//		/// This has a ThreadPool that has a single thread and queueSize of 1.
//		/// </summary>
//		private class TestCommandRejection : TestHystrixCommand<bool?>
//		{
//
//			internal const int FALLBACK_NOT_IMPLEMENTED = 1;
//			internal const int FALLBACK_SUCCESS = 2;
//			internal const int FALLBACK_FAILURE = 3;
//
//			internal readonly int fallbackBehavior;
//
//			internal readonly int sleepTime;
//
//			internal TestCommandRejection(TestCircuitBreaker circuitBreaker, HystrixThreadPool threadPool, int sleepTime, int timeout, int fallbackBehavior) : base(testPropsBuilder().setThreadPool(threadPool).setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationThreadTimeoutInMilliseconds(timeout)))
//			{
//				this.fallbackBehavior = fallbackBehavior;
//				this.sleepTime = sleepTime;
//			}
//
//			protected internal override bool? run()
//			{
//				Console.WriteLine(">>> TestCommandRejection running");
//				try
//				{
//					Thread.Sleep(sleepTime);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//				return true;
//			}
//
//			protected internal override bool? Fallback
//			{
//				get
//				{
//					if (fallbackBehavior == FALLBACK_SUCCESS)
//					{
//						return false;
//					}
//					else if (fallbackBehavior == FALLBACK_FAILURE)
//					{
//						throw new Exception("Assert.Failed on fallback");
//					}
//					else
//					{
//						// FALLBACK_NOT_IMPLEMENTED
//						return base.Fallback;
//					}
//				}
//			}
//		}
//
//		/// <summary>
//		/// HystrixCommand that receives a custom thread-pool, sleepTime, timeout
//		/// </summary>
//		private class CommandWithCustomThreadPool : TestHystrixCommand<bool?>
//		{
//
//			public bool didExecute = false;
//
//			internal readonly int sleepTime;
//
//			internal CommandWithCustomThreadPool(TestCircuitBreaker circuitBreaker, HystrixThreadPool threadPool, int sleepTime, HystrixCommandProperties.Setter properties) : base(testPropsBuilder().setThreadPool(threadPool).setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(properties))
//			{
//				this.sleepTime = sleepTime;
//			}
//
//			protected internal override bool? run()
//			{
//				Console.WriteLine("**** Executing CommandWithCustomThreadPool. Execution => " + sleepTime);
//				didExecute = true;
//				try
//				{
//					Thread.Sleep(sleepTime);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//				return true;
//			}
//		}
//
//		/// <summary>
//		/// The run() will Assert.Fail and getFallback() take a long time.
//		/// </summary>
//		private class TestSemaphoreCommandWithSlowFallback : TestHystrixCommand<bool?>
//		{
//
//			internal readonly long fallbackSleep;
//
//			internal TestSemaphoreCommandWithSlowFallback(TestCircuitBreaker circuitBreaker, int fallbackSemaphoreExecutionCount, long fallbackSleep) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withFallbackIsolationSemaphoreMaxConcurrentRequests(fallbackSemaphoreExecutionCount)))
//			{
//				this.fallbackSleep = fallbackSleep;
//			}
//
//			protected internal override bool? run()
//			{
//				throw new Exception("run Assert.Fails");
//			}
//
//			protected internal override bool? Fallback
//			{
//				get
//				{
//					try
//					{
//						Thread.Sleep(fallbackSleep);
//					}
//					catch (InterruptedException e)
//					{
//						Console.WriteLine(e.ToString());
//						Console.Write(e.StackTrace);
//					}
//					return true;
//				}
//			}
//		}
//
//		private class NoRequestCacheTimeoutWithoutFallback : TestHystrixCommand<bool?>
//		{
//			public NoRequestCacheTimeoutWithoutFallback(TestCircuitBreaker circuitBreaker) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationThreadTimeoutInMilliseconds(200)))
//			{
//
//				// we want it to timeout
//			}
//
//			protected internal override bool? run()
//			{
//				try
//				{
//					Thread.Sleep(500);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(">>>> Sleep Interrupted: " + e.Message);
//					//                    e.printStackTrace();
//				}
//				return true;
//			}
//
//			public override string CacheKey
//			{
//				get
//				{
//					return null;
//				}
//			}
//		}
//
//		/// <summary>
//		/// The run() will take time. No fallback implementation.
//		/// </summary>
//		private class TestSemaphoreCommand : TestHystrixCommand<bool?>
//		{
//
//			internal readonly long executionSleep;
//
//			internal TestSemaphoreCommand(TestCircuitBreaker circuitBreaker, int executionSemaphoreCount, long executionSleep) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationStrategy(ExecutionIsolationStrategy.SEMAPHORE).withExecutionIsolationSemaphoreMaxConcurrentRequests(executionSemaphoreCount)))
//			{
//				this.executionSleep = executionSleep;
//			}
//
//			internal TestSemaphoreCommand(TestCircuitBreaker circuitBreaker, TryableSemaphore semaphore, long executionSleep) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationStrategy(ExecutionIsolationStrategy.SEMAPHORE)).setExecutionSemaphore(semaphore))
//			{
//				this.executionSleep = executionSleep;
//			}
//
//			protected internal override bool? run()
//			{
//				try
//				{
//					Thread.Sleep(executionSleep);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//				return true;
//			}
//		}
//
//		/// <summary>
//		/// Semaphore based command that allows caller to use latches to know when it has started and signal when it
//		/// would like the command to finish
//		/// </summary>
//		private class LatchedSemaphoreCommand : TestHystrixCommand<bool?>
//		{
//
//			internal readonly CountDownLatch startLatch, waitLatch;
//
//			/// 
//			/// <param name="circuitBreaker"> </param>
//			/// <param name="semaphore"> </param>
//			/// <param name="startLatch">
//			///            this command calls <seealso cref="java.util.concurrent.CountDownLatch#countDown()"/> immediately
//			///            upon running </param>
//			/// <param name="waitLatch">
//			///            this command calls <seealso cref="java.util.concurrent.CountDownLatch#await()"/> once it starts
//			///            to run. The caller can use the latch to signal the command to finish </param>
//			internal LatchedSemaphoreCommand(TestCircuitBreaker circuitBreaker, TryableSemaphore semaphore, CountDownLatch startLatch, CountDownLatch waitLatch) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationStrategy(ExecutionIsolationStrategy.SEMAPHORE)).setExecutionSemaphore(semaphore))
//			{
//				this.startLatch = startLatch;
//				this.waitLatch = waitLatch;
//			}
//
//			protected internal override bool? run()
//			{
//				// signals caller that run has started
//				this.startLatch.countDown();
//
//				try
//				{
//					// waits for caller to countDown latch
//					this.waitLatch.@await();
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//					return false;
//				}
//				return true;
//			}
//		}
//
//		/// <summary>
//		/// The run() will take time. Contains fallback.
//		/// </summary>
//		private class TestSemaphoreCommandWithFallback : TestHystrixCommand<bool?>
//		{
//
//			internal readonly long executionSleep;
//			internal readonly bool? fallback;
//
//			internal TestSemaphoreCommandWithFallback(TestCircuitBreaker circuitBreaker, int executionSemaphoreCount, long executionSleep, bool? fallback) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationStrategy(ExecutionIsolationStrategy.SEMAPHORE).withExecutionIsolationSemaphoreMaxConcurrentRequests(executionSemaphoreCount)))
//			{
//				this.executionSleep = executionSleep;
//				this.fallback = fallback;
//			}
//
//			protected internal override bool? run()
//			{
//				try
//				{
//					Thread.Sleep(executionSleep);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//				return true;
//			}
//
//			protected internal override bool? Fallback
//			{
//				get
//				{
//					return fallback;
//				}
//			}
//
//		}
//
//		private class RequestCacheNullPointerExceptionCase : TestHystrixCommand<bool?>
//		{
//			public RequestCacheNullPointerExceptionCase(TestCircuitBreaker circuitBreaker) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationThreadTimeoutInMilliseconds(200)))
//			{
//				// we want it to timeout
//			}
//
//			protected internal override bool? run()
//			{
//				try
//				{
//					Thread.Sleep(500);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(e.ToString());
//					Console.Write(e.StackTrace);
//				}
//				return true;
//			}
//
//			protected internal override bool? Fallback
//			{
//				get
//				{
//					return false;
//				}
//			}
//
//			public override string CacheKey
//			{
//				get
//				{
//					return "A";
//				}
//			}
//		}
//
//		private class RequestCacheTimeoutWithoutFallback : TestHystrixCommand<bool?>
//		{
//			public RequestCacheTimeoutWithoutFallback(TestCircuitBreaker circuitBreaker) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationThreadTimeoutInMilliseconds(200)))
//			{
//				// we want it to timeout
//			}
//
//			protected internal override bool? run()
//			{
//				try
//				{
//					Thread.Sleep(500);
//				}
//				catch (InterruptedException e)
//				{
//					Console.WriteLine(">>>> Sleep Interrupted: " + e.Message);
//					//                    e.printStackTrace();
//				}
//				return true;
//			}
//
//			public override string CacheKey
//			{
//				get
//				{
//					return "A";
//				}
//			}
//		}
//
//		private class RequestCacheThreadRejectionWithoutFallback : TestHystrixCommand<bool?>
//		{
//
//			internal readonly CountDownLatch completionLatch;
//
//			public RequestCacheThreadRejectionWithoutFallback(TestCircuitBreaker circuitBreaker, CountDownLatch completionLatch) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics).setThreadPool(new HystrixThreadPoolAnonymousInnerClassHelper()))
//			{
//				this.completionLatch = completionLatch;
//			}
//
//			private class HystrixThreadPoolAnonymousInnerClassHelper : HystrixThreadPool
//			{
//				public HystrixThreadPoolAnonymousInnerClassHelper()
//				{
//				}
//
//
//				public override ThreadPoolExecutor Executor
//				{
//					get
//					{
//						return null;
//					}
//				}
//
//				public override void markThreadExecution()
//				{
//
//				}
//
//				public override void markThreadCompletion()
//				{
//
//				}
//
//				public override bool QueueSpaceAvailable
//				{
//					get
//					{
//						// always return false so we reject everything
//						return false;
//					}
//				}
//
//				public override Scheduler Scheduler
//				{
//					get
//					{
//						return new HystrixContextScheduler(HystrixPlugins.Instance.ConcurrencyStrategy, this);
//					}
//				}
//
//			}
//
//			protected internal override bool? run()
//			{
//				try
//				{
//					if (completionLatch.@await(1000, TimeUnit.MILLISECONDS))
//					{
//						throw new Exception("timed out waiting on completionLatch");
//					}
//				}
//				catch (InterruptedException e)
//				{
//					throw new Exception(e);
//				}
//				return true;
//			}
//
//			public override string CacheKey
//			{
//				get
//				{
//					return "A";
//				}
//			}
//		}
//
//		private class BadRequestCommand : TestHystrixCommand<bool?>
//		{
//
//			public BadRequestCommand(TestCircuitBreaker circuitBreaker, ExecutionIsolationStrategy isolationType) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setCommandPropertiesDefaults(HystrixCommandPropertiesTest.UnitTestPropertiesSetter.withExecutionIsolationStrategy(isolationType)))
//			{
//			}
//
//			protected internal override bool? run()
//			{
//				throw new HystrixBadRequestException("Message to developer that they passed in bad data or something like that.");
//			}
//
//			protected internal override bool? Fallback
//			{
//				get
//				{
//					return false;
//				}
//			}
//
//			protected internal override string CacheKey
//			{
//				get
//				{
//					return "one";
//				}
//			}
//
//		}
//
//		private class CommandWithErrorThrown : TestHystrixCommand<bool?>
//		{
//
//			public CommandWithErrorThrown(TestCircuitBreaker circuitBreaker) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics))
//			{
//			}
//
//
//
//			protected internal override bool? run()
//			{
//				throw new Exception("simulated java.lang.Error message");
//			}
//
//		}
//
//		private class CommandWithCheckedException : TestHystrixCommand<bool?>
//		{
//
//			public CommandWithCheckedException(TestCircuitBreaker circuitBreaker) : base(testPropsBuilder().setCircuitBreaker(circuitBreaker).setMetrics(circuitBreaker.Metrics))
//			{
//			}
//
//
//
//			protected internal override bool? run()
//			{
//				throw new IOException("simulated checked exception message");
//			}
//
//		}
//
//
//
//		internal enum CommandKeyForUnitTest
//		{
//			KEY_ONE,
//			KEY_TWO
//		}
//
//
//
//		internal enum CommandGroupForUnitTest
//		{
//			OWNER_ONE,
//			OWNER_TWO
//		}
//
//
//
//		internal enum ThreadPoolKeyForUnitTest
//		{
//			THREAD_POOL_ONE,
//			THREAD_POOL_TWO
//		}
//
//		private static HystrixPropertiesStrategy TEST_PROPERTIES_FACTORY = new TestPropertiesFactory();
//
//		private class TestPropertiesFactory : HystrixPropertiesStrategy
//		{
//
//			public override HystrixCommandProperties getCommandProperties(HystrixCommandKey commandKey, HystrixCommandProperties.Setter builder)
//			{
//				if (builder == null)
//				{
//					builder = HystrixCommandPropertiesTest.UnitTestPropertiesSetter;
//				}
//				return HystrixCommandPropertiesTest.asMock(builder);
//			}
//
//			public override HystrixThreadPoolProperties getThreadPoolProperties(HystrixThreadPoolKey threadPoolKey, HystrixThreadPoolProperties.Setter builder)
//			{
//				if (builder == null)
//				{
//					builder = HystrixThreadPoolProperties.Setter.UnitTestPropertiesBuilder;
//				}
//				return HystrixThreadPoolProperties.Setter.asMock(builder);
//			}
//
//			public override HystrixCollapserProperties getCollapserProperties(HystrixCollapserKey collapserKey, HystrixCollapserProperties.Setter builder)
//			{
//				throw new IllegalStateException("not expecting collapser properties");
//			}
//
//			public override string getCommandPropertiesCacheKey(HystrixCommandKey commandKey, HystrixCommandProperties.Setter builder)
//			{
//				return null;
//			}
//
//			public override string getThreadPoolPropertiesCacheKey(HystrixThreadPoolKey threadPoolKey, com.netflix.hystrix.HystrixThreadPoolProperties.Setter builder)
//			{
//				return null;
//			}
//
//			public override string getCollapserPropertiesCacheKey(HystrixCollapserKey collapserKey, com.netflix.hystrix.HystrixCollapserProperties.Setter builder)
//			{
//				return null;
//			}
//
//		}
//
//		private class TestExecutionHook : HystrixCommandExecutionHook
//		{
//
//			internal AtomicInteger startExecute = new AtomicInteger();
//
//			public override void onStart<T>(HystrixCommand<T> commandInstance)
//			{
//				base.onStart(commandInstance);
//				startExecute.incrementAndGet();
//			}
//
//			internal object endExecuteSuccessResponse = null;
//
//			public override T onComplete<T>(HystrixCommand<T> commandInstance, T response)
//			{
//				endExecuteSuccessResponse = response;
//				return base.onComplete(commandInstance, response);
//			}
//
//			internal Exception endExecuteFailureException = null;
//			internal HystrixException.FailureType endExecuteFailureType = null;
//
//			public override Exception onError<T>(HystrixCommand<T> commandInstance, HystrixException.FailureType Assert.FailureType, Exception e)
//			{
//				endExecuteFailureException = e;
//				endExecuteFailureType = Assert.FailureType;
//				return base.onError(commandInstance, Assert.FailureType, e);
//			}
//
//			internal AtomicInteger startRun = new AtomicInteger();
//
//			public override void onRunStart<T>(HystrixCommand<T> commandInstance)
//			{
//				base.onRunStart(commandInstance);
//				startRun.incrementAndGet();
//			}
//
//			internal object runSuccessResponse = null;
//
//			public override T onRunSuccess<T>(HystrixCommand<T> commandInstance, T response)
//			{
//				runSuccessResponse = response;
//				return base.onRunSuccess(commandInstance, response);
//			}
//
//			internal Exception runFailureException = null;
//
//			public override Exception onRunError<T>(HystrixCommand<T> commandInstance, Exception e)
//			{
//				runFailureException = e;
//				return base.onRunError(commandInstance, e);
//			}
//
//			internal AtomicInteger startFallback = new AtomicInteger();
//
//			public override void onFallbackStart<T>(HystrixCommand<T> commandInstance)
//			{
//				base.onFallbackStart(commandInstance);
//				startFallback.incrementAndGet();
//			}
//
//			internal object fallbackSuccessResponse = null;
//
//			public override T onFallbackSuccess<T>(HystrixCommand<T> commandInstance, T response)
//			{
//				fallbackSuccessResponse = response;
//				return base.onFallbackSuccess(commandInstance, response);
//			}
//
//			internal Exception fallbackFailureException = null;
//
//			public override Exception onFallbackError<T>(HystrixCommand<T> commandInstance, Exception e)
//			{
//				fallbackFailureException = e;
//				return base.onFallbackError(commandInstance, e);
//			}
//
//			internal AtomicInteger threadStart = new AtomicInteger();
//
//			public override void onThreadStart<T>(HystrixCommand<T> commandInstance)
//			{
//				base.onThreadStart(commandInstance);
//				threadStart.incrementAndGet();
//			}
//
//			internal AtomicInteger threadComplete = new AtomicInteger();
//
//			public override void onThreadComplete<T>(HystrixCommand<T> commandInstance)
//			{
//				base.onThreadComplete(commandInstance);
//				threadComplete.incrementAndGet();
//			}
//
//		}
//
//	}
//
//}
