using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class AEDPosSystemTransactionRecognizer : ISystemTransactionRecognizer
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public AEDPosSystemTransactionRecognizer(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public bool IsSystemTransaction(Transaction transaction)
        {
            return transaction.To ==
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                    .Name) && IsSystemTransactionMethod(transaction.MethodName);
        }

        private bool IsSystemTransactionMethod(string methodName)
        {
            return new List<string>
            {
                nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateValue),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateTinyBlockInformation),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextTerm)
            }.Contains(methodName);
        }
    }
}