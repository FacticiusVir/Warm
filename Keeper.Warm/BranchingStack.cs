using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keeper.Warm
{
    public class BranchingStack
        : IEnumerable<int>
    {
        private int[] stack;
        private int stackPointer = -1;
        private Trail trail;

        public BranchingStack(Trail trail, int size)
        {
            this.trail = trail;
            this.stack = new int[size];
        }

        public int Pointer
        {
            get
            {
                return this.stackPointer;
            }
            set
            {
                while (value > this.stackPointer)
                {
                    this.Push(0);
                }

                while (value < this.stackPointer)
                {
                    this.Pop();
                }
            }
        }

        public int this[int index]
        {
            get
            {
                return this.stack[index];
            }
            set
            {
                this.trail.AddItem(new Address(AddressType.Stack, index), this.stack[index]);

                this.stack[index] = value;
            }
        }

        public void BacktrackPointer(int pointer)
        {
            this.stackPointer = pointer;
        }

        public void Backtrack(TrailItem item)
        {
            this.stack[item.Address.Pointer] = item.Value;
        }

        public void Push(int value)
        {
            this.stackPointer++;

            System.Diagnostics.Debug.WriteLine("Push: {2} {0}/{1}", value, new Cell(value), this.stackPointer);

            this[this.stackPointer] = value;
        }

        public int Pop()
        {
            int result = this.stack[this.stackPointer];

            System.Diagnostics.Debug.WriteLine("Pop: {2} {0}/{1}", result, new Cell(result), this.stackPointer);

            this.stackPointer--;

            return result;
        }

        public int Peek()
        {
            return this.stack[this.stackPointer];
        }

        public void Allocate(int count)
        {
            for (int index = 0; index < count; index++)
            {
                this.Push(0);
            }
        }

        public void Deallocate(int count)
        {
            for (int index = 0; index < count; index++)
            {
                this.Pop();
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            return this.stack.Take(this.stackPointer + 1).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
