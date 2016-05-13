using System;
using System.Collections.Generic;
using System.Linq;

namespace Keeper.Warm
{
    public class Machine
    {
        private List<FunctorDescriptor> functorList = new List<FunctorDescriptor>();
        private List<Func<Machine, bool>> callbacks = new List<Func<Machine, bool>>();

        private Address[] globalRegisters;

        private BranchingStack stack;
        private Trail trail;
        private Stack<Address> pdl;

        private int[] heap;
        private int[] code;

        public Address StackPointer
        {
            get
            {
                return new Address(AddressType.Stack, this.stack.Pointer);
            }
        }

        public Address TopOfHeap
        {
            get
            {
                return this.globalRegisters[(int)GlobalRegister.TopOfHeap];
            }
            set
            {
                this.globalRegisters[(int)GlobalRegister.TopOfHeap] = value;
            }
        }

        public Address InstructionPointer
        {
            get
            {
                return this.globalRegisters[(int)GlobalRegister.InstructionPointer];
            }
            set
            {
                this.globalRegisters[(int)GlobalRegister.InstructionPointer] = value;
            }
        }

        public Address Environment
        {
            get
            {
                return this.globalRegisters[(int)GlobalRegister.Environment];
            }
            set
            {
                System.Diagnostics.Debug.WriteLine("Set E: " + value);
                this.globalRegisters[(int)GlobalRegister.Environment] = value;
            }
        }

        public Address ContinuationPointer
        {
            get
            {
                return this.globalRegisters[(int)GlobalRegister.ContinuationPointer];
            }
            set
            {
                this.globalRegisters[(int)GlobalRegister.ContinuationPointer] = value;
            }
        }

        public int ChoicePointBase
        {
            get
            {
                return this.globalRegisters[(int)GlobalRegister.ChoicePointBase].Value;
            }
            set
            {
                this.globalRegisters[(int)GlobalRegister.ChoicePointBase] = new Address(AddressType.Blank, value);
            }
        }

        public Machine()
        {
            int baseSize = 1 << 10;

            this.globalRegisters = new Address[Enum.GetValues(typeof(GlobalRegister)).Length];
            this.TopOfHeap = new Address(AddressType.Heap, 0);
            this.InstructionPointer = new Address(AddressType.Code, 0);
            this.Environment = new Address(AddressType.Blank, 0);
            this.ContinuationPointer = new Address(AddressType.Code, -1);
            this.heap = new int[baseSize];
            this.trail = new Trail();
            this.stack = new BranchingStack(this.trail, baseSize);
            this.pdl = new Stack<Address>();
            this.code = new int[baseSize * 64];

            this.code[0] = (int)Opcode.Halt;
        }

        public bool Start(int instructionPointer, int variableArgumentCount)
        {
            System.Diagnostics.Debug.WriteLine("Start @ {0} w/{1}", instructionPointer, variableArgumentCount);

            this.TopOfHeap = new Address(AddressType.Heap, variableArgumentCount);
            this.InstructionPointer = new Address(AddressType.Code, instructionPointer);
            this.ContinuationPointer = new Address(AddressType.Code, -1);
            this.ChoicePointBase = 0;
            this.Environment = new Address(AddressType.Blank, 0);
            this.trail.Clear();

            this.stack.Pointer = -1;

            for (int index = 0; index < variableArgumentCount; index++)
            {
                int cellValue = new Cell(Tag.Ref, new Address(AddressType.Heap, index)).Value;
                this.heap[index] = cellValue;
                this.stack.Push(cellValue);
            }

            return this.Run();
        }

        public bool Continue()
        {
            System.Diagnostics.Debug.WriteLine("Continue");

            if (!this.Backtrack())
            {
                return false;
            }

            return this.Run();
        }

        private bool Run()
        {
            StepResult result;

            do result = this.Step();
            while (result == StepResult.Continue);

            return result == StepResult.Success;
        }

