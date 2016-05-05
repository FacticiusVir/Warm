using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keeper.Warm
{
    public interface ICodeGenerator
    {
        void Emit(Opcode opcode);
        void Emit(Opcode opcode, int argument);
        void Emit(Opcode opcode, FunctorDescriptor functor);
        void Emit(Opcode opcode, Label label);
        void Emit(Opcode opcode, Local local);
        Label DefineLabel();
        void MarkLabel(Label label);
        Local DefineLocal();
    }

    public class Local
    {
        internal Local(int index)
        {
            this.Index = index;
        }

        internal int Index
        {
            get;
            private set;
        }
    }

    public class Label
    {
        internal Label()
        {
        }
    }

    public class CodeGenerator
        : ICodeGenerator
    {
        private class LabelInfo
        {
            public List<int> References = new List<int>();
            public int? Mark;
        }

        private List<int> code = new List<int>();
        private Stack<Dictionary<Label, LabelInfo>> contexts = new Stack<Dictionary<Label, LabelInfo>>();
        private Func<string, int, int> rulePointerLookup;
        private Func<FunctorDescriptor, int> functorLookup;
        private int nextFreeLocal;
        private int argumentCount;

        public CodeGenerator(int argumentCount, Func<string, int, int> rulePointerLookup, Func<FunctorDescriptor, int> functorLookup)
        {
            this.argumentCount = argumentCount;
            this.rulePointerLookup = rulePointerLookup;
            this.functorLookup = functorLookup;

            this.contexts.Push(new Dictionary<Label, LabelInfo>());
        }

        public void Emit(Opcode opcode)
        {
            this.code.Add((int)opcode);
        }

        public void Emit(Opcode opcode, int argument)
        {
            this.code.Add((int)opcode);
            this.code.Add(argument);
        }

        public void Emit(Opcode opcode, FunctorDescriptor functor)
        {
            this.code.Add((int)opcode);
            if (opcode.HasFlag(Opcode.RulePointerOperand))
            {
                this.code.Add(rulePointerLookup(functor.Name, functor.Arity));
            }
            else
            {
                this.code.Add(functorLookup(functor));
            }
        }

        public void Emit(Opcode opcode, Label label)
        {
            this.code.Add((int)opcode);

            var context = this.contexts.Peek();

            var info = context[label];

            info.References.Add(this.code.Count);

            this.code.Add(0);
        }

        public void Emit(Opcode opcode, Local local)
        {
            this.code.Add((int)opcode);
            this.code.Add(local.Index);
        }

        public Label DefineLabel()
        {
            var result = new Label();

            var context = this.contexts.Peek();

            context.Add(result, new LabelInfo());

            return result;
        }

        public void MarkLabel(Label label)
        {
            var context = this.contexts.Peek();

            var info = context[label];

            info.Mark = this.code.Count;
        }

        public IEnumerable<int> Generate()
        {
            yield return (int)Opcode.Allocate;
            yield return this.nextFreeLocal;

            this.ApplyLabelsForCurrentContext();

            foreach (var item in this.code)
            {
                yield return item;
            }

            yield return (int)Opcode.Deallocate;
            yield return this.argumentCount;
            yield return (int)Opcode.Proceed;
        }

        private void ApplyLabelsForCurrentContext()
        {
            foreach (var label in this.contexts.Peek().Values)
            {
                foreach (int referencePointer in label.References)
                {
                    this.code[referencePointer] = label.Mark.Value - (referencePointer - 1);
                }
            }
        }

        public Local DefineLocal()
        {
            return new Local(this.nextFreeLocal++);
        }
    }
}
