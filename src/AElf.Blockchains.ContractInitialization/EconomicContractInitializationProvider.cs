using System;
using System.Linq;
using Acs0;
using AElf.Contracts.Economic;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.ContractInitialization
{
    public class EconomicContractInitializationProvider : ContractInitializationProviderBase
    {
        private readonly EconomicOptions _economicOptions;
        private readonly ConsensusOptions _consensusOptions;
        
        protected override Hash ContractName { get; } = EconomicSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Economic";

        public EconomicContractInitializationProvider(
            IOptionsSnapshot<EconomicOptions> economicOptions, IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _consensusOptions = consensusOptions.Value;
            _economicOptions = economicOptions.Value;
        }

        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            var economicContractMethodCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            economicContractMethodCallList.Add(
                nameof(EconomicContractContainer.EconomicContractStub.InitialEconomicSystem),
                new InitialEconomicSystemInput
                {
                    NativeTokenDecimals = _economicOptions.Decimals,
                    IsNativeTokenBurnable = _economicOptions.IsBurnable,
                    NativeTokenSymbol = _economicOptions.Symbol,
                    NativeTokenName = _economicOptions.TokenName,
                    NativeTokenTotalSupply = _economicOptions.TotalSupply,
                    MiningRewardTotalAmount =
                        Convert.ToInt64(_economicOptions.TotalSupply * _economicOptions.DividendPoolRatio),
                    TransactionSizeFeeUnitPrice = _economicOptions.TransactionSizeFeeUnitPrice
                });

            // Issue remain native tokens to first initial miner.
            economicContractMethodCallList.Add(
                nameof(EconomicContractContainer.EconomicContractStub.IssueNativeToken), new IssueNativeTokenInput
                {
                    Amount = Convert.ToInt64(_economicOptions.TotalSupply * (1 - _economicOptions.DividendPoolRatio)),
                    To = Address.FromPublicKey(
                        ByteArrayHelper.HexStringToByteArray(_consensusOptions.InitialMinerList.First())),
                    Memo = "Issue native token"
                });

            return economicContractMethodCallList;
        }
    }
}