        public IEnumerable<Opcode> CodeView(int top)
        {
            return this.code.Take(top).Select(x => (Opcode)x);
        }

        public IEnumerable<Cell> HeapView
        {
            get
            {
                return this.heap.Select(x => new Cell(x));
            }
        }

        public IEnumerable<Cell> StackView
        {
            get
            {
                return this.stack.Select(x => new Cell(x));
            }
        }

        public int AddFunctor(FunctorDescriptor descriptor)
        {
            int result = this.functorList.Count;

            this.functorList.Add(descriptor);

            return result;
        }

        public FunctorDescriptor GetFunctor(int index)
        {
            return this.functorList[index];
        }

        private enum StepResult
        {
            Continue,
            Success,
            Fail
        }

        private StepResult Step()
        {
            int instructionPointer = this.InstructionPointer.Pointer;
            Opcode opcode = (Opcode)this.code[instructionPointer];

            this.InstructionPointer += this.GetInstructionSize(opcode);

            System.Diagnostics.Debug.WriteLine("{0}: {1} {2}", instructionPointer, opcode,
                                                string.Join(", ", Enumerable.Range(0, this.GetInstructionSize(opcode) - 1)
                                                            .Select(x => this.code[instructionPointer + x + 1])
                                                            .Select(x => string.Format("{0}/{1}", x, new Cell(x)))));

            switch (opcode)
            {
                case Opcode.Fail:
                    if (this.IsBacktrackAvailable)
                    {
                        System.Diagnostics.Debug.WriteLine("Backtrack on Fail");
                        this.Backtrack();
                        break;
                    }
                    else
                    {
                        return StepResult.Fail;
                    }
                case Opcode.Halt:
                    return StepResult.Success;
                case Opcode.Allocate:
                    this.Allocate(this.code[instructionPointer + 1]);
                    break;
                case Opcode.Deallocate:
                    this.Deallocate(this.code[instructionPointer + 1]);
                    break;
                case Opcode.StoreLocal:
                    this.StoreToLocal(this.code[instructionPointer + 1]);
                    break;
                case Opcode.LoadLocal:
                    this.LoadFromLocal(this.code[instructionPointer + 1]);
                    break;
                case Opcode.LoadLocalAddress:
                    this.LoadLocalAddress(this.code[instructionPointer + 1]);
                    break;
                case Opcode.LoadGlobalRegisterH:
                    this.LoadGlobalRegisterValue(GlobalRegister.TopOfHeap);
                    break;
                case Opcode.StoreGlobalRegisterH:
                    this.StoreGlobalRegisterValue(GlobalRegister.TopOfHeap);
                    break;
                case Opcode.LoadGlobalRegisterB0:
                    this.LoadGlobalRegisterValue(GlobalRegister.ChoicePointBase);
                    break;
                case Opcode.StoreGlobalRegisterB0:
                    this.StoreGlobalRegisterValue(GlobalRegister.ChoicePointBase);
                    break;
                case Opcode.Duplicate:
                    this.Duplicate();
                    break;
                case Opcode.ApplyTagStr:
                    this.ApplyTag(Tag.Str);
                    break;
                case Opcode.ApplyTagFun:
                    this.ApplyTag(Tag.Fun);
                    break;
                case Opcode.ApplyTagRef:
                    this.ApplyTag(Tag.Ref);
                    break;
                case Opcode.ApplyTagCon:
                    this.ApplyTag(Tag.Con);
                    break;
                case Opcode.ApplyTagLis:
                    this.ApplyTag(Tag.Lis);
                    break;
                case Opcode.Load:
                    this.Load();
                    break;
                case Opcode.Store:
                    this.Store();
                    break;
                case Opcode.LoadArgumentAddress0:
                case Opcode.LoadArgumentAddress1:
                case Opcode.LoadArgumentAddress2:
                case Opcode.LoadArgumentAddress3:
                case Opcode.LoadArgumentAddress4:
                case Opcode.LoadArgumentAddress5:
                case Opcode.LoadArgumentAddress6:
                case Opcode.LoadArgumentAddress7:
                    this.LoadArgumentAddress((int)opcode & 7);
                    break;
                case Opcode.LoadConstant0:
                case Opcode.LoadConstant1:
                case Opcode.LoadConstant2:
                case Opcode.LoadConstant3:
                case Opcode.LoadConstant4:
                case Opcode.LoadConstant5:
                case Opcode.LoadConstant6:
                case Opcode.LoadConstant7:
                    this.LoadConstant((int)opcode & 7);
                    break;
                case Opcode.LoadConstant:
                    this.LoadConstant(this.code[instructionPointer + 1]);
                    break;
                case Opcode.Call:
                    this.Call(this.code[instructionPointer + 1]);
                    break;
                case Opcode.Proceed:
                    return this.Proceed()
                        ? StepResult.Continue
                        : StepResult.Success;
                case Opcode.Increment:
                    this.Increment();
                    break;
                case Opcode.Deref:
                    this.Dereference();
                    break;
                case Opcode.GetTag:
                    this.GetTag();
                    break;
                case Opcode.GetAddress:
                    this.GetAddress();
                    break;
                case Opcode.BranchNotEqual:
                    this.Branch((x, y) => x != y, instructionPointer + this.code[instructionPointer + 1]);
                    break;
                case Opcode.BranchEqual:
                    this.Branch((x, y) => x == y, instructionPointer + this.code[instructionPointer + 1]);
                    break;
                case Opcode.BranchAlways:
                    this.Branch(null, instructionPointer + this.code[instructionPointer + 1]);
                    break;
                case Opcode.BranchAbsolute:
                    this.Branch(null, this.code[instructionPointer + 1]);
                    break;
                case Opcode.Bind:
                    this.Bind();
                    break;
                case Opcode.Add:
                    this.Add();
                    break;
                case Opcode.Unify:
                    if (!this.Unify())
                    {
                        System.Diagnostics.Debug.WriteLine("Unify Fail");
                        goto case Opcode.Fail;
                    }
                    break;
                case Opcode.Nop:
                    break;
                case Opcode.Pop:
                    this.stack.Pop();
                    break;
                case Opcode.ChoicePoint:
                    this.ChoicePoint(this.code[instructionPointer + 1]);
                    break;
                case Opcode.GetLevel:
                    this.GetLevel();
                    break;
                case Opcode.Cut:
                    this.Cut();
                    break;
                case Opcode.Trace:
                    this.Trace(this.code[instructionPointer + 1]);
                    break;
                case Opcode.EndTrace:
                    this.Trace(this.code[instructionPointer + 1], true);
                    break;
                case Opcode.Callback:
                    if (!this.Callback(this.code[instructionPointer + 1]))
                    {
                        goto case Opcode.Fail;
                    }
                    break;
                default:
                    throw new NotImplementedException("Unknown Opcode: " + opcode);
            }

            System.Diagnostics.Debug.WriteLine("");

            return StepResult.Continue;
        }

