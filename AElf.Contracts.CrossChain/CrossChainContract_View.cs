using System.Linq;
using AElf.Common;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp.Utils;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    // ReSharper disable once PossibleNullReferenceException

    public partial class CrossChainContract
    {
        public override CrossChainMerkleProofContext GetBoundParentChainHeightAndMerklePathByHeight(SInt64Value input)
        {
            var boundParentChainHeight = State.ChildHeightToParentChainHeight[input.Value];
            Assert(boundParentChainHeight != 0);
            var merklePath = State.TxRootMerklePathInParentChain[input.Value];
            Assert(merklePath != null);
            return new CrossChainMerkleProofContext
            {
                MerklePathForParentChainRoot = merklePath,
                BoundParentChainHeight = boundParentChainHeight
            };
        }
        
        /// <summary>
        /// Cross chain txn verification.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override BoolValue VerifyTransaction(VerifyTransactionInput input)
        {
            var parentChainHeight = input.ParentChainHeight;
            var merkleTreeRoot = GetMerkleTreeRoot(input.VerifiedChainId, parentChainHeight);
            Assert(merkleTreeRoot != null,
                $"Parent chain block at height {parentChainHeight} is not recorded.");
            var rootCalculated = ComputeRootWithTransactionStatusMerklePath(input.TransactionId, input.Path);
            
            //Api.Assert((parentRoot??Hash.Empty).Equals(rootCalculated), "Transaction verification Failed");
            return new BoolValue {Value = merkleTreeRoot.Equals(rootCalculated)};
        }
        public override SInt32Value GetChainStatus(SInt32Value input)
        {
            var info = State.SideChainInfos[input.Value];
            Assert(info != null, "Not existed side chain.");
            return new SInt32Value() {Value = (int) info.SideChainStatus};
        }

        public override SInt64Value GetSideChainHeight(SInt32Value input)
        {
            var height = State.CurrentSideChainHeight[input.Value];
            Assert(height != 0);
            return new SInt64Value() {Value = height};
        }

        public override SInt64Value GetParentChainHeight(Empty input)
        {
            return new SInt64Value() {Value = State.CurrentParentChainHeight.Value};
        }

        public override SInt32Value GetParentChainId(Empty input)
        {
            var parentChainId = State.ParentChainId.Value;
            Assert(parentChainId != 0);
            return new SInt32Value() {Value = parentChainId};
        }

        public override SInt64Value LockedBalance(SInt32Value input)
        {
            var chainId = input.Value;
            var sideChainInfo = State.SideChainInfos[chainId];
            Assert(sideChainInfo != null, "Not existed side chain.");
            Assert(Context.Sender.Equals(sideChainInfo.Proposer), "Unable to check balance.");
            return new SInt64Value() {Value = State.IndexingBalance[chainId]};
        }

        public override SideChainIdAndHeightDict GetSideChainIdAndHeight(Empty input)
        {
            var dict = new SideChainIdAndHeightDict();
            var serialNumber = State.SideChainSerialNumber.Value;
            for (long i = 1; i <= serialNumber; i++)
            {
                int chainId = ChainHelpers.GetChainId(i);
                var sideChainInfo = State.SideChainInfos[chainId];
                if (sideChainInfo.SideChainStatus != SideChainStatus.Active)
                    continue;
                var height = State.CurrentSideChainHeight[chainId];
                dict.IdHeightDict.Add(chainId, height);
            }

            return dict;
        }

        public override SideChainIdAndHeightDict GetAllChainsIdAndHeight(Empty input)
        {
            var dict = GetSideChainIdAndHeight(new Empty());

            if (State.ParentChainId.Value == 0)
                return dict;
            var parentChainHeight = State.CurrentParentChainHeight.Value;
            dict.IdHeightDict.Add(State.ParentChainId.Value, parentChainHeight);
            return dict;
        }
        
        
        public override SInt64Value CurrentSideChainSerialNumber(Empty input)
        {
            return new SInt64Value() {Value = State.SideChainSerialNumber.Value};
        }

        public override SInt64Value LockedToken(SInt32Value input)
        {
            var info = State.SideChainInfos[input.Value];
            Assert(info != null, "Not existed side chain.");
            Assert(info.SideChainStatus != (SideChainStatus) 3, "Disposed side chain.");
            return new SInt64Value() {Value = info.LockedTokenAmount};
        }

        public override Address LockedAddress(SInt32Value input)
        {
            var info = State.SideChainInfos[input.Value];
            Assert(info != null, "Not existed side chain.");
            Assert(info.SideChainStatus != (SideChainStatus) 3, "Disposed side chain.");
            return info.Proposer;
        }

    }
}