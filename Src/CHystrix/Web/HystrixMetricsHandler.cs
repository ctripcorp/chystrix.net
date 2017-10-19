using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Runtime.Serialization;

using CHystrix.Utils;
using CHystrix.Utils.Web;
using CHystrix.Utils.Extensions;

namespace CHystrix.Web
{
    internal class HystrixMetricsHandler : IHttpHandler
    {
        public const string OperationName = "_metrics";

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                MetricsInfo metrics = new MetricsInfo()
                {
                    CommandInfoList = HystrixStreamHandler.GetHystrixCommandInfoList(),
                    ThreadPoolInfoList = HystrixStreamHandler.GetHystrixThreadPoolList()
                };
                metrics.CommandCount = metrics.CommandInfoList.Count;
                metrics.ThreadPoolCount = metrics.ThreadPoolInfoList.Count;
                context.Response.ContentType = HttpContentTypes.Json;
                context.Response.Write(metrics.ToJson());
            }
            catch (Exception ex)
            {
                context.Response.ContentType = HttpContentTypes.PlainText;
                context.Response.Write(ex.Message);
            }
        }
    }

    [DataContract]
    internal class MetricsInfo
    {
        [DataMember(Order=1)]
        public int CommandCount { get; set; }

        [DataMember(Order=2)]
        public int ThreadPoolCount { get; set; }

        [DataMember(Order=3)]
        public List<HystrixCommandInfo> CommandInfoList { get; set; }

        [DataMember(Order=4)]
        public List<HystrixThreadPoolInfo> ThreadPoolInfoList { get; set; }
    }
}
