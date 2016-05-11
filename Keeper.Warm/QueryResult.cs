using Keeper.Warm;
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

        public bool CanContinue
        {
            get
            {
                return this.machine.IsBacktrackAvailable;
            }
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
                return BuildTermFromHeap(this.machine, variables.Length - (index + 1));
            }
        }

        public static ITerm BuildTermFromHeap(Machine machine, int index)
        {
            return BuildTermFromAddress(machine, new Address(AddressType.Heap, index));
        }

        public static ITerm BuildTermFromAddress(Machine machine, Address termAddress)
        {
            Cell value = new Cell(machine.DereferenceAndLoad(termAddress));

            switch (value.Tag)
            {
                case Tag.Ref:
                    return new Variable(value.Address.Pointer.ToString());
                case Tag.Str:
                    Cell functorCell = new Cell(machine.DereferenceAndLoad(value.Address));
                    FunctorDescriptor functor = machine.GetFunctor(functorCell.Address.Pointer);

                    ITerm[] terms = null;

                    if (functor.Arity > 0)
                    {
                        terms = new ITerm[functor.Arity];

                        for (int termIndex = 0; termIndex < functor.Arity; termIndex++)
                        {
                            terms[termIndex] = BuildTermFromHeap(machine, (value.Address + termIndex + 1).Pointer);
                        }
                    }

                    return new CompoundTerm(functor.Name, terms);
                case Tag.Con:
                    FunctorDescriptor constantFunctor = machine.GetFunctor(value.Address.Pointer);

                    if (constantFunctor.Name == "[]")
                    {
                        return EmptyList.Instance;
                    }
                    else
                    {
                        return new Atom(constantFunctor.Name);
                    }
                case Tag.Lis:
                    var headTerm = BuildTermFromHeap(machine, value.Address.Pointer);
                    var TailTerm = BuildTermFromHeap(machine, (value.Address + 1).Pointer);

                    return new ListPair(headTerm, (IListTail)TailTerm);
                default:
                    throw new Exception("Unexpected tag in term:" + value);
            }
        }
    }
}