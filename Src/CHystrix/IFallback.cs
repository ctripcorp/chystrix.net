using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    public interface IFallback<T>
    {
        T GetFallback();
    }
}
