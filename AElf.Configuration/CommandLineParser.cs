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
            

            // Network
//            if (opts.Bootnodes != null && opts.Bootnodes.Any())
//                NetworkConfig.Instance.Bootnodes = opts.Bootnodes.ToList();
//
//            if (opts.PeersDbPath != null)
//                NetworkConfig.Instance.PeersDbPath = opts.PeersDbPath;
//
//            if (opts.Port.HasValue)
//                NetworkConfig.Instance.ListeningPort = opts.Port.Value;
//
//            if (!string.IsNullOrWhiteSpace(opts.NetAllowed))
//            {
//                NetworkConfig.Instance.NetAllowed = opts.NetAllowed;
//            }
//            if (opts.NetWhitelist != null && opts.NetWhitelist.Any())
//            {
//                NetworkConfig.Instance.NetWhitelist = opts.NetWhitelist.ToList();
//            }

            if (!string.IsNullOrWhiteSpace(opts.ConsensusType))
            {
                ConsensusConfig.Instance.ConsensusType = ConsensusTypeHelper.GetType(opts.ConsensusType);
            }

            // node config
            if (opts.IsMiner.HasValue)
            {
                NodeConfig.Instance.IsMiner = opts.IsMiner.Value;
            }

            if (!string.IsNullOrWhiteSpace(opts.ExecutorType))
            {
                NodeConfig.Instance.ExecutorType = opts.ExecutorType;
            }

            if (!string.IsNullOrWhiteSpace(opts.NodeName))
            {
                NodeConfig.Instance.NodeName = opts.NodeName;
            }

            if (!string.IsNullOrWhiteSpace(opts.NodeAccount))
            {
                NodeConfig.Instance.NodeAccount = opts.NodeAccount;
            }

            if (!string.IsNullOrWhiteSpace(opts.NodeAccountPassword))
            {
                NodeConfig.Instance.NodeAccountPassword = opts.NodeAccountPassword;
            }

            // TODO: 
            NodeConfig.Instance.ConsensusKind = ConsensusKind.AElfDPoS;
            
            //LogManager.GlobalThreshold = LogLevel.FromOrdinal(opts.LogLevel);
        }
    }
}