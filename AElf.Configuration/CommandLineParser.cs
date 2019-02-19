using AElf.Common.Application;
using CommandLine;

namespace AElf.Configuration
{
    public class CommandLineParser
    {
        public void Parse(string[] args)
        {
            //Parser.Default.Settings.IgnoreUnknownArguments = true;
            var parser=new Parser(settings =>
            {
                settings.CaseSensitive = false;
                settings.IgnoreUnknownArguments = true;
            });
            parser
                .ParseArguments<CommandLineOptions>(args)
                .WithParsed(MapOptions)
                .WithNotParsed(o=>{});
        }

        private void MapOptions(CommandLineOptions opts)
        {
            ApplicationHelpers.ConfigPath = opts.ConfigPath;
            ApplicationHelpers.LogPath = opts.LogPath;
            //LogManager.GlobalThreshold = LogLevel.FromOrdinal(opts.LogLevel);
        }
    }
}