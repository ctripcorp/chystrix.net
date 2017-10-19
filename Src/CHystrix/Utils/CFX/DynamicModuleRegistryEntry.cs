using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix.Utils.CFX
{
    internal sealed class DynamicModuleRegistryEntry
    {
        public readonly string Name;
        public readonly string Type;

        public DynamicModuleRegistryEntry(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
}
