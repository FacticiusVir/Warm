using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keeper.Warm.Test
{
    [TestClass]
    public class MachineTest
    {
        [TestMethod]
        public void CanInitialise()
        {
            var target = new Machine();
        }
    }
}
