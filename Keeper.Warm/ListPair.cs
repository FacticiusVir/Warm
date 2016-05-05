using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keeper.MakeSomething
{
    public class ListPair
        : IListTail, ITerm
    {
        public static IListTail Create(params ITerm[] items)
        {
            return Create((IEnumerable<ITerm>)items);
        }

        public static IListTail Create(IEnumerable<ITerm> items)
        {
            if (!items.Any())
            {
                return EmptyList.Instance;
            }
            else
            {
                return new ListPair(items.First(), Create(items.Skip(1)));
            }
        }

        public ListPair(ITerm head, IListTail tail)
        {
            this.Head = head;
            this.Tail = tail;
        }

        public ITerm Head
        {
            get;
            private set;
        }

        public IListTail Tail
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return string.Format("[{0}]", this.GetInnerString());
        }

        private string GetInnerString()
        {
            return string.Format("{0}{1}", this.Head.ToString(), this.GetTailString());
        }

        private string GetTailString()
        {
            if (this.Tail is EmptyList)
            {
                return "";
            }
            else if (this.Tail is ListPair)
            {
                return ", " + ((ListPair)this.Tail).GetInnerString();
            }
            else
            {
                return this.Tail.ToString();
            }
        }
    }

    public class EmptyList
        : IListTail
    {
        static EmptyList()
        {
            EmptyList.Instance = new EmptyList();
        }

        private EmptyList()
        {
        }

        public static EmptyList Instance
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return "[]";
        }
    }
}
