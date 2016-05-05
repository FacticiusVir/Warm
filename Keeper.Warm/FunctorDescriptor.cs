using System.Linq;
using Keeper.MakeSomething;

namespace Keeper.Warm
{
    public struct FunctorDescriptor
    {
        public string Name;
        public int Arity;

        public FunctorDescriptor(EmptyList emptyList)
        {
            this.Name = "[]";
            this.Arity = 0;
        }

        public FunctorDescriptor(Atom term)
        {
            this.Name = term.Token;
            this.Arity = 0;
        }

        public FunctorDescriptor(CompoundTerm term)
        {
            this.Name = term.Header.Token;
            this.Arity = term.Terms.Count();
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", this.Name, this.Arity);
        }

        public override bool Equals(object obj)
        {
            return obj is FunctorDescriptor
                && ((FunctorDescriptor)obj) == this;
        }

        public static bool operator ==(FunctorDescriptor left, FunctorDescriptor right)
        {
            return left.Name == right.Name
                    && left.Arity == right.Arity;
        }

        public static bool operator !=(FunctorDescriptor left, FunctorDescriptor right)
        {
            return left.Name != right.Name
                    || left.Arity != right.Arity;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() ^ this.Arity;
        }
    }
}
