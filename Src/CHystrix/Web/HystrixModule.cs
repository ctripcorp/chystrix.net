using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading;

using CHystrix.Utils;
using CHystrix.Registration;

namespace CHystrix.Web
{
    internal class HystrixModule : IHttpModule
    {
        private static object Lock = new object();

        public static bool? InitSuccess { get; private set; }

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += Context_BeginRequestHandler;
        }

        private void Context_BeginRequestHandler(object sender, EventArgs e)
        {
            try
            {
                if (InitSuccess.HasValue)
                    return;

                HttpApplication app = sender as HttpApplication;
                if (app == null)
                    return;

                if (Monitor.TryEnter(Lock))
                {
                    try
                    {
                        if (InitSuccess.HasValue)
                            return;

                        Uri url = app.Context.Request.Url;
                        string appVirtualPath = app.Context.Request.ApplicationPath;
                        HystrixCommandBase.ApplicationPath = url.Scheme + "://" + url.Authority + appVirtualPath;

                        InitSuccess = true;
                    }
                    catch
                    {
                        InitSuccess = false;
                        throw;
                    }
                    finally
                    {
                        Monitor.Exit(Lock);
                    }
                }
            }
            catch (Exception ex)
            {
                CommonUtils.Log.Log(LogLevelEnum.Fatal, "Failed to init web host info.", ex,
                    new Dictionary<string, string>().AddLogTagData("FXD303026"));
            }
        }
    }
}
