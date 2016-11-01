namespace Keeper.Warm.Prolog
{
    public class Platform
    {
        private const int topOfHeapPointer = 0;

        public static readonly MethodToken HeapAllocate = new MethodToken(1, 1);

        public static readonly MethodToken NewCell = new MethodToken(2, 1);

        public static readonly MethodToken GetCellTag = new MethodToken(1, 1);

        public static readonly MethodToken GetCellValue = new MethodToken(1, 1);

        public static readonly MethodToken Dereference = new MethodToken(1, 1);

        private Machine machine;

        public Platform()
        {
            this.machine = new Machine();

            this.machine.DefineMethod(HeapAllocate, new Opcode[]
            {
                Opcode.LoadConstant,
                topOfHeapPointer,
                Opcode.LoadPointerHeap,
                Opcode.Duplicate,
                Opcode.StoreLocal0,
                Opcode.Load,
                Opcode.Duplicate,
                Opcode.LoadArgumentAddress0,
                Opcode.Load,
                Opcode.Add,
                Opcode.LoadLocal0,
                Opcode.Store,
                Opcode.LoadPointerHeap,
                Opcode.Proceed

            }, localCount: 1);

            this.machine.DefineMethod(NewCell, new Opcode[]
            {
                Opcode.LoadConstant2,
                Opcode.Call,
                0,
                Opcode.StoreLocal0,
                Opcode.LoadArgumentAddress0,
                Opcode.Load,
                Opcode.LoadLocal0,
                Opcode.Store,
                Opcode.LoadArgumentAddress1,
                Opcode.Load,
                Opcode.LoadLocal0,
                Opcode.Increment,
                Opcode.Store,
                Opcode.Proceed
            }, new [] { HeapAllocate }, 1);

            this.machine.DefineMethod(GetCellTag, new Opcode[]
            {
                Opcode.LoadArgumentAddress0,
                Opcode.Load,
                Opcode.Load,
                Opcode.Proceed
            });

            this.machine.DefineMethod(GetCellValue, new Opcode[]
            {
                Opcode.LoadArgumentAddress0,
                Opcode.Load,
                Opcode.Increment,
                Opcode.Load,
                Opcode.Proceed
            });

            this.machine.DefineMethod(Dereference, new Opcode[]
            {
                Opcode.LoadArgumentAddress0,
                Opcode.Load,
                Opcode.StoreLocal0,
                Opcode.LoadLocal0,
                Opcode.Call,
                0,

            }, new[] { GetCellTag, GetCellValue }, 1);
        }

        public void DefineMethod(MethodToken token, Opcode[] code, MethodToken[] methodTable = null, int localCount = 0)
        {
            this.machine.DefineMethod(token, code, methodTable, localCount);
        }

        public Machine.Thread SpawnThread(MethodToken entryPoint, params long[] initialStack)
        {
            var newThread = this.machine.SpawnThread(entryPoint, initialStack);

            newThread.SetHeap(topOfHeapPointer, 1);

            return newThread;
        }
    }
}
