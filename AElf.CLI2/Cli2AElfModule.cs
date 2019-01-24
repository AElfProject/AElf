using System.Collections.Generic;
using AElf.CLI2.Commands;
using AElf.CLI2.Commands.CrossChain;
using AElf.CLI2.Commands.Proposal;
using AElf.CLI2.JS;
using AElf.CLI2.JS.Crypto;
using AElf.CLI2.JS.IO;
using AElf.Modularity;
using ChakraCore.NET.Debug;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CLI2
{
    
    public class Cli2AElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddAssemblyOf<Cli2AElfModule>();

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