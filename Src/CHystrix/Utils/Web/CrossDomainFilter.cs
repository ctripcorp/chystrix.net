using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;

namespace CHystrix.Utils.Web
{
    internal static class CrossDomainFilter
    {
        public static bool EnableCrossDomainSupport(this HttpContext current)
        {
            bool isPreflight = false;

            HttpRequest request = current.Request;
            HttpResponse response = current.Response;
            if (request.Headers[HttpHeaders.Origin] != null)
            {
                response.AddHeader(HttpHeaders.AllowOrigin, request.Headers[HttpHeaders.Origin]);

                //preflight request
                if (request.HttpMethod == HttpMethods.Options
                    && (request.Headers[HttpHeaders.RequestMethod] != null || request.Headers[HttpHeaders.RequestHeaders] != null))
                {
                    response.AddHeader(HttpHeaders.AllowHeaders, request.Headers[HttpHeaders.RequestHeaders]);
                    response.AddHeader(HttpHeaders.AllowMethods, request.Headers[HttpHeaders.RequestMethod]);

                    response.StatusCode = (int)HttpStatusCode.OK;
                    isPreflight = true;
                }
            }

            return isPreflight;
        }
    }
}