        private bool Callback(int callbackIndex)
        {
            var callback = this.callbacks[callbackIndex];

            return callback(this);
        }

        private void Trace(int functorIndex, bool end = false)
        {
            var functor = this.GetFunctor(functorIndex);

            var argumentStrings = new List<string>();

            for (int argumentIndex = 0; argumentIndex < functor.Arity; argumentIndex++)
            {
                var address = this.Environment - argumentIndex;

                argumentStrings.Add(new Cell(this.DereferenceAndLoad(address)).ToString());
            }

            string traceString = string.Format("{2}: {0} ({1})", functor, string.Join(", ", argumentStrings), end ? "Exit" : "Enter");

            System.Diagnostics.Debug.WriteLine(traceString);

            //Console.WriteLine("--" + new string('\t', this.trail.Level) + traceString);
        }

        private void Cut()
        {
            //Console.WriteLine("--Cut");

            int level = this.stack.Pop();

            while (this.trail.Level > level)
            {
                this.trail.Cut();
            }
        }

        private void GetLevel()
        {
            this.stack.Push(this.trail.Level);
        }

        private void ChoicePoint(int nextChoicePointer)
        {
            this.trail.Push();

            System.Diagnostics.Debug.WriteLine("Level: " + this.trail.Level);

            this.trail.AddItem(new Address(AddressType.GlobalRegister, (int)GlobalRegister.InstructionPointer), nextChoicePointer);
            this.trail.AddItem(new Address(AddressType.GlobalRegister, (int)GlobalRegister.ContinuationPointer), this.ContinuationPointer.Value);
            this.trail.AddItem(new Address(AddressType.GlobalRegister, (int)GlobalRegister.Environment), this.Environment.Value);
            this.trail.AddItem(new Address(AddressType.GlobalRegister, (int)GlobalRegister.TopOfHeap), this.TopOfHeap.Value);
            this.trail.AddItem(new Address(AddressType.GlobalRegister, (int)GlobalRegister.StackPointer), this.stack.Pointer);
            this.trail.AddItem(new Address(AddressType.GlobalRegister, (int)GlobalRegister.ChoicePointBase), this.ChoicePointBase);
        }

