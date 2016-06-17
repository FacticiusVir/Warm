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
            var target = new Machine();
        }

        [TestMethod]
        public void CanDefineMethod()
        {
            var target = new Machine();

            var methodToken = new MethodToken(0);

            target.DefineMethod(methodToken, new Opcode[]
                { },
                null);
        }

        [TestMethod]
        public void CanSpawnThread()
        {
            var target = new Machine();

            var methodToken = new MethodToken(0);

            target.DefineMethod(methodToken, new Opcode[]
                { },
                null);

            var thread = target.SpawnThread(methodToken);
        }

        [TestMethod]
        public void CanLoadConstant()
        {
            var target = new Machine();

            var methodToken = new MethodToken(0);

            target.DefineMethod(methodToken, new Opcode[]
                {
                    Opcode.LoadConstant2
                },
                null);

            var thread = target.SpawnThread(methodToken);

            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(1, thread.Stack.Count());
            Assert.AreEqual(2, thread.Stack.First());
        }

        [TestMethod]
        public void CanAdd()
        {
            var target = new Machine();

            var methodToken = new MethodToken(0);

            target.DefineMethod(methodToken, new Opcode[]
                {
                    Opcode.LoadConstant2,
                    Opcode.LoadConstant3,
                    Opcode.Add
                },
                null);

            var thread = target.SpawnThread(methodToken);

            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(1, thread.Stack.Count());
            Assert.AreEqual(5, thread.Stack.First());
        }
    }
}