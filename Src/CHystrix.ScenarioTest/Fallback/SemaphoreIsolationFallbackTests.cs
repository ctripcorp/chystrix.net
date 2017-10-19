using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CHystrix.ScenarioTest.Fallback
{
    [TestClass]
    public class SemaphoreIsolationFallbackTests : TestClassBase
    {
        [TestMethod]
        public void RunCommandFailAndFallbackSuccess()
        {
            string fallback = string.Empty;
            string actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new Exception(); }, () => fallback);
            Assert.AreEqual(fallback, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ScenarioTestException))]
        public void RunCommandFailAndFallbackFail()
        {
            HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new Exception(); },
                () => { throw new ScenarioTestException(); });
        }

        [TestMethod]
        public void RunCommandSuccessAndFallbackFail()
        {
            string expected = string.Empty;
            string actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => expected,
                () => { throw new ScenarioTestException(); });
            Assert.AreEqual(expected, actual);
        }
 
        [TestMethod]
        public void RunCommandSuccessAndFallbackSuccess()
        {
            string expected = string.Empty;
            string fallback = null;
            string actual = HystrixCommandBase.RunCommand<string>(TestCommandKey, () => expected,
                () => fallback);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RunCommandFailAndFallbackSuccessFromConcreteClass()
        {
            string expected = string.Empty;
            string fallback = null;
            string actual = new SampleHasFallbackIsolationCommand(TestCommandKey, expectedResult: expected,
                execute: () => { throw new Exception(); }, fallback: () => fallback).Run();
            Assert.AreEqual(fallback, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ScenarioTestException))]
        public void RunCommandFailAndFallbackFailFromConcreteClass()
        {
            new SampleHasFallbackIsolationCommand(TestCommandKey, execute: () => { throw new Exception(); },
                fallback: () => { throw new ScenarioTestException(); }).Run();
        }

        [TestMethod]
        public void RunCommandSuccessAndFallbackFailFromConcreteClass()
        {
            string expected = string.Empty;
            string actual = new SampleHasFallbackIsolationCommand(TestCommandKey, expectedResult: expected,
                execute: () => expected, fallback: () => { throw new Exception(); }).Run();
            Assert.AreEqual(expected, actual);
        }
 
        [TestMethod]
        public void RunCommandSuccessAndFallbackSuccessFromConcreteClass()
        {
            string expected = string.Empty;
            string fallback = null;
            string actual = new SampleHasFallbackIsolationCommand(TestCommandKey, expectedResult: expected,
                execute: () => expected, fallback: () => fallback).Run();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RunCommandFailCausedByBadRequestAndFallbackSuccess()
        {
            HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new BadRequestException(); }, () => string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ScenarioTestException))]
        public void RunCommandFailCausedByBadRequestAndFallbackFail()
        {
            HystrixCommandBase.RunCommand<string>(TestCommandKey, () => { throw new BadRequestException(); },
                () => { throw new ScenarioTestException(); });
        }
    }
}
