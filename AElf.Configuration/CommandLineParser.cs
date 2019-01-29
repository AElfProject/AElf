using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Common.Application;
using AElf.Common.Enums;
using AElf.Configuration.Config.Chain;
using AElf.Configuration.Config.Consensus;
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

            if (!string.IsNullOrWhiteSpace(opts.ConsensusType))
            {
                ConsensusConfig.Instance.ConsensusType = ConsensusTypeHelper.GetType(opts.ConsensusType);
            }

            //LogManager.GlobalThreshold = LogLevel.FromOrdinal(opts.LogLevel);
        }
    }
}