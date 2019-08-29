using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Kernel.TransactionPool.Application;

namespace AElf.Blockchains.SideChain
{
    public class SideChainSystemTransactionMethodNameListProvider : ISystemTransactionMethodNameListProvider
    {
        public List<string> GetSystemTransactionMethodNameList()
        {
            return new List<string>
            {
                nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextTerm),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateValue),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateTinyBlockInformation),
                nameof(TokenContractContainer.TokenContractStub.ClaimTransactionFees),
                nameof(TokenContractContainer.TokenContractStub.DonateResourceToken),
                nameof(CrossChainContractContainer.CrossChainContractStub.RecordCrossChainData)
            };
        }
    }
}