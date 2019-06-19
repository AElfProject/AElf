using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using AElf.Types;
using Acs7;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract : CrossChainContractContainer.CrossChainContractBase
    {
        private int RequestChainCreationWaitingPeriod { get; } = 24 * 60 * 60;

        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            //State.AuthorizationContract.Value = authorizationContractAddress;
            State.Initialized.Value = true;
            State.ParentChainId.Value = input.ParentChainId;
            State.CreationHeightOnParentChain.Value = input.CreationHeightOnParentChain;
            State.CurrentParentChainHeight.Value = input.CreationHeightOnParentChain - 1;
            return new Empty();
        }

        #region Side chain lifetime actions

        /// <summary>
        /// Request from normal address to create side chain.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override RequestChainCreationOutput RequestChainCreation(SideChainCreationRequest input)
        {
            // no need to check authority since invoked in transaction from normal address
            Assert(input.LockedTokenAmount > 0 && input.LockedTokenAmount > input.IndexingPrice && !input.ContractCode.IsEmpty,
                "Invalid chain creation request.");

            State.SideChainSerialNumber.Value = State.SideChainSerialNumber.Value + 1;
            var serialNumber = State.SideChainSerialNumber.Value;
            int chainId = ChainHelpers.GetChainId(serialNumber);
            var info = State.SideChainInfos[chainId];
            Assert(info == null, "Chain creation request already exists.");

            // lock token and resource
            LockTokenAndResource(input, chainId);

            // side chain creation proposal
            Hash proposalId = Propose(RequestChainCreationWaitingPeriod, Context.Self, nameof(CreateSideChain),
                new SInt32Value {Value = chainId});
//            request.ProposalHash = hash;
            var sideChainInfo = new SideChainInfo
            {
                Proposer = Context.Sender,
                SideChainId = chainId,
                SideChainStatus = SideChainStatus.Review,
                SideChainCreationRequest = input
            };
            State.SideChainInfos[chainId] = sideChainInfo;

            return new RequestChainCreationOutput {ChainId = chainId, ProposalId = proposalId};
        }

        public override Empty WithdrawRequest(SInt32Value input)
        {
            var chainId = input.Value;
            // no need to check authority since invoked in transaction from normal address
            var sideChainInfo = State.SideChainInfos[chainId];
            Assert(sideChainInfo != null &&
                   sideChainInfo.SideChainStatus == SideChainStatus.Review,
                "Side chain creation request not found.");

            Assert(Context.Sender.Equals(sideChainInfo.Proposer), "Authentication failed.");
            UnlockTokenAndResource(sideChainInfo);
            sideChainInfo.SideChainStatus = SideChainStatus.Terminated;
            State.SideChainInfos[chainId] = sideChainInfo;
            return new Empty();
        }

        /// <summary>
        /// Create side chain. It is a proposal result from system address.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt32Value CreateSideChain(SInt32Value input)
        {
            var chainId = input.Value;
            // side chain creation should be triggered by organization address from parliament.
            CheckOwnerAuthority();
            var sideChainInfo = State.SideChainInfos[chainId];
            
            Assert(
                sideChainInfo != null &&
                sideChainInfo.SideChainStatus == SideChainStatus.Review, "Side chain creation request not found.");

            sideChainInfo.SideChainStatus = SideChainStatus.Active;
            sideChainInfo.CreationTimestamp = Context.CurrentBlockTime;
            sideChainInfo.CreationHeightOnParentChain = Context.CurrentHeight;
            State.SideChainInfos[chainId] = sideChainInfo;
            State.CurrentSideChainHeight[chainId] = 0;

            var initialConsensusInfo = GetCurrentMiners();
            State.SideChainInitialConsensusInfo[chainId] = new BytesValue{Value = initialConsensusInfo.ToByteString()};
            Context.LogDebug(() => $"Initial miner list for side chain {chainId} :" +
                                   string.Join(",",
                                       initialConsensusInfo.MinerList.PublicKeys));
            Context.LogDebug(() => $"RoundNumber {initialConsensusInfo.RoundNumber}");
            // Event is not used for now.
            Context.Fire(new CreationRequested()
            {
                ChainId = chainId,
                Creator = Context.Sender
            });
            return new SInt32Value() {Value = chainId};
        }

        /// <summary>
        /// Recharge for side chain.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Recharge(RechargeInput input)
        {
            var chainId = input.ChainId;
            var amount = input.Amount;
            var sideChainInfo = State.SideChainInfos[chainId];
            Assert(
                sideChainInfo != null &&
                (sideChainInfo.SideChainStatus == SideChainStatus.Active ||
                 sideChainInfo.SideChainStatus == SideChainStatus.InsufficientBalance),
                "Side chain not found or not able to be recharged.");
            State.IndexingBalance[chainId] = State.IndexingBalance[chainId] + amount;
            if (State.IndexingBalance[chainId] > sideChainInfo.SideChainCreationRequest.IndexingPrice)
            {
                sideChainInfo.SideChainStatus = SideChainStatus.Active;
                State.SideChainInfos[chainId] = sideChainInfo;
            }
           
            TransferFrom(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = Context.Variables.NativeSymbol,
                Amount = amount,
                Memo = "Recharge."
            });
            return new Empty();
        }

        /// <summary>
        /// Request form normal address to dispose side chain
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Hash RequestChainDisposal(SInt32Value input)
        {
            // no need to check authority since invoked in transaction from normal address
            var request = State.SideChainInfos[input.Value];
            Assert(
                request != null && (request.SideChainStatus == SideChainStatus.Active || request.SideChainStatus == SideChainStatus.InsufficientBalance),"Side chain not found");
            
            Assert(Context.Sender.Equals(request.Proposer), "Not authorized to dispose.");

            // side chain disposal
            Hash proposalHash = Propose(RequestChainCreationWaitingPeriod, Context.Self, nameof(DisposeSideChain),
                input);
            return proposalHash;
        }

        /// <summary>
        /// Dispose side chain. It is a proposal result from system address.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value DisposeSideChain(SInt32Value input)
        {
            CheckOwnerAuthority();

            var chainId = input.Value;
            // side chain disposal should be triggered by multi sig txn from system address.
            //CheckAuthority(Context.Genesis);
            var info = State.SideChainInfos[chainId];
            Assert(info != null, "Not existed side chain.");
            Assert(info.SideChainStatus == SideChainStatus.Active || info.SideChainStatus == SideChainStatus.InsufficientBalance, "Unable to dispose this side chain.");

            UnlockTokenAndResource(info);
            info.SideChainStatus = SideChainStatus.Terminated;
            State.SideChainInfos[chainId] = info;
            Context.Fire(new Disposed()
            {
                ChainId = chainId
            });
            return new SInt64Value {Value = chainId};
        }
        
        #endregion Side chain lifetime actions

        #region Cross chain actions

        public override CrossChainBlockData GetIndexedCrossChainBlockDataByHeight(SInt64Value input)
        {
            var indexedCrossChainBlockData = State.IndexedCrossChainBlockData[input.Value];
            Assert(indexedCrossChainBlockData != null);
            return indexedCrossChainBlockData;
        }

        public override Empty RecordCrossChainData(CrossChainBlockData input)
        {
            var crossChainBlockData = input;
            //Assert(IsMiner(), "Not authorized to do this.");
            var indexedCrossChainData = State.IndexedCrossChainBlockData[Context.CurrentHeight];
            Assert(indexedCrossChainData == null); // This should not fail.
            
            var sideChainBlockData = crossChainBlockData.SideChainBlockData;
            IndexParentChainBlockInfo(crossChainBlockData.ParentChainBlockData.ToArray());
            var indexedSideChainBlockData = IndexSideChainBlockInfo(sideChainBlockData.ToArray());

            var actualCrossChainData = new CrossChainBlockData();
            actualCrossChainData.ParentChainBlockData.AddRange(crossChainBlockData.ParentChainBlockData);
            actualCrossChainData.SideChainBlockData.AddRange(indexedSideChainBlockData);
            State.IndexedCrossChainBlockData[Context.CurrentHeight] = actualCrossChainData;
            return new Empty();
        }

        /// <summary>
        /// Index parent chain block data.
        /// </summary>
        /// <param name="parentChainBlockData"></param>
        private void IndexParentChainBlockInfo(ParentChainBlockData[] parentChainBlockData)
        {
            // only miner can do this.
            //Api.IsMiner("Not authorized to do this.");
//            Assert(parentChainBlockData.Length <= 256, "Beyond maximal capacity for once indexing.");
            var parentChainId = State.ParentChainId.Value;
            var currentHeight = State.CurrentParentChainHeight.Value;
            
            for (var i = 0; i < parentChainBlockData.Length; i++)
            {
                var blockInfo = parentChainBlockData[i];
                Assert(parentChainId == blockInfo.ChainId, "Wrong parent chain id.");
                long parentChainHeight = blockInfo.Height;
                var targetHeight = currentHeight + 1;
                Assert(targetHeight == parentChainHeight,
                    $"Parent chain block info at height {targetHeight} is needed, not {parentChainHeight}");
                Assert(blockInfo.TransactionStatusMerkleRoot != null,
                    "Parent chain transaction status merkle tree root needed.");
                State.ParentChainTransactionStatusMerkleTreeRoot[parentChainHeight] = blockInfo.TransactionStatusMerkleRoot;
                foreach (var indexedBlockInfo in blockInfo.IndexedMerklePath)
                {
                    BindParentChainHeight(indexedBlockInfo.Key, parentChainHeight);
                    AddIndexedTxRootMerklePathInParentChain(indexedBlockInfo.Key, indexedBlockInfo.Value);
                }

                // send consensus data shared from main chain  
                if (i == parentChainBlockData.Length - 1 && blockInfo.ExtraData.TryGetValue("Consensus", out var bytes))
                {
                    Context.LogDebug(() => "Updating consensus information..");
                    UpdateCurrentMiners(bytes);
                }
                
                if (blockInfo.CrossChainExtraData != null)
                    State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight] =
                        blockInfo.CrossChainExtraData.SideChainTransactionsRoot;

                currentHeight = targetHeight;
            }

            State.CurrentParentChainHeight.Value = currentHeight;
        }

        /// <summary>
        /// Index side chain block data.
        /// </summary>
        /// <param name="sideChainBlockData">Side chain block data to be indexed.</param>
        /// <returns>Valid side chain block data which are indexed.</returns>
        private List<SideChainBlockData> IndexSideChainBlockInfo(SideChainBlockData[] sideChainBlockData)
        {
            // only miner can do this.
//            Api.IsMiner("Not authorized to do this.");

            var indexedSideChainBlockData = new List<SideChainBlockData>();
            for (var i = 0; i < sideChainBlockData.Length; i++)
            {
                var blockInfo = sideChainBlockData[i];
                var chainId = blockInfo.ChainId;
                var info = State.SideChainInfos[chainId];
                if (info == null || info.SideChainStatus != SideChainStatus.Active)
                    continue;
                var currentSideChainHeight = State.CurrentSideChainHeight[chainId];
                
                var target = currentSideChainHeight != 0
                    ? currentSideChainHeight + 1
                    : Constants.GenesisBlockHeight;
                long sideChainHeight = blockInfo.Height;
                if (target != sideChainHeight)
                    continue;

                // indexing fee
                var indexingPrice = info.SideChainCreationRequest.IndexingPrice;
                var lockedToken = State.IndexingBalance[chainId];

                lockedToken -= indexingPrice;
                State.IndexingBalance[chainId] = lockedToken;
                
                if (lockedToken < indexingPrice)
                {
                    info.SideChainStatus = SideChainStatus.Terminated;
                }
                
                State.SideChainInfos[chainId] = info;

                Transfer(new TransferInput
                {
                    To = Context.Sender,
                    Symbol = Context.Variables.NativeSymbol,
                    Amount = indexingPrice,
                    Memo = "Index fee."
                });

                State.CurrentSideChainHeight[chainId] = sideChainHeight;
                indexedSideChainBlockData.Add(blockInfo);
            }
            
            return indexedSideChainBlockData;
        }
        
        #endregion Cross chain actions

        public override Empty ChangOwnerAddress(Address input)
        {
            CheckOwnerAuthority();
            State.Owner.Value = input;
            return new Empty();
        }
    }
}