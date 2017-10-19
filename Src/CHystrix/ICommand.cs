using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    public interface ICommand
    {
        CommandStatusEnum Status { get; }
        string GroupKey { get; }
        string CommandKey { get; }
        string InstanceKey { get; }
        string Key { get; }
    }
}
