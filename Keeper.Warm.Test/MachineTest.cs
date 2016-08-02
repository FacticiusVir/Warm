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

            target.DefineMethod(methodToken, new Opcode[] { }, null);
        }

        [TestMethod]
        public void CanSpawnThread()
        {
            var target = new Machine();

            var methodToken = new MethodToken(0);

            target.DefineMethod(methodToken, new Opcode[] { }, null);

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

        [TestMethod]
        public void CanLoadArguments()
        {
            var target = new Machine();

            var methodToken = new MethodToken(1);

            target.DefineMethod(methodToken, new Opcode[]
                {
                    Opcode.LoadArgumentAddress0,
                    Opcode.Load
                },
                null);

            var thread = target.SpawnThread(methodToken, 10);

            Assert.AreEqual(1, thread.Stack.Count());

            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(2, thread.Stack.Count());
            Assert.AreEqual(10, thread.Stack.First());
        }

        [TestMethod]
        public void CanReturn()
        {
            var target = new Machine();

            var methodToken = new MethodToken(0, 1);

            target.DefineMethod(methodToken, new Opcode[]
                {
                    Opcode.LoadConstant2,
                    Opcode.Proceed
                },
                null);

            var thread = target.SpawnThread(methodToken);

            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(1, thread.Stack.Count());
            Assert.AreEqual(2, thread.Stack.First());
        }

        [TestMethod]
        public void CanCallAndReturn()
        {
            var target = new Machine();

            var methodToken = new MethodToken(1, 1);

            target.DefineMethod(methodToken, new Opcode[]
                {
                    Opcode.LoadArgumentAddress0,
                    Opcode.Load,
                    Opcode.LoadConstant2,
                    Opcode.Add,
                    Opcode.Proceed
                });

            var callerToken = new MethodToken(0);

            target.DefineMethod(callerToken, new Opcode[]
            {
                Opcode.LoadConstant3,
                Opcode.Call,
                0,
                Opcode.LoadConstant5,
                Opcode.Add
            }, new[] { methodToken });

            var thread = target.SpawnThread(callerToken);
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(1, thread.Stack.Count());
            Assert.AreEqual(5, thread.Stack.First());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(1, thread.Stack.Count());
            Assert.AreEqual(10, thread.Stack.First());
        }

        [TestMethod]
        public void CanStoreAndLoadHeap()
        {
            var target = new Machine();

            var methodToken = new MethodToken(1, 1);

            target.DefineMethod(methodToken, new Opcode[]
                {
                    Opcode.LoadConstant5,
                    Opcode.LoadPointerHeap,
                    0,
                    Opcode.Store,
                    Opcode.LoadPointerHeap,
                    0,
                    Opcode.Load
                });

            var thread = target.SpawnThread(methodToken);
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(0, thread.Stack.Count());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(StepResult.Continue, thread.Step());
            Assert.AreEqual(1, thread.Stack.Count());
            Assert.AreEqual(5, thread.Stack.First());
            Assert.AreEqual(5, thread.Heap.ElementAt(0));
        }
    }
}