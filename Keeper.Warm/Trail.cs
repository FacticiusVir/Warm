using System;
using System.Collections.Generic;
using System.Linq;

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

        public int Level
        {
            get
            {
                return this.trails.Count;
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

        public void Cut()
        {
            var trailItems = this.PopBacktrackItems();

            if (this.IsBacktrackAvailable)
            {
                foreach (var item in trailItems)
                {
                    this.AddItem(item.Address, item.Value);
                }
            }
        }

        public void Clear()
        {
            this.trails.Clear();
        }
    }
}
