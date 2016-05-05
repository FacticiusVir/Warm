using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keeper.Warm
{
    public class Trail
    {
        private Stack<List<TrailItem>> trails = new Stack<List<TrailItem>>();
        
        public void AddItem(Address address, int value)
        {
            if (this.IsBacktrackAvailable)
            {
                this.trails.Peek().Add(new TrailItem()
                {
                    Address = address,
                    Value = value
                });
            }
        }

        public bool IsBacktrackAvailable
        {
            get
            {
                return this.trails.Any();
            }
        }

        public void Push()
        {
            this.trails.Push(new List<TrailItem>());
        }

        public IEnumerable<TrailItem> PopBacktrackItems()
        {
            return this.trails.Pop();
        }
    }
}
