using System.Collections.Generic;
using System.Linq;
using Acs3;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract
    {
        private const string ConsensusExtraDataName = "Consensus";
        
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
            return new MerklePath().AddRange(path).ComputeRootWith(
                Hash.FromRawBytes(txId.DumpByteArray().Concat(txResultStatusRawBytes).ToArray()));
        }

        private Hash ComputeRootWithMultiHash(IEnumerable<Hash> nodes)
        {
            return nodes.ComputeBinaryMerkleTreeRootWithLeafNodes();
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
            
            TransferFrom(new TransferFromInput
            {
                From = Context.Origin,
                To = Context.Self,
                Amount = sideChainInfo.LockedTokenAmount,
                Symbol = Context.Variables.NativeSymbol
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
                    Symbol = Context.Variables.NativeSymbol
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
            state.Value = Context.GetContractAddressByName(contractSystemName);
        }

        private void Transfer(TransferInput input)
        {
            ValidateContractState(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            State.TokenContract.Transfer.Send(input);
        }

        private void TransferFrom(TransferFromInput input)
        {
            ValidateContractState(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            State.TokenContract.TransferFrom.Send(input);
        }

        private MinerListWithRoundNumber GetCurrentMiners()
        {
            ValidateContractState(State.ConsensusContract, SmartContractConstants.ConsensusContractSystemName);
            var miners = State.ConsensusContract.GetCurrentMinerListWithRoundNumber.Call(new Empty());
            return miners;
        }

        // only for side chain
        private void UpdateCurrentMiners(ByteString bytes)
        {
            ValidateContractState(State.ConsensusContract, SmartContractConstants.ConsensusContractSystemName);
            State.ConsensusContract.UpdateConsensusInformation.Send(new ConsensusInformation {Value = bytes});
        }

        private Hash GetParentChainMerkleTreeRoot(long parentChainHeight)
        {
            return State.ParentChainTransactionStatusMerkleTreeRoot[parentChainHeight];
        }
        
        private Hash GetSideChainMerkleTreeRoot(long parentChainHeight)
        {
            var indexedSideChainData = State.IndexedSideChainBlockData[parentChainHeight];
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
            ValidateContractState(State.ParliamentAuthContract, SmartContractConstants.ParliamentAuthContractSystemName);
            Address organizationAddress = State.ParliamentAuthContract.GetGenesisOwnerAddress.Call(new Empty());
            State.Owner.Value = organizationAddress;

            return State.Owner.Value;
        }
        
        private void CheckOwnerAuthority()
        {
            var owner = GetOwnerAddress();
            Assert(owner.Equals(Context.Sender), "Not authorized to do this.");
        }
    }
}