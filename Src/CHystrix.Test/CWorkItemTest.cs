// -----------------------------------------------------------------------
// <copyright file="CWorkItemTest.cs" company="Microsoft">
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
    using CHystrix.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestClass]
    public class CWorkItemTest
    {
        [TestMethod]
        public void TestMarkTimeout()
        {
            var workItem = new CWorkItem<bool>();

            var m1 = workItem.MarkTimeout();
            var m2 = workItem.MarkTimeout();
            var m3 = workItem.MarkTimeout();
            var m4 = workItem.MarkTimeout();

            Assert.AreEqual(m1, true);

            Assert.AreNotEqual(m2, true);
            Assert.AreNotEqual(m3, true);
            Assert.AreNotEqual(m4, true);

            workItem = new CWorkItem<bool>();

            var taskCount = 10;
            var tasks = new Task<bool>[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                tasks[i]=Task.Factory.StartNew<bool>(()=>{

                    return workItem.MarkTimeout();
                });
            }

            Task.WaitAll(tasks);

            Assert.AreEqual(1, tasks.Count(x => x.Result));

        }
    }
}
