using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Globalization;

namespace CHystrix.Utils
{
    internal static class CommonUtils
    {
        public const string HystrixNamePattern = @"^[a-zA-Z0-9][a-zA-Z0-9\-_.]*[a-zA-Z0-9]$";

        public const long UnixEpoch = 621355968000000000L;
        public static readonly DateTime UnixEpochDateTimeUtc = new DateTime(UnixEpoch, DateTimeKind.Utc);

        public static CommandExecutionEventEnum[] CommandExecutionEvents { get; private set; }
        public static CommandExecutionEventEnum[] CoreCommandExecutionEvents { get; private set; }
        public static CommandExecutionEventEnum[] ValuableCommandExecutionEvents { get; private set; }
        public static CommandExecutionEventEnum[] CoreFailedCommandExecutionEvents { get; private set; }

        public static ILog Log { get; private set; }

        public static string AppId { get; private set; }

        public static string HostIP { get; private set; }

        static CommonUtils()
        {
            CommandExecutionEvents = (CommandExecutionEventEnum[])Enum.GetValues(typeof(CommandExecutionEventEnum));
            CoreCommandExecutionEvents = new CommandExecutionEventEnum[]
            {
                CommandExecutionEventEnum.Success, CommandExecutionEventEnum.Failed, CommandExecutionEventEnum.Timeout, CommandExecutionEventEnum.ShortCircuited,
                CommandExecutionEventEnum.Rejected, CommandExecutionEventEnum.BadRequest
            };
            ValuableCommandExecutionEvents = new CommandExecutionEventEnum[]
            {
                CommandExecutionEventEnum.Success, CommandExecutionEventEnum.Failed, CommandExecutionEventEnum.Timeout, CommandExecutionEventEnum.ShortCircuited
            };
            CoreFailedCommandExecutionEvents = new CommandExecutionEventEnum[]
            {
                CommandExecutionEventEnum.Failed, CommandExecutionEventEnum.Timeout, CommandExecutionEventEnum.ShortCircuited,
                CommandExecutionEventEnum.Rejected, CommandExecutionEventEnum.BadRequest
            };

            AppId = ConfigurationManager.AppSettings["AppId"];
            if (string.IsNullOrWhiteSpace(AppId))
                AppId = null;
            else
                AppId = AppId.Trim();

            Log = ComponentFactory.CreateLog(typeof(CommonUtils));

            try
            {
                HostIP = Dns.GetHostAddresses(Dns.GetHostName())
                    .Where(c => c.AddressFamily == AddressFamily.InterNetwork)
                    .Select(c => c.ToString()).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Log(LogLevelEnum.Fatal, "Failed to get host IP.", ex);
            }
        }

        public static long CurrentTimeInMiliseconds
        {
            get
            {
                return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }
        }

        public static long CurrentUnixTimeInMilliseconds
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime dtUtc = DateTime.Now;
                if (dtUtc.Kind != DateTimeKind.Utc)
                {
                    dtUtc = now.Kind == DateTimeKind.Unspecified && now > DateTime.MinValue
                        ? DateTime.SpecifyKind(now.Subtract(TimeZoneInfo.Local.GetUtcOffset(now)), DateTimeKind.Utc)
                        : TimeZoneInfo.ConvertTimeToUtc(now);
                }

                return (long)(dtUtc.Subtract(UnixEpochDateTimeUtc)).TotalMilliseconds;
            }
        }

        public static string GenerateTypeKey(Type type)
        {
            return type.FullName + "__" + type.Assembly.GetName().Name;
        }

        public static string GenerateKey(String instanceKey, String commandKey)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
                return null;

            commandKey = commandKey.Trim();
            return string.IsNullOrWhiteSpace(instanceKey) ? commandKey : commandKey + "." + instanceKey.Trim();
        }

        public static void SubcribeConfigChangeEvent(this ICommandConfigSet configSet, HandleConfigChangeDelegate handleConfigChange)
        {
            IConfigChangeEvent hasConfigChangeEvent = configSet as IConfigChangeEvent;
            if (hasConfigChangeEvent == null)
                return;

            hasConfigChangeEvent.OnConfigChanged += handleConfigChange;
        }

        public static void RaiseConfigChangeEvent(this ICommandConfigSet configSet)
        {
            IConfigChangeEvent hasConfigChangeEvent = configSet as IConfigChangeEvent;
            if (hasConfigChangeEvent == null)
                return;

            hasConfigChangeEvent.RaiseConfigChangeEvent();
        }

        public static CommandExecutionHealthSnapshot GetHealthSnapshot(this Dictionary<CommandExecutionEventEnum, int> executionEventDistribution)
        {
            int totalCount = 0;
            int failedCount = 0;
            foreach (KeyValuePair<CommandExecutionEventEnum, int> pair in executionEventDistribution)
            {
                if (CommonUtils.ValuableCommandExecutionEvents.Contains(pair.Key))
                {
                    totalCount += pair.Value;
                    if (pair.Key != CommandExecutionEventEnum.Success)
                        failedCount += pair.Value;
                }
            }
            return new CommandExecutionHealthSnapshot(totalCount, failedCount);
        }

        public static long GetPercentile(this List<long> list, double percent)
        {
            return GetPercentile(list, percent, false);
        }

        public static long GetPercentile(this List<long> list, double percent, bool sorted)
        {
            if (list == null)
                return 0;

            if (list.Count <= 0)
                return 0;

            if (!sorted)
                list.Sort();

            if (percent <= 0.0)
                return list[0];

            if (percent >= 100.0)
                return list[list.Count - 1];

            int rank = (int)(percent * (list.Count - 1) / 100);
            return list[rank];
        }

        public static void GetAuditData(this List<long> list, out int count, out long sum, out long min, out long max)
        {
            if (list == null)
                list = new List<long>();

            int iCount = 0;
            long iSum = 0;
            long iMin = long.MaxValue;
            long iMax = long.MinValue;
            foreach (long item in list)
            {
                iCount++;
                iSum += item;
                if (item < iMin)
                    iMin = item;
                if (item > iMax)
                    iMax = item;
            }
            if (iCount == 0)
            {
                iMin = 0;
                iMax = 0;
            }

            count = iCount;
            sum = iSum;
            min = iMin;
            max = iMax;
        }

        public static bool IsValidHystrixName(this string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            return Regex.IsMatch(name, HystrixNamePattern);
        }

        public static Dictionary<string, string> AddLogTagData(this Dictionary<string, string> tagData, string errorCode)
        {
            if (tagData == null)
                tagData = new Dictionary<string, string>();

            tagData["HystrixAppName"] = HystrixCommandBase.HystrixAppName;
            tagData["ErrorCode"] = errorCode;

            return tagData;
        }

        public static bool IsBadRequestException(this Exception ex)
        {
            if (ex == null)
                return false;

            if (ex is BadRequestException)
                return true;

            if (CustomBadRequestExceptionChecker.IsBadRequestException(ex))
                return true;

            return false;
        }
    }
}
