﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Launcher
{
    class Program
    {
        private static void RegisterAssemblyResolveEvent()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;

            currentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        public static void Main(string[] args)
        {
            RegisterAssemblyResolveEvent();
            ILogger<Program> logger = NullLogger<Program>.Instance;
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                if (logger == NullLogger<Program>.Instance)
                    Console.WriteLine(e);
                logger.LogCritical(e, "program crashed");
            }
        }

        // create default https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder => { builder.ClearProviders(); })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseAutofac();
    }
}