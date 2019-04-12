using System;
using System.Collections.Generic;
using AElf.CLI.Commands.Contract;
using AElf.CLI.Commands.CrossChain;
using AElf.CLI.Commands.Proposal;
using Autofac;

namespace AElf.CLI.Commands
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
                [typeof(InteractiveConsoleOption)] = typeof(InteractiveConsoleCommand),
                [typeof(DeployContractOption)] = typeof(DeployContractCommand),
                [typeof(UpdateContractOption)] = typeof(UpdateContractCommand),
                [typeof(ChangeContractOwnerOption)] = typeof(ChangeContractOwnerCommand),
                [typeof(GetAbiOption)] = typeof(GetAbiCommand),
                [typeof(SendTransactionOption)] = typeof(SendTransactionCommand),
                [typeof(CallReadOnlyOption)] = typeof(CallReadOnlyCommand),
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
                [typeof(CheckChainStatusOption)] = typeof(CheckChainStatusCommand),
                [typeof(VerifyCrossChainTransactionOption)] = typeof(VerifyCrossChainTransactionCommand),
                [typeof(CertificateGenerationOption)] = typeof(CertificateGenerationCommand),
                [typeof(WithdrawChainCreationRequestOption)] = typeof(WithdrawChainCreationRequestCommand),
                [typeof(RechargeForSideChainOption)] =typeof(RechargeForSideChainCommand)
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