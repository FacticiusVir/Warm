using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keeper.Warm.Test
{
    [TestClass]
    public class MachineTest
    {
        [TestMethod]
        public void CanInitialise()
        {
            var target = new Machine(1024);
        }

        [TestMethod]
        public void CanSpawnThread()
        {
            var target = new Machine(1024);

            var thread = target.SpawnThread(0, 128);
        }

        [TestMethod]
        public void CanLoadConstant()
        {
            var target = new Machine(1024);

            int codePointer = target.LoadCodeBlock(new Opcode[]
            {
                Opcode.LoadConstant2
            });

            var thread = target.SpawnThread(codePointer, 128);

            Assert.IsTrue(thread.Step());
            Assert.AreEqual(thread.Stack.Count(), 1);
            Assert.AreEqual(thread.Stack.First(), 2);
        }

        [TestMethod]
        public void CanAdd()
        {
            var target = new Machine(1024);

            int codePointer = target.LoadCodeBlock(new Opcode[]
            {
                Opcode.LoadConstant2,
                Opcode.LoadConstant3,
                Opcode.Add
            });

            var thread = target.SpawnThread(codePointer, 128);

            Assert.IsTrue(thread.Step());
            Assert.IsTrue(thread.Step());
            Assert.IsTrue(thread.Step());
            Assert.AreEqual(1, thread.Stack.Count());
            Assert.AreEqual(5, thread.Stack.First());
        }
    }
}