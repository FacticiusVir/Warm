using System;
using System.Collections.Generic;
using System.Linq;

namespace Keeper.Warm
{
    public class Machine
    {
        private Dictionary<MethodToken, MethodInfo> methods = new Dictionary<MethodToken, MethodInfo>();

        public void DefineMethod(MethodToken token, Opcode[] code, List<MethodToken> methodTable)
        {
            var methodInfo = new MethodInfo()
            {
                Code = code,
                MethodTable = methodTable
            };

            this.methods.Add(token, methodInfo);
        }

        public Thread SpawnThread(MethodToken entryPoint)
        {
            return new Thread(this, entryPoint);
        }

        private static int GetInstructionSize(Opcode opcode)
        {
            Opcode operandSizeFlag
                = opcode & Opcode.OperandMask;

            switch (operandSizeFlag)
            {
                case Opcode.NoOperand:
                    return 1;
                case Opcode.Int64Operand:
                case Opcode.MethodTokenOperand:
                    return 2;
                default:
                    throw new NotSupportedException();
            }
        }

        public class Thread
        {
            private Machine parent;
            private Stack<Word> stack = new Stack<Word>();
            private Stack<MethodStackFrame> methodStack = new Stack<MethodStackFrame>();

            public Thread(Machine parent, MethodToken entryPoint)
            {
                this.parent = parent;

                this.methodStack.Push(
                    new MethodStackFrame(this.stack, 0)
                    {
                        Token = entryPoint
                    });
            }

            public StepResult Step()
            {
                if (!this.methodStack.Any())
                {
                    return StepResult.Halt;
                }

                var opCodes = this.parent.methods[this.CurrentFrame.Token];

                if (this.CurrentFrame.InstructionPointer > opCodes.Code.Length)
                {
                    return StepResult.Halt;
                }

                Opcode currentOpcode = opCodes.Code[this.CurrentFrame.InstructionPointer];

                int opcodeStep = GetInstructionSize(currentOpcode);

                int nextInstructionPointer = this.CurrentFrame.InstructionPointer + opcodeStep;

                switch (currentOpcode)
                {
                    case Opcode.Halt:
                        return StepResult.Halt;
                    case Opcode.Duplicate:
                        this.Duplicate();
                        break;
                    case Opcode.Call:
                        int tokenIndex = (int)opCodes.Code[this.CurrentFrame.InstructionPointer + 1];
                        var callTarget = opCodes.MethodTable[tokenIndex];
                        this.Call(callTarget);
                        break;
                    case Opcode.Proceed:
                        this.Proceed();
                        break;
                    case Opcode.LoadConstant0:
                    case Opcode.LoadConstant1:
                    case Opcode.LoadConstant2:
                    case Opcode.LoadConstant3:
                    case Opcode.LoadConstant4:
                    case Opcode.LoadConstant5:
                    case Opcode.LoadConstant6:
                    case Opcode.LoadConstant7:
                        this.LoadConstant(new Word { Int64 = (int)currentOpcode & 0xFF });
                        break;
                    case Opcode.Add:
                        this.Add();
                        break;
                    default:
                        throw new Exception($"Unknown opcode: {currentOpcode}");
                }

                this.CurrentFrame.InstructionPointer = nextInstructionPointer;

                return StepResult.Continue;
            }

            private void Proceed()
            {
                throw new NotImplementedException();
            }

            private void Call(MethodToken callTarget)
            {
                if (!this.parent.methods.ContainsKey(callTarget))
                {
                    throw new Exception("Missing method.");
                }
                else
                {
                    this.methodStack.Push(
                        new MethodStackFrame(this.stack, 0)
                        {
                            Token = callTarget
                        });
                }
            }

            private void Duplicate()
            {
                Word value = this.CurrentFrame.Peek();

                this.CurrentFrame.Push(value);
            }

            private void Add()
            {
                Word a = this.CurrentFrame.Pop();
                Word b = this.CurrentFrame.Pop();
                this.CurrentFrame.Push(new Word { Int64 = a.Int64 + b.Int64 });
            }

            private void LoadConstant(Word constantValue)
            {
                this.CurrentFrame.Push(constantValue);
            }

            private MethodStackFrame CurrentFrame
            {
                get
                {
                    return this.methodStack.Any()
                        ? this.methodStack.Peek()
                        : null;
                }
            }

            public IEnumerable<long> Stack
            {
                get
                {
                    return this.stack.Select(x => x.Int64);
                }
            }
        }

        private class MethodInfo
        {
            public Opcode[] Code
            {
                get;
                internal set;
            }

            public List<MethodToken> MethodTable
            {
                get;
                internal set;
            }
        }

        private class MethodStackFrame
        {
            private Stack<Word> threadStack;
            private int stackBase;
            private int localCount;

            public MethodStackFrame(Stack<Word> threadStack, int localCount)
            {
                this.threadStack = threadStack;
                this.stackBase = this.threadStack.Count;
                this.localCount = localCount;
                for (int index = 0; index < localCount; index++)
                {
                    this.threadStack.Push(new Word());
                }
            }

            public MethodToken Token
            {
                get;
                set;
            }

            public int InstructionPointer
            {
                get;
                set;
            }

            public void Push(Word value)
            {
                this.threadStack.Push(value);
            }

            public Word Pop()
            {
                if (this.threadStack.Count <= (this.stackBase + this.localCount))
                {
                    throw new Exception("Invalid stack pop.");
                }
                else
                {
                    return this.threadStack.Pop();
                }
            }

            public Word Peek()
            {
                if (this.threadStack.Count <= (this.stackBase + this.localCount))
                {
                    throw new Exception("Invalid stack peek.");
                }
                else
                {
                    return this.threadStack.Peek();
                }
            }

            public void Clear()
            {
                while (this.threadStack.Count > this.stackBase - this.Token.Arity)
                {
                    this.threadStack.Pop();
                }
            }
        }
    }
}