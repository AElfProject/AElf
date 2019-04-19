using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.CSharp.Core.Utils;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract
    {
        /// <summary>
        /// Bind parent chain height together with self height.
        /// </summary>
        /// <param name="childHeight"></param>
        /// <param name="parentHeight"></param>
        private void BindParentChainHeight(long childHeight, long parentHeight)
        {
            Assert(State.ChildHeightToParentChainHeight[childHeight] == 0,
                $"Already bound at height {childHeight} with parent chain");
            State.ChildHeightToParentChainHeight[childHeight] = parentHeight;
        }

        private Hash ComputeRootWithTransactionStatusMerklePath(Hash txId, IEnumerable<Hash> path)
        {
            var txResultStatusRawBytes =
                EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString());
            return new MerklePath(path).ComputeRootWith(
                Hash.FromRawBytes(txId.DumpByteArray().Concat(txResultStatusRawBytes).ToArray()));
        }

        private Hash ComputeRootWithMultiHash(IEnumerable<Hash> nodes)
        {
            var binaryMerkleTree = new BinaryMerkleTree();
            binaryMerkleTree.AddNodes(nodes);
            return binaryMerkleTree.ComputeRootHash();
        }
        
        /// <summary>
        /// Record merkle path of self chain block, which is from parent chain. 
        /// </summary>
        /// <param name="height"></param>
        /// <param name="path"></param>
        private void AddIndexedTxRootMerklePathInParentChain(long height, MerklePath path)
        {
            var existing = State.TxRootMerklePathInParentChain[height];
            Assert(existing == null,
                $"Merkle path already bound at height {height}.");
            State.TxRootMerklePathInParentChain[height] = path;
        }

        private void LockTokenAndResource(SideChainCreationRequest sideChainInfo, int chainId)
        {
            //Api.Assert(request.Proposer.Equals(Api.GetFromAddress()), "Unable to lock token or resource.");
            // update locked token balance
            
            var balance = GetBalance(new GetBalanceInput
            {
                Owner = Context.Sender,
                Symbol = "ELF"
            });

            Assert(balance > 0);
            
            TransferFrom(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = sideChainInfo.LockedTokenAmount,
                Symbol = "ELF"
            });
            State.IndexingBalance[chainId] = sideChainInfo.LockedTokenAmount;
            // Todo: enable resource
            // lock 
            /*foreach (var resourceBalance in sideChainInfo.ResourceBalances)
            {
                Api.LockResource(resourceBalance.Amount, resourceBalance.Type);
            }*/
        }

        private void UnlockTokenAndResource(SideChainInfo sideChainInfo)
        {
            //Api.Assert(sideChainInfo.LockedAddress.Equals(Api.GetFromAddress()), "Unable to withdraw token or resource.");
            // unlock token
            var chainId = sideChainInfo.SideChainId;
            var balance = State.IndexingBalance[chainId];
            if (balance != 0)
                Transfer(new TransferInput
                {
                    To = sideChainInfo.Proposer,
                    Amount = balance,
                    Symbol = "ELF"
                });
            State.IndexingBalance[chainId] = 0;

            // unlock resource 
            /*foreach (var resourceBalance in sideChainInfo.ResourceBalances)
            {
                Api.UnlockResource(resourceBalance.Amount, resourceBalance.Type);
            }*/
        }

        private void ValidateContractState(ContractReferenceState state, Hash contractSystemName)
        {
            if (state.Value != null)
                return;
            state.Value = State.BasicContractZero.GetContractAddressByName.Call(contractSystemName);
        }

        private void Transfer(TransferInput input)
        {
            ValidateContractState(State.TokenContract, State.TokenContractSystemName.Value);
            State.TokenContract.Transfer.Send(input);
        }

        private void TransferFrom(TransferFromInput input)
        {
            ValidateContractState(State.TokenContract, State.TokenContractSystemName.Value);
            State.TokenContract.TransferFrom.Send(input);
        }

        private long GetBalance(GetBalanceInput input)
        {
            ValidateContractState(State.TokenContract, State.TokenContractSystemName.Value);
            var output = State.TokenContract.GetBalance.Call(input);
            return output.Balance;
        }

        private MinerListWithRoundNumber GetCurrentMiners()
        {
            ValidateContractState(State.ConsensusContract, State.ConsensusContractSystemName.Value);
            var miners = State.ConsensusContract.GetCurrentMiners.Call(new Empty());
            return miners;
        }

        // only for side chain
        private void UpdateCurrentMiners(ByteString bytes)
        {
            ValidateContractState(State.ConsensusContract, State.ConsensusContractSystemName.Value);
            State.ConsensusContract.UpdateMainChainConsensus.Send(new ConsensusInformation{Bytes = bytes});
        }
        
        private Hash GetParentChainMerkleTreeRoot(long parentChainHeight)
        {
            return State.ParentChainTransactionStatusMerkleTreeRoot[parentChainHeight];
        }
        
        private Hash GetSideChainMerkleTreeRoot(long parentChainHeight)
        {
            var indexedSideChainData = State.IndexedCrossChainBlockData[parentChainHeight];
            return ComputeRootWithMultiHash(
                indexedSideChainData.SideChainBlockData.Select(d => d.TransactionMerkleTreeRoot));
        }
        
        private Hash GetCousinChainMerkleTreeRoot(long parentChainHeight)
        {
            return State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight];
        }

        private Hash GetMerkleTreeRoot(int chainId, long parentChainHeight)
        {
            if (State.ParentChainId.Value == 0)
            {
                // Local is main chain
                return GetSideChainMerkleTreeRoot(parentChainHeight);
            }

            return chainId != State.ParentChainId.Value
                ? GetCousinChainMerkleTreeRoot(parentChainHeight)
                : GetParentChainMerkleTreeRoot(parentChainHeight);
        }

        private Address GetOwnerAddress()
        {
            if (State.Owner.Value != null) 
                return State.Owner.Value;
            ValidateContractState(State.ParliamentAuthContract, State.ParliamentAuthContractSystemName.Value);
            var organizationInput = new CreateOrganizationInput
            {
                ReleaseThresholdInFraction = 2d / 3
            };
            Address organizationAddress = State.ParliamentAuthContract.GetOrganizationAddress.Call(organizationInput);
            State.Owner.Value = organizationAddress;

            return State.Owner.Value;
        }
        
        private void CheckOwnerAuthority()
        {
            var owner = GetOwnerAddress();
            Assert(owner.Equals(Context.Sender), "Not authorized to do this.");
        }
        
        private Hash Propose(int waitingPeriod, Address targetAddress, string invokingMethod, IMessage input)
        {
            var expiredTime = Context.CurrentBlockTime.AddSeconds(waitingPeriod).ToUniversalTime();
            var proposal = new CreateProposalInput
            {
                ContractMethodName = invokingMethod,
                OrganizationAddress = GetOwnerAddress(),
                ExpiredTime = Timestamp.FromDateTime(expiredTime),
                Params = input.ToByteString(),
                ToAddress = targetAddress
            };
            ValidateContractState(State.ParliamentAuthContract, State.ParliamentAuthContractSystemName.Value);
            State.ParliamentAuthContract.CreateProposal.Send(proposal);
            return Hash.FromMessage(proposal);
        }
    }
}