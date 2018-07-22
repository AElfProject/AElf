using System;
using System.IO;
using System.Runtime.InteropServices;
using AElf.CLI2.JS;
using AElf.Kernel.Modules.AutofacModule;
using Autofac;
using ChakraCore.NET.Hosting;
using ChakraCore.NET.API;
namespace AElf.CLI2
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new LoggerModule("aelf-cli"));
            builder.RegisterModule(new JSModule());
            var container = builder.Build();
            var jsEngine = container.Resolve<IJSEngine>();
            try
            {
                jsEngine.RunScriptFile("./Scripts/entry.js");
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}