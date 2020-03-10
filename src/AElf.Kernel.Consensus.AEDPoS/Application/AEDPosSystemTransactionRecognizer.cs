using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class AEDPosSystemTransactionRecognizer : SystemTransactionRecognizerBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public AEDPosSystemTransactionRecognizer(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public override bool IsSystemTransaction(Transaction transaction)
        {
            return CheckSystemContractAddress(transaction.To,
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                    .Name)) && CheckSystemContractMethod(transaction.MethodName,
                nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateValue),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateTinyBlockInformation),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextTerm));
        }
    }
}