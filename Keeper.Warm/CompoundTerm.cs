using System;
using System.Collections.Generic;
using System.Linq;

namespace Keeper.MakeSomething
{
    public sealed class Rule
    {
        public Rule(string header, IEnumerable<ITerm> headTerms, params CompoundTerm[] goals)
            : this(new CompoundTerm(header, headTerms), goals)
        {
        }

        public Rule(CompoundTerm head, params CompoundTerm[] goals)
        {
            this.Head = head;
            this.Goals = goals.ToArray();
        }

        public CompoundTerm Head
        {
            get;
            private set;
        }

        public IEnumerable<CompoundTerm> Goals
        {
            get;
            set;
        }

    }

    public sealed class CompoundTerm
        : ITerm
    {
        public CompoundTerm(string header, params ITerm[] terms)
            : this(header, (IEnumerable<ITerm>)terms)
        {
        }

        public CompoundTerm(string header, IEnumerable<ITerm> terms)
        {
            this.Header = new Atom(header);
            this.Terms = terms == null
                ? Enumerable.Empty<ITerm>()
                : terms.ToArray();
        }

        public Atom Header
        {
            get;
            private set;
        }

        public IEnumerable<ITerm> Terms
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return string.Format("{0}({1})", this.Header.ToString(), string.Join(", ", this.Terms.Select(x => x.ToString())));
        }
    }
}