        public bool IsBacktrackAvailable
        {
            get
            {
                return this.trail.IsBacktrackAvailable;
            }
        }

        public bool Backtrack()
        {
            System.Diagnostics.Debug.WriteLine("Backtrack");
            //Console.WriteLine("--Backtrack");

            if (!this.IsBacktrackAvailable)
            {
                return false;
            }

            var items = this.trail.PopBacktrackItems().ToArray();

            foreach (var item in items.Reverse())
            {
                switch (item.Address.Type)
                {
                    case AddressType.GlobalRegister:
                        if (item.Address.Pointer == (int)GlobalRegister.StackPointer)
                        {
                            this.stack.BacktrackPointer(item.Value);
                        }
                        else
                        {
                            this.globalRegisters[item.Address.Pointer] = new Address(item.Value);
                        }
                        break;
                    case AddressType.Heap:
                        this.heap[item.Address.Pointer] = item.Value;
                        break;
                    case AddressType.Stack:
                        this.stack.Backtrack(item);
                        break;
                    default:
                        break;
                        throw new ArgumentException("Invalid address type: " + item.Address.Type);
                }
            }

            return true;
        }

        private bool Unify()
        {
            pdl.Clear();

            pdl.Push(new Cell(this.stack.Pop()).Address);
            pdl.Push(new Cell(this.stack.Pop()).Address);

            bool fail = false;

            while (pdl.Any() && !fail)
            {
                Address d1 = this.Dereference(pdl.Pop());
                Address d2 = this.Dereference(pdl.Pop());

                if (d1 != d2)
                {
                    Cell cell1 = new Cell(this.LoadFromStore(d1));
                    Cell cell2 = new Cell(this.LoadFromStore(d2));

                    if (cell1.Tag == Tag.Ref)
                    {
                        this.Bind(d1, d2);
                    }
                    else
                    {
                        switch (cell2.Tag)
                        {
                            case Tag.Ref:
                                this.Bind(d1, d2);
                                break;
                            case Tag.Con:
                                fail = cell1.Tag != Tag.Con || cell1.Address != cell2.Address;
                                break;
                            case Tag.Lis:
                                if (cell1.Tag != Tag.Lis)
                                {
                                    fail = true;
                                }
                                else
                                {
                                    pdl.Push(cell1.Address);
                                    pdl.Push(cell2.Address);
                                    pdl.Push(cell1.Address + 1);
                                    pdl.Push(cell2.Address + 1);
                                }
                                break;
                            case Tag.Str:
                                if (cell1.Tag != Tag.Str)
                                {
                                    fail = true;
                                }
                                else
                                {
                                    FunctorDescriptor functor1 = this.functorList[new Cell(this.LoadFromStore(cell1.Address)).Address.Pointer];
                                    FunctorDescriptor functor2 = this.functorList[new Cell(this.LoadFromStore(cell2.Address)).Address.Pointer];

                                    if (functor1 != functor2)
                                    {
                                        fail = true;
                                    }
                                    else
                                    {
                                        for (int index = 0; index < functor1.Arity; index++)
                                        {
                                            pdl.Push(cell1.Address + index + 1);
                                            pdl.Push(cell2.Address + index + 1);
                                        }
                                    }
                                }
                                break;
                            default:
                                fail = true;
                                break;
                        }
                    }
                }
            }

            return !fail;
        }

