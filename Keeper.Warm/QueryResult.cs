using Keeper.MakeSomething;
using System;
using System.Collections.Generic;

namespace Keeper.Warm
{
    public class QueryResult
    {
        private Machine machine;
        private Variable[] variables;

        public QueryResult(bool success, Variable[] variables, Machine machine)
        {
            this.Success = success;
            this.variables = variables;
            this.machine = machine;
        }

        public bool Success
        {
            get;
            private set;
        }

        public bool Continue()
        {
            bool result = this.machine.Continue();

            this.Success = result;

            return result;
        }

        public IEnumerable<Variable> Variables
        {
            get
            {
                return this.variables;
            }
        }

        public ITerm GetVariable(Variable variable)
        {
            int index = Array.IndexOf(variables, variable);

            if (index < 0)
            {
                return null;
            }
            else
            {
                return this.BuildTermFromHeap(variables.Length - (index + 1));
            }
        }

        private ITerm BuildTermFromHeap(int index)
        {
            Address termAddress = new Address(AddressType.Heap, index);

            Cell value = new Cell(this.machine.DereferenceAndLoad(termAddress));

            switch (value.Tag)
            {
                case Tag.Ref:
                    return new Variable(value.Address.Pointer.ToString());
                case Tag.Str:
                    Cell functorCell = new Cell(this.machine.DereferenceAndLoad(value.Address));
                    FunctorDescriptor functor = this.machine.GetFunctor(functorCell.Address.Pointer);

                    ITerm[] terms = null;

                    if (functor.Arity > 0)
                    {
                        terms = new ITerm[functor.Arity];

                        for (int termIndex = 0; termIndex < functor.Arity; termIndex++)
                        {
                            terms[termIndex] = this.BuildTermFromHeap((value.Address + termIndex + 1).Pointer);
                        }
                    }

                    return new CompoundTerm(functor.Name, terms);
                case Tag.Con:
                    FunctorDescriptor constantFunctor = this.machine.GetFunctor(value.Address.Pointer);

                    if (constantFunctor.Name == "[]")
                    {
                        return EmptyList.Instance;
                    }
                    else
                    {
                        return new Atom(constantFunctor.Name);
                    }
                case Tag.Lis:
                    var headTerm = this.BuildTermFromHeap(value.Address.Pointer);
                    var TailTerm = this.BuildTermFromHeap((value.Address + 1).Pointer);

                    return new ListPair(headTerm, (IListTail)TailTerm);
                default:
                    throw new Exception("Unexpected tag in term:" + value);
            }
        }
    }
}