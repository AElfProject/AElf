using System;
using System.Collections.Generic;
using AElf.CLI2.Commands.CrossChain;
using AElf.CLI2.Commands.Proposal;
using Autofac;

namespace AElf.CLI2.Commands
{
    public class CmdModule : Module
    {
        private readonly BaseOption _option;

        public static readonly IDictionary<Type, Type> Commands;

        public CmdModule(BaseOption option)
        {
            _option = option;
        }

        static CmdModule()
        {
            Commands = new Dictionary<Type, Type>
            {
                [typeof(CreateOption)] = typeof(CreateCommand),
                [typeof(InteractiveOption)] = typeof(InteractiveCommand),
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
            };
        }

        protected override void Load(ContainerBuilder builder)
        {
//            _option.ParseEnvVars();
            var cmdType = Commands[_option.GetType()];
            builder.RegisterInstance(_option);
            builder.RegisterType(cmdType).As<Command>();
            base.Load(builder);
        }
    }
}