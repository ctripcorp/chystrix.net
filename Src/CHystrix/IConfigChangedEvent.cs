using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    internal interface IConfigChangeEvent
    {
        event HandleConfigChangeDelegate OnConfigChanged;

        void RaiseConfigChangeEvent();
    }

    internal delegate void HandleConfigChangeDelegate(ICommandConfigSet configSet);
}
