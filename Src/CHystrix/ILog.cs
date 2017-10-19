using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix
{
    internal interface ILog
    {
        void Log(LogLevelEnum level, string message);
        void Log(LogLevelEnum level, string message, Exception ex);
        void Log(LogLevelEnum level, string message, Dictionary<string, string> tagInfo);
        void Log(LogLevelEnum level, string message, Exception ex, Dictionary<string, string> tagInfo);
    }
}
