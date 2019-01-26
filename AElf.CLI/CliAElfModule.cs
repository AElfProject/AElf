using System.Collections.Generic;
using AElf.CLI.Commands;
using AElf.CLI.Commands.CrossChain;
using AElf.CLI.Commands.Proposal;
using AElf.CLI.JS;
using AElf.CLI.JS.Crypto;
using AElf.CLI.JS.IO;
using AElf.Modularity;
using ChakraCore.NET.Debug;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CLI
{
    
    public class CliAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddAssemblyOf<CliAElfModule>();

            services.AddTransient<IConsole,Console>();
            services.AddTransient<IJSEngine,JSEngine>();
            services.AddTransient<IRequestExecutor,RequestExecutor>();
            services.AddTransient<IRandomGenerator,PseudoRandomGenerator>();
            services.AddTransient<IDebugAdapter,JSDebugAdapter>();

            /*
            var cmds = new Dictionary<Type, Type>
            {
                [typeof(CreateOption)] = typeof(CreateCommand),
                [typeof(InteractiveConsoleOption)] = typeof(InteractiveConsoleCommand),
                [typeof(DeployOption)] = typeof(DeployCommand),
                [typeof(GetAbiOption)] = typeof(GetAbiCommand),
                [typeof(SendTransactionOption)] = typeof(SendTransactionCommand),
                [typeof(GetTxResultOption)] = typeof(GetTxResultCommand),
                [typeof(GetBlockHeightOption)] = typeof(GetBlockHeightCommand),
                [typeof(GetBlockInfoOption)] = typeof(GetBlockInfoCommand),
                [typeof(GetMerkelPathOption)] = typeof(GetMerkelPathCommand),
                [typeof(CreateMultiSigOption)] = typeof(CreateMultiSigAddressCommand),
                [typeof(ProposalOption)] = typeof(ProposeCommand),
                [typeof(CheckProposalOption)] = typeof(CheckProposalCommand),
                [typeof(ApprovalOption)] = typeof(ApproveCommand),
                [typeof(ReleaseProposalOption)] = typeof(ReleaseProposalCommand),
                [typeof(ChainCreationRequestOption)] = typeof(ChainCreationRequestCommand),
                [typeof(ChainDisposalRequestOption)] = typeof(ChainDisposalRequestCommand),
                [typeof(CheckChainStatusOption)] = typeof(CheckChainStatusCommand)
            };*/
        }
    }
}