        private void Add()
        {
            int value = this.stack.Pop();

            int operand = this.stack.Pop();

            value += operand;

            this.stack.Push(value);
        }

        private void Bind()
        {
            this.Bind(new Cell(this.stack.Pop()).Address, new Cell(this.stack.Pop()).Address);
        }

        private void GetAddress()
        {
            Cell value = new Cell(this.stack.Pop());

            this.stack.Push(value.Address.Value);
        }

        private void Branch(Func<int, int, bool> comparer, int jumpPointer)
        {
            bool shouldBranch = true;

            if (comparer != null)
            {
                int a = this.stack.Pop();
                int b = this.stack.Pop();

                shouldBranch = comparer(a, b);
            }

            if (shouldBranch)
            {
                this.InstructionPointer = new Address(AddressType.Code, jumpPointer);
            }
        }

        private void GetTag()
        {
            Cell value = new Cell(this.stack.Pop());

            this.stack.Push((int)value.Tag);
        }

        private void Increment()
        {
            int value = this.stack.Pop();

            value++;

            this.stack.Push(value);
        }

        private bool Proceed()
        {
            this.InstructionPointer = this.ContinuationPointer;

            return this.InstructionPointer.Pointer >= 0;
        }

        public void Call(int jumpPointer)
        {
            this.ContinuationPointer = this.InstructionPointer;
            this.InstructionPointer = new Address(AddressType.Code, jumpPointer);
        }

        public void Push(Cell value)
        {
            this.stack.Push(value.Value);
        }

        public void Pop()
        {
            this.stack.Pop();
        }

        private void LoadConstant(int constantValue)
        {
            this.stack.Push(constantValue);
        }

        private void LoadArgumentAddress(int argumentIndex)
        {
            this.stack.Push((this.Environment - argumentIndex).Value);
        }

        private void Allocate(int variableCount)
        {
            int e = this.Environment.Value;
            this.Environment = new Address(AddressType.Stack, this.stack.Pointer);
            this.stack.Push(e);
            this.stack.Push(this.ContinuationPointer.Value);

            this.stack.Allocate(variableCount);
        }

        private void Deallocate(int argumentCount)
        {
            Address e = this.Environment;
            this.ContinuationPointer = new Address(this.LoadFromStore(e + 2));
            this.Environment = new Address(this.LoadFromStore(e + 1));

            this.stack.Pointer = e.Pointer;

            this.stack.Deallocate(argumentCount);
        }

        public void SetCode(Opcode[] code, int offset = 0)
        {
            Array.Copy(code, 0, this.code, offset, Math.Min(code.Length, this.code.Length));
        }

        public int AddCallback(Func<Machine, bool> callback)
        {
            this.callbacks.Add(callback);

            return this.callbacks.Count - 1;
        }

        private void Duplicate()
        {
            this.stack.Push(this.stack.Peek());
        }

        private void LoadGlobalRegisterValue(GlobalRegister register)
        {
            Address address =
                register == GlobalRegister.StackPointer
                    ? new Address(AddressType.Stack, this.stack.Pointer)
                    : this.globalRegisters[(int)register];
            this.stack.Push(address.Value);
        }

