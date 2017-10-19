using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using CHystrix.Utils;

namespace CHystrix
{
    internal static class CustomBadRequestExceptionChecker
    {
        public static ConcurrentDictionary<string, Func<Exception, bool>> BadRequestExceptionCheckers { get; private set; }

        static CustomBadRequestExceptionChecker()
        {
            BadRequestExceptionCheckers = new ConcurrentDictionary<string, Func<Exception, bool>>(StringComparer.InvariantCultureIgnoreCase);
        }

        public static bool IsBadRequestException(Exception ex)
        {
            foreach (KeyValuePair<string, Func<Exception, bool>> item in BadRequestExceptionCheckers)
            {
                try
                {
                    if (item.Value(ex))
                        return true;
                }
                catch (Exception ex2)
                {
                    CommonUtils.Log.Log(LogLevelEnum.Warning, "Failed to check bad request exception by custom delegate: " + item.Key, ex2,
                        new Dictionary<string, string>().AddLogTagData("FXD303047"));
                }
            }

            return false;
        }
    }
}
