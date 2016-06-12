using System;
using System.Collections.Generic;
using System.Linq;

namespace Keeper.Warm
{
    public class Machine
    {
        private List<MethodInfo> methods = new List<MethodInfo>();

        public int DefineMethod(Opcode[] code)
        {
            var methodInfo = new MethodInfo();

            int index = this.methods.Count;

            this.methods.Add(methodInfo);

            return index;
        }

        private class MethodInfo
        {
        }
    }
}