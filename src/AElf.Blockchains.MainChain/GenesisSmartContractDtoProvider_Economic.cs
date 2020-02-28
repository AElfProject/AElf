using System;
using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Economic;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForEconomic()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("Economic")).Value,
                EconomicSmartContractAddressNameProvider.Name, GenerateEconomicInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateEconomicInitializationCallList()
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