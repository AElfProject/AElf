using System;
using System.Linq;
using Autofac;
using Autofac.Core;
using NLog;
using NLog.Conditions;
using NLog.Config;
using NLog.Targets;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class LoggerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Step 1. Create configuration object 
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);

            consoleTarget.UseDefaultRowHighlightingRules = false;
            
            var highlightRule = new ConsoleRowHighlightingRule();
            
            highlightRule.Condition = ConditionParser.ParseExpression("level == LogLevel.Trace");
            highlightRule.BackgroundColor = ConsoleOutputColor.White;
            highlightRule.ForegroundColor = ConsoleOutputColor.Black;
            consoleTarget.RowHighlightingRules.Add(highlightRule);

            // Step 3. Set target properties 
            consoleTarget.Layout = @"[ ${date:format=HH\:mm\:ss} - ${logger} ] : ${message}";

            // Step 4. Define rules
            var rule1 = new LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule1);
            
            // Step 5. Activate the configuration
            LogManager.Configuration = config;
        }
        
        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
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
            }
            
            e.Parameters = e.Parameters.Union(
                new[]
                {
                    new ResolvedParameter((p, i) => p.ParameterType == typeof (ILogger), (p, i) => LogManager.GetLogger(loggerName?.Name ?? t.Name))
                });
        }
    }
}