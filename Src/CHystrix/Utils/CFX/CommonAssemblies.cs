using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Reflection;
using System.Web;

namespace CHystrix.Utils.CFX
{
    [SecurityCritical]
    internal static class CommonAssemblies
    {
        internal static readonly Assembly MicrosoftWebInfrastructure = typeof(CommonAssemblies).Assembly;
        internal static readonly Assembly mscorlib = typeof(object).Assembly;
        internal static readonly Assembly System = typeof(Uri).Assembly;
        internal static readonly Assembly SystemWeb = typeof(HttpContext).Assembly;
    }
}
