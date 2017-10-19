using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;
using System.Runtime.Serialization;

using CHystrix.Utils.Web;
using CHystrix.Utils.Extensions;

namespace CHystrix.Web
{
    internal class HystrixCommandHandler : IHttpHandler
    {
        public const string OperationName = "_command";

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                List<CommandInfo> commandInfo = HystrixCommandBase.CommandComponentsCollection.Values.Select(v => v.CommandInfo).ToList();
                context.Response.ContentType = HttpContentTypes.Json;
                context.Response.Write(commandInfo.ToJson());
            }
            catch (Exception ex)
            {
                context.Response.ContentType = HttpContentTypes.PlainText;
                context.Response.Write(ex.Message);
            }
        }
    }
}