        private void StoreGlobalRegisterValue(GlobalRegister register)
        {
            Address address = new Address(this.stack.Pop());

            if (register == GlobalRegister.StackPointer)
            {
                this.stack.Pointer = address.Pointer;
            }
            else
            {
                this.globalRegisters[(int)register] = address;
            }
        }

        private void ApplyTag(Tag tag)
        {
            Address address = new Address(this.stack.Pop());
            this.stack.Push(new Cell(tag, address).Value);
        }

        private void LoadFromLocal(int localIndex)
        {
            this.stack.Push(this.LoadFromStore(this.GetLocalAddress(localIndex)));
        }

        private void LoadLocalAddress(int localIndex)
        {
            this.stack.Push(this.GetLocalAddress(localIndex).Value);
        }

        private Address GetLocalAddress(int localIndex)
        {
            return this.Environment + 3 + localIndex;
        }

        private void Load()
        {
            this.stack.Push(this.LoadFromStore(new Address(this.stack.Pop())));
        }

        public int LoadFromStore(Address address)
        {
            int value;

            switch (address.Type)
            {
                case AddressType.Stack:
                    value = this.stack[address.Pointer];
                    break;
                case AddressType.Heap:
                    value = this.heap[address.Pointer];
                    break;
                case AddressType.Code:
                default:
                    throw new ArgumentException("Invalid address type: " + address.Type);
            }

            System.Diagnostics.Debug.WriteLine("Load: {0} {1}/{2}", address, value, new Cell(value));

            return value;
        }

        private void StoreToLocal(int localIndex)
        {
            int value = this.stack.Pop();

            this.StoreToAddress(this.Environment + 3 + localIndex, value);
        }

        private void Store()
        {
            int value = this.stack.Pop();
            Address address = new Address(this.stack.Pop());

            StoreToAddress(address, value);
        }

        private void StoreToAddress(Address address, int value)
        {
            switch (address.Type)
            {
                case AddressType.Stack:
                    this.stack[address.Pointer] = value;
                    break;
                case AddressType.Heap:
                    this.trail.AddItem(address, this.heap[address.Pointer]);
                    this.heap[address.Pointer] = value;
                    break;
                case AddressType.Code:
                default:
                    throw new ArgumentException("Invalid address type: " + address.Type);
            }

            System.Diagnostics.Debug.WriteLine("Store: {0} {1}/{2}", address, value, new Cell(value));
        }

        private void Dereference()
        {
            Address baseAddress = new Cell(this.stack.Pop()).Address;

            Address derefAddress = this.Dereference(baseAddress);

            this.stack.Push(derefAddress.Value);
        }

        public Address Dereference(Address address)
        {
            var cell = new Cell(this.LoadFromStore(address));

            if (cell.Tag == Tag.Ref && cell.Address != address)
            {
                return this.Dereference(cell.Address);
            }
            else
            {
                return address;
            }
        }

        public int DereferenceAndLoad(Address address)
        {
            address = this.Dereference(address);

            return this.LoadFromStore(address);
        }

        private void Bind(Address addressOne, Address addressTwo)
        {
            Cell cellOne = new Cell(this.LoadFromStore(addressOne));
            Cell cellTwo = new Cell(this.LoadFromStore(addressTwo));

            if (cellOne.Tag == Tag.Ref && (cellTwo.Tag != Tag.Ref || addressTwo < addressOne))
            {
                this.StoreToAddress(addressOne, this.LoadFromStore(addressTwo));
            }
            else
            {
                this.StoreToAddress(addressTwo, this.LoadFromStore(addressOne));
            }
        }

        private int GetInstructionSize(Opcode opcode)
        {
            Opcode operandSizeFlag
                = opcode & Opcode.OperandMask;

            switch (operandSizeFlag)
            {
                case Opcode.NoOperand:
                    return 1;
                case Opcode.Int32Operand:
                case Opcode.RulePointerOperand:
                    return 2;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
