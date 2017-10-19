using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix.Threading
{
    internal class CThreadCompleteException:Exception
    {
        private static readonly string msg = "some error happend when thread finish the work";
        public CThreadCompleteException(Exception innerException)
            : base(msg,innerException)
        {



        }
    }
}
