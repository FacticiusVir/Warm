using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Keeper.Warm.Prolog.Test
{
    [TestClass]
    public class PlatformTest
    {
        [TestMethod]
        public void CanInitialise()
        {
            var target = new Platform();
        }

        [TestMethod]
        public void CanHeapAllocate()
        {
            var target = new Platform();

            var thread = target.SpawnThread(Platform.HeapAllocate, 10);

            long heapPointer = thread.Heap.ElementAt(0);

            StepResult runningState = StepResult.Continue;

            while (runningState != StepResult.Halt)
            {
                Assert.AreEqual(StepResult.Continue, runningState);
                runningState = thread.Step();
            }

            Assert.AreEqual(((long)AddressType.Heap) << 56 | heapPointer, thread.Stack.ElementAt(0));
            Assert.AreEqual(heapPointer + 10, thread.Heap.ElementAt(0));
        }

        [TestMethod]
        public void CanSetTag()
        {
            var target = new Platform();

            var thread = target.SpawnThread(Platform.NewCell, 1234, (long)Tag.Reference);

            long heapPointer = thread.Heap.ElementAt(0);

            StepResult runningState = StepResult.Continue;

            while (runningState != StepResult.Halt)
            {
                Assert.AreEqual(StepResult.Continue, runningState);
                runningState = thread.Step();
            }

            Assert.AreEqual((long)Tag.Reference, thread.Heap.ElementAt((int)heapPointer));
            Assert.AreEqual(1234L, thread.Heap.ElementAt((int)heapPointer + 1));
        }

        [TestMethod]
        public void CanGetTag()
        {
            var target = new Platform();

            var testMethod = new MethodToken(0, 1);

            target.DefineMethod(testMethod, new Opcode[]
            {
                Opcode.LoadConstant,
                (Opcode)1234,
                Opcode.LoadConstant,
                (Opcode)Tag.Reference,
                Opcode.Call,
                (Opcode)0,
                Opcode.Call,
                (Opcode)1,
                Opcode.Proceed
            }, new[] { Platform.NewCell, Platform.GetCellTag });

            var thread = target.SpawnThread(testMethod);

            StepResult runningState = StepResult.Continue;

            while (runningState != StepResult.Halt)
            {
                Assert.AreEqual(StepResult.Continue, runningState);
                runningState = thread.Step();
            }

            Assert.AreEqual((long)Tag.Reference, thread.Stack.ElementAt(0));
        }

        [TestMethod]
        public void CanGetValue()
        {
            var target = new Platform();

            var testMethod = new MethodToken(0, 1);

            target.DefineMethod(testMethod, new Opcode[]
            {
                Opcode.LoadConstant,
                (Opcode)1234,
                Opcode.LoadConstant,
                (Opcode)Tag.Reference,
                Opcode.Call,
                (Opcode)0,
                Opcode.Call,
                (Opcode)1,
                Opcode.Proceed
            }, new[] { Platform.NewCell, Platform.GetCellValue });

            var thread = target.SpawnThread(testMethod);

            StepResult runningState = StepResult.Continue;

            while (runningState != StepResult.Halt)
            {
                Assert.AreEqual(StepResult.Continue, runningState);
                runningState = thread.Step();
            }

            Assert.AreEqual(1234L, thread.Stack.ElementAt(0));
        }
    }
}
