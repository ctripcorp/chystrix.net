using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace CHystrix.Threading.Test
{
    [TestClass]
    public class ThreadPoolTest
    {
        [TestMethod]
        public void TestWorkQueue()
        {
            const int workRunTime = 3000;
            CThreadPool pool = new CThreadPool(5);

            for (int i = 0; i < 25; i++)
            {
                pool.QueueWorkItem(() =>
                {
                    Thread.Sleep(workRunTime);
                    return true;
                });
            }


            Thread.Sleep(workRunTime);

            Assert.AreEqual(pool.NowRunningWorkCount,5);
           
           
        }


        [TestMethod]
        public void TestWorkTimeout() {

            const int workRunTime = 1000;
            int workCount = 25;
            CThreadPool pool = new CThreadPool(5);

            var tasks = new List<CWorkItem<bool>>();
            for (int i = 0; i < workCount; i++)
            {
                tasks.Add(pool.QueueWorkItem(() =>
                {
                    Thread.Sleep(workRunTime);
                    return true;
                }));
            }


            Thread.Sleep(workRunTime+10);

            Assert.AreEqual(pool.NowRunningWorkCount+pool.FinishedWorkCount+pool.NowWaitingWorkCount+pool.TimeoutWorkCount, 
                workCount);

            pool.WorkItemTimeoutMiliseconds = 2000;

            Thread.Sleep(workRunTime);


            Assert.AreEqual(pool.NowWaitingWorkCount, 0);

            
        }

        [TestMethod]
        public void TestThreadWorkFail()
        {
            CThreadPool pool = new CThreadPool(5);


         
            var task =pool.QueueWorkItem<bool>(() =>
            {
                throw (new Exception("error"));
            });

            task.Wait();

            Assert.IsTrue(task.Status==CTaskStatus.Faulted&&task.IsFaulted);

      


        }

        
        [TestMethod]
        public void TestThreadPoolResize()
        {
            CThreadPool pool = new CThreadPool(1);
            const int workRunTime = 3000;

            for (int i = 0; i < 10; i++)
            {
                pool.QueueWorkItem(() =>
                {
                    Thread.Sleep(workRunTime);
                    return true;
                });
                
            }

            Assert.IsTrue(pool.NowRunningWorkCount==1);


            pool.MaxConcurrentCount = 5;


            Thread.Sleep(10);

            Assert.IsTrue(pool.NowRunningWorkCount == pool.MaxConcurrentCount);

            pool.MaxConcurrentCount = 1;

            Thread.Sleep(workRunTime);

            for (int i = 0; i < 10; i++)
            {
                pool.QueueWorkItem(() =>
                {
                    Thread.Sleep(workRunTime);
                    return true;
                });

            }



            Assert.AreEqual(pool.MaxConcurrentCount, pool.NowRunningWorkCount);

            pool.MaxConcurrentCount = 6;

            Thread.Sleep(10);


            Assert.IsTrue(pool.NowRunningWorkCount == pool.MaxConcurrentCount);



        }

        [TestMethod]
        public void TestThreadPoolPerformance()
        {

            CThreadPool pool = new CThreadPool(1);

            var task = pool.QueueWorkItem<bool>(() =>
            {
                return true;
            });

            task.Wait();
            //the delay of initing thread should less 10 millisec
            Assert.IsTrue(task.ExeuteMilliseconds - task.RealExecuteMilliseconds < 10);



            int failCount = 0,delayFailCount=0;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            var tasks = new List<CWorkItem<bool>>();
            
            for (int i = 0; i < 100000; i++)
            {
                var task1 = pool.QueueWorkItem<bool>(() =>
                {
                    return true;
                });

                tasks.Add(task1);

            }

            watch.Stop();

            Console.WriteLine("add 100000 work to queue total cost:" + watch.ElapsedMilliseconds);

            Assert.IsTrue(watch.ElapsedMilliseconds < 150);

            watch.Restart();
            foreach (var task1 in tasks)
            {
                if (!task1.Result) failCount++;

                if (task1.RealExecuteMilliseconds > 1)
                {
                    delayFailCount++;
                }
   
                
            }


            Console.WriteLine("exe 100000 empty work cost:" + watch.ElapsedMilliseconds);

            Assert.IsTrue(watch.ElapsedMilliseconds < 5000);

            Assert.IsTrue(failCount == 0);

            Assert.IsTrue(delayFailCount==0);

           

            pool.MaxConcurrentCount = 10;

            var tmpTasks = new List<CWorkItem<bool>>();

            for (int i = 0; i < 10; i++)
            {
                var t = pool.QueueWorkItem<bool>(() =>
                {
                    Thread.Sleep(1000);
                    return true;
                });

                tmpTasks.Add(t);

            }


            foreach (var item in tmpTasks)
            {
                item.Wait();

                Assert.IsTrue(item.ExeuteMilliseconds - item.RealExecuteMilliseconds < 20);
                
            }

      
                



        }

        [TestMethod]
        public void TestThreadIdleShutdownSelf()
        {
            int maxThreadCount = 5;
            CThreadPool pool = new CThreadPool(maxThreadCount);

            CThreadPool.SetCThreadIdleTimeout(100000);

            for (int i = 0; i < maxThreadCount; i++)
            {
                var task = pool.QueueWorkItem<bool>(() =>
                {
                    Thread.Sleep(1000);
                    return true;
                });
            }
            Assert.AreEqual(pool.NowRunningWorkCount, maxThreadCount);

            Thread.Sleep(2000);

            Assert.AreEqual(pool.NowRunningWorkCount, 0);
            Assert.AreEqual(maxThreadCount,pool.IdleThreadCount);


            pool = new CThreadPool(maxThreadCount);

            CThreadPool.SetCThreadIdleTimeout(1);

            for (int i = 0; i < maxThreadCount; i++)
            {
                var task = pool.QueueWorkItem<bool>(() =>
                {
                    Thread.Sleep(1000);
                    return true;
                });
            }

            Thread.Sleep(2500);

            Assert.AreEqual(0,pool.IdleThreadCount);
            Assert.AreEqual(0, pool.NowRunningWorkCount);

            pool.MaxConcurrentCount = 1;
            CThreadPool.SetCThreadIdleTimeout(2);


           var task1 = pool.QueueWorkItem(() =>
            {

                return true;
            });
            task1.Wait();

            Assert.AreEqual(1,pool.IdleThreadCount);
            Assert.AreEqual(0, pool.PoolThreadCount);
            Thread.Sleep(1890);

            Assert.AreEqual(1, pool.CurrentPoolSize);
            Assert.AreEqual(1, pool.IdleThreadCount);
           

            var task2 = pool.QueueWorkItem(() =>
            {

                return true;
            });

            Assert.IsTrue(task1.Result==true);

            Assert.AreEqual(1,pool.CurrentPoolSize);

            Assert.AreEqual(task1.WorkerThreadID,task2.WorkerThreadID);



        }

        [TestMethod]
        public void TestThreadPoolCompleteCallback()
        {
  
            CThreadPool pool = new CThreadPool(5);

            var test ="default";
            var taskMsg = "";

            pool.ThreadWorkCompleteCallback = (t) =>
            {

                test = "callback";

                if (t.IsFaulted)
                {
                    taskMsg = "some error";
                }

            };

            var task = pool.QueueWorkItem(() =>
            {

                return true;

            });
            task.Wait();
            Assert.AreEqual(test, "callback");


            task = pool.QueueWorkItem<bool>(() =>
            {
                throw (new Exception("error"));

                
            });
            task.Wait();

          

            pool.ThreadWorkCompleteCallback = (t) =>
            {
                if (t.IsFaulted)
                {
                    taskMsg = "some error";
                }

            };


            Assert.AreEqual(taskMsg, "some error");


        }

        [TestMethod]
        public void TestWorkItemStatusChange()
        {

            CThreadPool pool = new CThreadPool(5);


            var task = pool.QueueWorkItem(() =>
            {
                return true;
            });

            Assert.IsTrue(task.Result);

            var result = 101;
            var eventResult = 0;
            var task1 = pool.QueueWorkItem(() =>
            {

                return result;
            });


            task1.StatusChange += (o, e) =>
            {
                if (e.Status==CTaskStatus.RanToCompletion)
                {
                    eventResult = ((CWorkItem<int>)o).Result;
                }
            };

            task1.Wait();

            Assert.AreEqual(result,eventResult);

        }


    }
}
