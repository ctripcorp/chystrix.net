using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    internal enum CommandExecutionEventEnum
    {
        Success, BadRequest, Failed, Rejected, Timeout, ShortCircuited, FallbackSuccess, FallbackFailed, FallbackRejected,
        ExceptionThrown,
        //ThreadPoolRejected, 
        //ThreadExecution, 
        //ThreadMaxActive, 
        //ResponseFromCache
    }
}
