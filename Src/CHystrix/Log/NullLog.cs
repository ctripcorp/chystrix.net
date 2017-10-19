using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHystrix.Log
{
    internal class NullLog : ILog
    {
        protected readonly ICommandConfigSet ConfigSet;

        public NullLog(Type type)
            : this(null, type)
        {
        }

        public NullLog(ICommandConfigSet configSet, Type type)
        {
            try
            {
                ConfigSet = configSet;
            }
            catch
            {
            }
        }

        protected void DegradeLogLevelIfNeeded(ref LogLevelEnum level)
        {
            if (ConfigSet == null || !ConfigSet.DegradeLogLevel)
                return;

            switch (level)
            {
                case LogLevelEnum.Warning:
                    level = LogLevelEnum.Info;
                    break;
                case LogLevelEnum.Error:
                    level = LogLevelEnum.Warning;
                    break;
                case LogLevelEnum.Fatal:
                    level = LogLevelEnum.Error;
                    break;
            }
        }

        public void Log(LogLevelEnum level, string message)
        {
            try
            {
                DegradeLogLevelIfNeeded(ref level);

                switch (level)
                {
                    case LogLevelEnum.Info:
                        break;
                    case LogLevelEnum.Warning:
                        break;
                    case LogLevelEnum.Error:
                        break;
                    case LogLevelEnum.Fatal:
                        break;
                }
            }
            catch
            {
            }
        }

        public void Log(LogLevelEnum level, string message, Exception ex)
        {
            try
            {
                DegradeLogLevelIfNeeded(ref level);

                switch (level)
                {
                    case LogLevelEnum.Info:
                        break;
                    case LogLevelEnum.Warning:
                        break;
                    case LogLevelEnum.Error:
                        break;
                    case LogLevelEnum.Fatal:
                        break;
                }
            }
            catch
            {
            }
        }

        public void Log(LogLevelEnum level, string message, Dictionary<string, string> tagInfo)
        {
            try
            {
                DegradeLogLevelIfNeeded(ref level);

                switch (level)
                {
                    case LogLevelEnum.Info:
                        break;
                    case LogLevelEnum.Warning:
                        break;
                    case LogLevelEnum.Error:
                        break;
                    case LogLevelEnum.Fatal:
                        break;
                }
            }
            catch
            {
            }
        }

        public void Log(LogLevelEnum level, string message, Exception ex, Dictionary<string, string> tagInfo)
        {
            try
            {
                DegradeLogLevelIfNeeded(ref level);

                switch (level)
                {
                    case LogLevelEnum.Info:
                        break;
                    case LogLevelEnum.Warning:
                        break;
                    case LogLevelEnum.Error:
                        break;
                    case LogLevelEnum.Fatal:
                        break;
                }
            }
            catch
            {
            }
        }
    }
}
