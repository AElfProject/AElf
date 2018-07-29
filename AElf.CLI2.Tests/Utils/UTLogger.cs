using AElf.Kernel.Modules.AutofacModule;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit.Abstractions;

namespace AElf.CLI2.Tests.Utils
{
    [Target("UT")]
    public class UTTarget : TargetWithLayout
    {
        public UTTarget(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; set; }

        protected override void Write(LogEventInfo logEvent)
        {
            var msg = Layout.Render(logEvent);
            OutputHelper.WriteLine(msg);
        }
    }


    public class UTLogModule : LoggerModule
    {
        private ITestOutputHelper _output;
        public UTLogModule(ITestOutputHelper outputHelper) : base("ut-aelf-cli2")
        {
            _output = outputHelper;
        }

        protected override void HookLogConfiguration(LoggingConfiguration configuration)
        {
            base.HookLogConfiguration(configuration);
            var utTarget = new UTTarget(_output);
            configuration.AddTarget("unittest", new UTTarget(_output));
            utTarget.Layout = "${date} | ${message}";
            var rule = new LoggingRule("*", LogLevel.Debug, utTarget);
            configuration.LoggingRules.Add(rule);
        }
    }
}