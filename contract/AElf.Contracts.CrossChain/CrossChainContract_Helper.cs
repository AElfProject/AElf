using System.Collections.Generic;
using System.Linq;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
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

        private Hash ComputeRootWithTransactionStatusMerklePath(Hash txId, MerklePath path)
        {
            var txResultStatusRawBytes =
                EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString());
            var hash = Hash.FromRawBytes(txId.ToByteArray().Concat(txResultStatusRawBytes).ToArray());
            return path.ComputeRootWithLeafNode(hash);
        }

        private Hash ComputeRootWithMultiHash(IEnumerable<Hash> nodes)
        {
            return BinaryMerkleTree.FromLeafNodes(nodes).Root;
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

        private void CreateSideChainToken(SideChainCreationRequest sideChainCreationRequest, 
            SideChainTokenInfo sideChainTokenInfo, int chainId)
        {
            TransferFrom(new TransferFromInput
            {
                From = Context.Origin,
                To = Context.Self,
                Amount = sideChainCreationRequest.LockedTokenAmount,
                Symbol = Context.Variables.NativeSymbol
            });
            State.IndexingBalance[chainId] = sideChainCreationRequest.LockedTokenAmount;
            
            CreateSideChainToken(sideChainTokenInfo, chainId);
        }

        private void UnlockTokenAndResource(SideChainInfo sideChainInfo)
        {
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
        }
        
        private void AssertSideChainTokenInfo(SideChainTokenInfo sideChainTokenInfo)
        {
            Assert(
                !string.IsNullOrEmpty(sideChainTokenInfo.Symbol) 
                && !string.IsNullOrEmpty(sideChainTokenInfo.TokenName),
                "Invalid side chain token name,");
            Assert(sideChainTokenInfo.TotalSupply > 0, "Invalid side chain token supply.");
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

        private void CreateSideChainToken(SideChainTokenInfo sideChainTokenInfo, int chainId)
        {
            ValidateContractState(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            State.TokenContract.Create.Send(new CreateInput
            {
                TokenName = sideChainTokenInfo.TokenName,
                Decimals = sideChainTokenInfo.Decimals,
                IsBurnable = sideChainTokenInfo.IsBurnable,
                Issuer = Context.Origin,
                IssueChainId = chainId,
                IsTransferDisabled = false,
                Symbol = sideChainTokenInfo.Symbol,
                TotalSupply = sideChainTokenInfo.TotalSupply
            });
        }

        private TokenInfo GetNativeTokenInfo()
        {
            ValidateContractState(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            return State.TokenContract.GetNativeTokenInfo.Call(new Empty());
        }

        private TokenInfo GetTokenInfo(string symbol)
        {
            ValidateContractState(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            return State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = symbol
            });
        }

        private TokenInfoList GetResourceTokenInfo()
        {
            ValidateContractState(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            return State.TokenContract.GetResourceTokenInfo.Call(new Empty());
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
                indexedSideChainData.SideChainBlockData.Select(d => d.TransactionStatusMerkleTreeRoot));
        }
        
        private Hash GetCousinChainMerkleTreeRoot(long parentChainHeight)
        {
            return State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight];
        }

        private Hash GetMerkleTreeRoot(int chainId, long parentChainHeight)
        {
            if (chainId == State.ParentChainId.Value)
            {
                // it is parent chain
                return GetParentChainMerkleTreeRoot(parentChainHeight);
            }

            if (State.SideChainInfo[chainId] != null)
            {
                // it is child chain
                return GetSideChainMerkleTreeRoot(parentChainHeight);
            }

            return GetCousinChainMerkleTreeRoot(parentChainHeight);
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

        private void AssertCurrentMiner()
        {
            ValidateContractState(State.ConsensusContract, SmartContractConstants.ConsensusContractSystemName);
            var isCurrentMiner = State.ConsensusContract.IsCurrentMiner.Call(Context.Sender).Value;
            Context.LogDebug(() => $"Sender is currentMiner : {isCurrentMiner}.");
            Assert(isCurrentMiner, "No permission.");
        }

        private void AssertParentChainBlock(int parentChainId, long currentRecordedHeight, ParentChainBlockData parentChainBlockData)
        {
            Assert(parentChainId == parentChainBlockData.ChainId, "Wrong parent chain id.");
            Assert(currentRecordedHeight + 1 == parentChainBlockData.Height,
                $"Parent chain block info at height {currentRecordedHeight + 1} is needed, not {parentChainBlockData.Height}");
            Assert(parentChainBlockData.TransactionStatusMerkleTreeRoot != null,
                "Parent chain transaction status merkle tree root needed.");
        }

        private int GetChainId(long serialNumber)
        {
            return ChainHelper.GetChainId(serialNumber + Context.ChainId);
        }
    }
}