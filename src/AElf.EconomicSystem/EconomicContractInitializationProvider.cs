using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Economic;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.OS;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EconomicSystem
{
    public class EconomicContractInitializationProvider : IContractInitializationProvider, ITransientDependency
    {
        private readonly EconomicOptions _economicOptions;
        private readonly ConsensusOptions _consensusOptions;
        
        public Hash SystemSmartContractName { get; } = EconomicSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.Economic";


        public EconomicContractInitializationProvider(
            IOptionsSnapshot<EconomicOptions> economicOptions, IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _consensusOptions = consensusOptions.Value;
            _economicOptions = economicOptions.Value;
        }
        
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>
            {
                new ContractInitializationMethodCall{
                    MethodName = nameof(EconomicContractContainer.EconomicContractStub.InitialEconomicSystem),
                    Params = new InitialEconomicSystemInput
                    {
                        NativeTokenDecimals = _economicOptions.Decimals,
                        IsNativeTokenBurnable = _economicOptions.IsBurnable,
                        NativeTokenSymbol = _economicOptions.Symbol,
                        NativeTokenName = _economicOptions.TokenName,
                        NativeTokenTotalSupply = _economicOptions.TotalSupply,
                        MiningRewardTotalAmount =
                            Convert.ToInt64(_economicOptions.TotalSupply * _economicOptions.DividendPoolRatio),
                        TransactionSizeFeeUnitPrice = _economicOptions.TransactionSizeFeeUnitPrice
                    }.ToByteString()
                },
                new ContractInitializationMethodCall{
                    MethodName = nameof(EconomicContractContainer.EconomicContractStub.IssueNativeToken),
                    Params = new IssueNativeTokenInput
                    {
                        Amount = Convert.ToInt64(_economicOptions.TotalSupply * (1 - _economicOptions.DividendPoolRatio)),
                        To = Address.FromPublicKey(
                            ByteArrayHelper.HexStringToByteArray(_consensusOptions.InitialMinerList.First())),
                        Memo = "Issue native token"
                    }.ToByteString()
                }
            };
        }
    }
}