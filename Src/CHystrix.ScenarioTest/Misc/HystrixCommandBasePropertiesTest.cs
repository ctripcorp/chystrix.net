using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CHystrix.ScenarioTest.Misc
{
    [TestClass]
    public class HystrixCommandBasePropertiesTest
    {
        [TestMethod]
        public void TestDefaultAppName()
        {
            Assert.AreEqual(HystrixCommandBase.DefaultAppName, HystrixCommandBase.HystrixAppName);
        }

        [TestMethod]
        public void TestHystrixVersion()
        {
            Assert.AreEqual(typeof(HystrixCommandBase).Assembly.GetName().Version.ToString(), HystrixCommandBase.HystrixVersion);
        }

        [TestMethod]
        public void TestHystrixMaxCommandCount()
        {
            Assert.AreEqual(HystrixCommandBase.DefaultMaxCommandCount, HystrixCommandBase.MaxCommandCount);
        }
    }
}
