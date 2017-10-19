using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    public enum CommandStatusEnum
    {
        NotStarted, Started, Success, Failed, Timeout, Rejected, ShortCircuited, FallbackSuccess, FallbackFailed, FallbackRejected
    }
}
