using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Web;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("CHystrix")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("CTrip")]
[assembly: AssemblyProduct("CHystrix")]
[assembly: AssemblyCopyright("Copyright © CTrip 2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ad7edf3c-b580-40e5-8351-d3e193e6a220")]

[assembly: PreApplicationStartMethod(typeof(CHystrix.Web.Initializer), "PreApplicationStartCode")]

[assembly: InternalsVisibleTo("CHystrix.Test")]
[assembly: InternalsVisibleTo("CHystrix.ScenarioTest")]
[assembly: InternalsVisibleTo("CHystrix.QA.Test")]
[assembly: InternalsVisibleTo("CHystrix.WebTests")]