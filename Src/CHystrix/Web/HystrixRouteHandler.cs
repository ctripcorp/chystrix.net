using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;

namespace CHystrix.Web
{
    internal class HystrixRouteHandler : IRouteHandler, IHttpHandler
    {
        public const string ControllerVariable = "controller";
        public const string ActionVariable = "action";
        public const string Route = "{" + ControllerVariable + "}/{*" + ActionVariable + "}";
        public const string HystrixRoutePrefix = "__chystrix";

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            if (requestContext.RouteData == null || requestContext.RouteData.Values == null)
                return this;

            if (!requestContext.RouteData.Values.ContainsKey(ControllerVariable) || requestContext.RouteData.Values[ControllerVariable] == null)
                return this;

            string controller = requestContext.RouteData.Values[ControllerVariable].ToString().ToLower();
            if (controller != HystrixRoutePrefix)
                return this;

            if (!requestContext.RouteData.Values.ContainsKey(ActionVariable) || requestContext.RouteData.Values[ActionVariable] == null)
                return this;

            string action = requestContext.RouteData.Values[ActionVariable].ToString().ToLower();
            string[] operationRoute = action.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (operationRoute.Length == 0)
                return this;

            string operation = operationRoute[0].Trim().ToLower();
            switch (operation)
            {
                case HystrixStreamHandler.OperationName:
                    return new HystrixStreamHandler();
                case HystrixConfigHandler.OperationName:
                    return new HystrixConfigHandler();
                case HystrixMetricsHandler.OperationName:
                    return new HystrixMetricsHandler();
                case HystrixCommandHandler.OperationName:
                    return new HystrixCommandHandler();
            }

            return this;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
        }
    }
}
