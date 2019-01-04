using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using apache.log4net.Extensions.Logging;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;
using System.Linq;

namespace AElf.RuntimeSetup
{
    [DependsOn(typeof(CoreAElfModule))]
    public class RuntimeSetupAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
            var variables = Environment.GetEnvironmentVariables();

            var keys = variables.Keys.Cast<string>()
                .Select(p=>new {Ori=p,Uni=p.ToUpper()})
                .GroupBy(p=>p.Uni)
                .ToList();

            foreach (var key in keys)
            {
                if (key.Count() > 1)
                {
                    var list = key.ToList();
                    for (int i = 1; i < list.Count; i++)
                    {
                        Environment.SetEnvironmentVariable(list[i].Ori,null);
                    }
                }
            }
            
            var variables2 = Environment.GetEnvironmentVariables();
            
            //fix a bug in Log4net provider
            /*
             private void ReplaceEnvironmentVariables(XDocument xDocument)
               {
               Hashtable hashtable = new Hashtable(Environment.GetEnvironmentVariables(), (IEqualityComparer) StringComparer.OrdinalIgnoreCase);
               foreach (XAttribute xattribute in xDocument.Descendants().Select<XElement, XAttribute>((Func<XElement, XAttribute>) (x => x.Attribute((XName) "value"))).Where<XAttribute>((Func<XAttribute, bool>) (x => x != null)))
               xattribute.Value = OptionConverter.SubstituteVariables(xattribute.Value, (IDictionary) hashtable);
               }
             */

                
            context.Services.AddLogging(builder =>
            {
                builder.AddLog4Net(new Log4NetSettings()
                {
                    ConfigFile = Path.GetFullPath(Path.Combine(
                        Path.GetDirectoryName(new Uri(typeof(RuntimeSetupAElfModule).GetTypeInfo().Assembly.CodeBase)
                            .LocalPath) ?? string.Empty, "log4net.config"))
                    //ConfigFile = Path.Combine(Directory.GetCurrentDirectory(),"log4net.config")
                });
                builder.SetMinimumLevel(LogLevel.Debug);
            });
        }
    }
}