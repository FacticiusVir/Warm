using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keeper.Warm
{
    public struct TrailItem
    {
        public Address Address
        {
            get;
            set;
        }

        public int Value
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Address, new Cell(this.Value));
        }
    }
}
