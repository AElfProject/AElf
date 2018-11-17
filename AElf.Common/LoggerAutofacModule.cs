using System;
using System.IO;
using System.Linq;
using AElf.Common.Attributes;
using Autofac;
using Autofac.Core;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace AElf.Common
{
    public class LoggerAutofacModule : Autofac.Module
    {
        private readonly string _nodeName;

        public LoggerAutofacModule(string nodeName = null)
        {
            _nodeName = nodeName;
        }

        protected override void Load(ContainerBuilder builder)
        {
            try
            {
                var logConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NLog.config");
                LogManager.Configuration = new XmlLoggingConfiguration(logConfigFile);
                var target = (FileTarget) LogManager.Configuration.FindTargetByName("file");

                if (string.IsNullOrWhiteSpace(_nodeName))
                {
                    target.FileName = "logs/log.txt";
                }
                else
                {
                    target.FileName = "logs/" + _nodeName + ".log";
                    target.ArchiveFileName = "logs/archives/" + _nodeName + "--{#}.arc";
                }

                LogManager.ReconfigExistingLoggers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Init NLog failed. {ex}");
            }
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
                Console.WriteLine($"Init NLog failed. {ex}");
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