// -----------------------------------------------------------------------
// <copyright file="HystrixCommandBaseTest.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace CHystrix.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestClass]
    public class HystrixCommandBaseTest
    {
        [TestMethod]
        public void TestAsyncCommandSuccessful()
        {
            Console.WriteLine(13%2);
            var result = HystrixCommandBase.RunCommandAsync<bool>("testasync", () =>
             {
                 return true;
             }, () =>
             {
                 return false;
             });

           Assert.AreEqual(true, result.Result);
           

        }

        [TestMethod]
        public void TestAsyncCommandFailedWithoutFallback()
        {
            var result = HystrixCommandBase.RunCommandAsync<bool>("testasync", () =>
            {
                throw new Exception("failed");
            });
            try
            {
                result.Wait();
            }
            catch { }
            Assert.AreEqual(true, result.IsFaulted);


        }

        [TestMethod]
        public void TestAsyncCommandFailedHaveFallback()
        {
            var result = HystrixCommandBase.RunCommandAsync<bool>("testasync", () =>
            {
                throw new Exception("failed");
            },()=>true);
            try
            {
                result.Wait();
            }
            catch { }
            Assert.AreEqual(true, result.Result);


        }

        [TestMethod]
        public void TestAsyncCommandFailedHaveFallbackFailed()
        {
            var result = HystrixCommandBase.RunCommandAsync<bool>("testasync", () =>
            {
                throw new Exception("failed");
            }, () => { throw new Exception("fall back failed"); });
            try
            {
                result.Wait();
            }
            catch { }
            Assert.AreEqual(true, result.IsFaulted);


        }

        [TestMethod]
        public void TestRunError()
        {
            try
            {
                CHystrix.HystrixCommandBase.RunCommand<object>("abc", () =>
                {
                    throw new Exception();
                });
            }
            catch
            {

            }
        }

    }
}
