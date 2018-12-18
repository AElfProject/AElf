using System;
using System.IO;
using System.Linq;
using AElf.Common.Application;
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
                    target.FileName = Path.Combine("logs", "log.txt");
                }
                else
                {
                    target.FileName = Path.Combine(ApplicationHelpers.LogPath, _nodeName + ".log");
                    target.ArchiveFileName = Path.Combine(ApplicationHelpers.LogPath, "archives", _nodeName + ".log");
                }
                
                HookLogConfiguration(LogManager.Configuration);
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
        
        
        protected virtual void HookLogConfiguration(LoggingConfiguration configuration) {}

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