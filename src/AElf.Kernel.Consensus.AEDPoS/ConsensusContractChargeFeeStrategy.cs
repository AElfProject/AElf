using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS
{
    public class ConsensusContractChargeFeeStrategy : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public Address ContractAddress =>
            _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);

        public string MethodName => string.Empty;

        public ConsensusContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public bool IsFree(Transaction transaction)
        {
            return new List<string>
            {
                nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateValue),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateTinyBlockInformation),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextTerm)
            }.Contains(transaction.MethodName);
        }
    }
}