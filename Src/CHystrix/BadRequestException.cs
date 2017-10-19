using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    public class BadRequestException : Exception
    {
        public BadRequestException()
        {
        }

        public BadRequestException(string message)
            : base(message)
        {
        }
    }
}
