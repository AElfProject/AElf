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
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForEconomic()
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
                        Convert.ToInt64(_economicOptions.TotalSupply * _economicOptions.DividendPoolRatio)
                });

            //TODO: Maybe should be removed after testing.
            foreach (var tokenReceiver in _consensusOptions.InitialMinerList)
            {
                economicContractMethodCallList.Add(
                    nameof(EconomicContractContainer.EconomicContractStub.IssueNativeToken), new IssueNativeTokenInput
                    {
                        Amount =
                            Convert.ToInt64(_economicOptions.TotalSupply *
                                            (1 - _economicOptions.DividendPoolRatio)) /
                            _consensusOptions.InitialMinerList.Count,
                        To = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(tokenReceiver)),
                        Memo = "Set initial miner's balance."
                    });
            }

            return economicContractMethodCallList;
        }
    }
}