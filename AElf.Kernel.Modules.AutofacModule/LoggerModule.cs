using System;
using System.Linq;
using AElf.Common.Attributes;
using Autofac;
using Autofac.Core;
using NLog;
using NLog.Config;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class LoggerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var logConfigFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NLog.config");
            LogManager.Configuration = new XmlLoggingConfiguration(logConfigFile);
        }

        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry,
            IComponentRegistration registration)
        {
            // Handle constructor parameters.
            registration.Preparing += OnComponentPreparing;
        }

        private void OnComponentPreparing(object sender, PreparingEventArgs e)
        {
            Type t = e.Component.Activator.LimitType;

            LoggerNameAttribute loggerName = null;

            try
            {
                loggerName = (LoggerNameAttribute) Attribute.GetCustomAttribute(t, typeof(LoggerNameAttribute));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            e.Parameters = e.Parameters.Union(
                new[]
                {
                    new ResolvedParameter((p, i) => p.ParameterType == typeof(ILogger),
                        (p, i) => LogManager.GetLogger(loggerName?.Name ?? t.Name))
                });
        }
    }
}