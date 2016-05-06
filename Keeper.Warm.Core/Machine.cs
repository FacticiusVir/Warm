using System;
using System.Collections.Generic;
using System.Linq;

namespace Keeper.Warm
{
    public class Machine
    {
        private int[] memory;
        private int topOfHeapPointer;

        public Machine(int memorySize)
        {
            this.memory = new int[memorySize];
        }

        private int MemAlloc(int size)
        {
            int result = this.topOfHeapPointer;

            int newTopOfHeap = this.topOfHeapPointer + size;

            if (newTopOfHeap > memory.Length)
            {
                throw new Exception("Out of memory");
            }
            else
            {
                this.topOfHeapPointer = newTopOfHeap;
                return result;
            }
        }

        public int LoadCodeBlock(Opcode[] code)
        {
            int codePointer = this.MemAlloc(code.Length);

            for (int offset = 0; offset < code.Length; offset++)
            {
                this.memory[codePointer + offset] = (int)code[offset];
            }

            return codePointer;
        }

        public Thread SpawnThread(int instructionPointer, int stackSize)
        {
            int stackPointer = this.MemAlloc(stackSize);

            return new Thread(this, instructionPointer, stackPointer);
        }

        public class Thread
        {
            private Machine parent;
            private int instructionPointer;
            private int stackPointer;
            private int stackOffset;

            internal Thread(Machine parent, int instructionPointer, int stackPointer)
            {
                this.parent = parent;
                this.instructionPointer = instructionPointer;
                this.stackPointer = stackPointer;
                this.stackOffset = 0;
            }

            public bool Step()
            {
                var opcode = (Opcode)this.parent.memory[this.instructionPointer];

                this.instructionPointer += GetInstructionSize(opcode);

                bool succeed = true;

                switch (opcode)
                {
                    case Opcode.LoadConstant0:
                    case Opcode.LoadConstant1:
                    case Opcode.LoadConstant2:
                    case Opcode.LoadConstant3:
                    case Opcode.LoadConstant4:
                    case Opcode.LoadConstant5:
                    case Opcode.LoadConstant6:
                    case Opcode.LoadConstant7:
                        this.PushValue((int)opcode & 7);
                        break;
                    case Opcode.Add:
                        this.Add();
                        break;
                    default:
                        throw new Exception("Unrecognised opcode: " + opcode);
                }

                return succeed;
            }

            private void Add()
            {
                int value = this.PopValue();

                int operand = this.PopValue();

                this.PushValue(value + operand);
            }

            private int PopValue()
            {
                this.stackOffset--;
                return this.parent.memory[this.stackPointer + this.stackOffset];
            }

            private void PushValue(int value)
            {
                this.parent.memory[this.stackPointer + this.stackOffset] = value;
                this.stackOffset++;
            }
            
            private static int GetInstructionSize(Opcode opcode)
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

            public IEnumerable<int> Stack
            {
                get
                {
                    return this.parent.memory.Skip(this.stackPointer).Take(this.stackOffset);
                }
            }
        }
    }
}