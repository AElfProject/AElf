using System;
using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Economic;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

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
            economicContractMethodCallList.Add(nameof(EconomicContractContainer.EconomicContractStub.InitialEconomicSystem),
                new InitialEconomicSystemInput
                {
                    NativeTokenDecimals = _tokenInitialOptions.Decimals,
                    IsNativeTokenBurnable = _tokenInitialOptions.IsBurnable, 
                    NativeTokenSymbol= _tokenInitialOptions.Symbol,
                    NativeTokenTotalSupply = _tokenInitialOptions.TotalSupply,
                    MiningRewardTotalAmount = Convert.ToInt64(_tokenInitialOptions.TotalSupply * _tokenInitialOptions.DividendPoolRatio)
                });

            //TODO: Maybe should be removed after testing.
            foreach (var tokenReceiver in _consensusOptions.InitialMiners)
            {
                economicContractMethodCallList.Add(
                    nameof(EconomicContractContainer.EconomicContractStub.IssueNativeToken), new IssueNativeTokenInput
                    {
                        Amount =
                            (long) (_tokenInitialOptions.TotalSupply * (1 - _tokenInitialOptions.DividendPoolRatio)) /
                            _consensusOptions.InitialMiners.Count,
                        To = Address.FromPublicKey(ByteArrayHelpers.FromHexString(tokenReceiver)),
                        Memo = "Set initial miner's balance."
                    });
            }
            return economicContractMethodCallList;
        }
    }
}