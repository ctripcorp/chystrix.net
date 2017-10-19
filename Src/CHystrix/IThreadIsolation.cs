using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHystrix
{
    public interface IThreadIsolation<T>
    {
        Task<T> RunAsync();
    }
}
