using Autofac;
using NLog;
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

            // Step 3. Set target properties 
            consoleTarget.Layout = @"${date:format=HH\:mm\:ss} : ${message}";

            // Step 4. Define rules
            var rule1 = new LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule1);
            
            // Step 5. Activate the configuration
            LogManager.Configuration = config;

            Logger logger = LogManager.GetLogger("General");
            
            builder.RegisterInstance(logger).As<ILogger>();
        }
    }
}