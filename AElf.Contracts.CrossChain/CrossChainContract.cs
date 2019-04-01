using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract : CrossChainContractContainer.CrossChainContractBase
    {
        private int RequestChainCreationWaitingPeriod { get; } = 24 * 60 * 60;

        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            State.ConsensusContractSystemName.Value = input.ConsensusContractSystemName;
            State.TokenContractSystemName.Value = input.TokenContractSystemName;
            //State.AuthorizationContract.Value = authorizationContractAddress;
            State.Initialized.Value = true;
            State.ParentChainId.Value = input.ParentChainId;
            return new Empty();
        }

        #region Side chain lifetime actions

        /// <summary>
        /// Request from normal address to create side chain.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt32Value RequestChainCreation(SideChainInfo input)
        {
            // no need to check authority since invoked in transaction from normal address
            Assert(
                input.SideChainStatus == SideChainStatus.Apply && input.Proposer != null &&
                Context.Sender.Equals(input.Proposer) && input.LockedTokenAmount > 0 &&
                input.LockedTokenAmount > input.IndexingPrice, "Invalid chain creation request.");

            State.SideChainSerialNumber.Value = State.SideChainSerialNumber.Value + 1;
            var serialNumber = State.SideChainSerialNumber.Value;
            int chainId = ChainHelpers.GetChainId(serialNumber);
            var info = State.SideChainInfos[chainId];
            Assert(info == null, "Chain creation request already exists.");

            // lock token and resource
            input.SideChainId = chainId;
            LockTokenAndResource(input);

            // side chain creation proposal
//            Hash hash = Propose("ChainCreation", RequestChainCreationWaitingPeriod, Context.Genesis,
//                Context.Self, CreateSideChainMethodName, ChainHelpers.ConvertChainIdToBase58(chainId));
            input.SideChainStatus = SideChainStatus.Review;
//            request.ProposalHash = hash;
            State.SideChainInfos[chainId] = input;

            return new SInt32Value() {Value = chainId};
        }

        public override Empty WithdrawRequest(SInt32Value input)
        {
            var chainId = input.Value;
            // no need to check authority since invoked in transaction from normal address
            var sideChainInfo = State.SideChainInfos[chainId];
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address. 
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
            // side chain creation should be triggered by multi sig txn from system address.
//            CheckAuthority(Context.Genesis);
            var request = State.SideChainInfos[chainId];
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address.
            Assert(
                request != null &&
                request.SideChainStatus == SideChainStatus.Review, "Side chain creation request not found.");

            request.SideChainStatus = SideChainStatus.Active;
            State.SideChainInfos[chainId] = request;
            State.CurrentSideChainHeight[chainId] = 0;

            var initialConsensusInfo = GetCurrentMiners();
            State.SideChainInitialConsensusInfo[chainId] = initialConsensusInfo;
            Context.LogDebug(() => $"Initial miner list for side chain {chainId} :" +
                                   string.Join(",",
                                       initialConsensusInfo.PublicKeys.Select(p =>
                                           Address.FromPublicKey(ByteArrayHelpers.FromHexString(p)).ToString())));
            Context.LogDebug(() => $"TermNumber {initialConsensusInfo.TermNumber}");
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
            if (State.IndexingBalance[chainId] > sideChainInfo.IndexingPrice)
            {
                sideChainInfo.SideChainStatus = SideChainStatus.Active;
                State.SideChainInfos[chainId] = sideChainInfo;
            }
           
            TransferFrom(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = "ELF",
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
        public override Empty RequestChainDisposal(SInt32Value input)
        {
            // no need to check authority since invoked in transaction from normal address
            var request = State.SideChainInfos[input.Value];
            Assert(
                request != null &&
                request.SideChainStatus == SideChainStatus.Active, "Side chain not found");

            Assert(Context.Sender.Equals(request.Proposer), "Not authorized to dispose.");

            // side chain disposal
//            Hash proposalHash = Propose("DisposeSideChain", RequestChainCreationWaitingPeriod, Context.Genesis,
//                Context.Self, DisposeSideChainMethodName, chainId);
            return new Empty();
        }

        /// <summary>
        /// Dispose side chain. It is a proposal result from system address.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value DisposeSideChain(SInt32Value input)
        {
            var chainId = input.Value;
            // side chain disposal should be triggered by multi sig txn from system address.
            //CheckAuthority(Context.Genesis);
            var info = State.SideChainInfos[chainId];
            Assert(info != null, "Not existed side chain.");

            // TODO: Only privileged account can trigger this method
            Assert(info.SideChainStatus == SideChainStatus.Active, "Unable to dispose this side chain.");

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
//            Context.FireEvent(new CrossChainIndexingEvent
//            {
//                SideChainTransactionsMerkleTreeRoot = calculatedRoot,
//                CrossChainBlockData = crossChainBlockData,
//                Sender = Context.Sender // for validation 
//            });
            return new Empty();
        }

        /// <summary>
        /// Index parent chain blocks.
        /// </summary>
        /// <param name="parentChainBlockData"></param>
        private void IndexParentChainBlockInfo(ParentChainBlockData[] parentChainBlockData)
        {
            // only miner can do this.
            //Api.IsMiner("Not authorized to do this.");
            Assert(parentChainBlockData.Length <= 256,"Beyond maximal capacity for once indexing.");
            var parentChainId = State.ParentChainId.Value;
            foreach (var blockInfo in parentChainBlockData)
            {
                Assert(parentChainId == blockInfo.Root.ParentChainId, "Wrong parent chain id.");
                long parentChainHeight = blockInfo.Root.ParentChainHeight;
                var currentHeight = State.CurrentParentChainHeight.Value;
                var target = currentHeight != 0 ? currentHeight + 1 : KernelConstants.GenesisBlockHeight;
                Assert(target == parentChainHeight,
                    $"Parent chain block info at height {target} is needed, not {parentChainHeight}");
                Assert(blockInfo.Root.TransactionStatusMerkleRoot != null,
                    "Parent chain transaction status merkle tree root needed.");
//                var merkleTreeRoot = State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight];
//                var parentTxStatusMerkleTreeRoot = State.ParentChainTransactionStatusMerkleTreeRoot[parentChainHeight];
//                Assert(merkleTreeRoot == null & parentTxStatusMerkleTreeRoot == null,
//                    $"Already indexed parent chain block info at height {parentChainHeight}");
                State.ParentChainTransactionStatusMerkleTreeRoot[parentChainHeight] = blockInfo.Root.TransactionStatusMerkleRoot;
                foreach (var indexedBlockInfo in blockInfo.IndexedMerklePath)
                {
                    BindParentChainHeight(indexedBlockInfo.Key, parentChainHeight);
                    AddIndexedTxRootMerklePathInParentChain(indexedBlockInfo.Key, indexedBlockInfo.Value);
                }

                // send consensus data shared from main chain  
                if (blockInfo.ExtraData.TryGetValue("Consensus", out var bytes))
                {
                    UpdateCurrentMiners(bytes);
                }

                State.CurrentParentChainHeight.Value = parentChainHeight;
                
                if (blockInfo.Root.CrossChainExtraData != null)
                    State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight] =
                        blockInfo.Root.CrossChainExtraData.SideChainTransactionsRoot;
            }
        }

        /// <summary>
        /// Index side chain block(s).
        /// </summary>
        /// <param name="sideChainBlockData"></param>
        /// <returns>Root of merkle tree created from side chain txn roots.</returns>
        private List<SideChainBlockData> IndexSideChainBlockInfo(SideChainBlockData[] sideChainBlockData)
        {
            // only miner can do this.
//            Api.IsMiner("Not authorized to do this.");
//            Api.Assert(sideChainBlockData.Length > 0, "Empty side chain block information.");
//            var currentHeight = Context.CurrentHeight;
//            var height = currentHeight + 1;
//            var result = State.IndexedSideChainBlockInfoResult[height];
//            Assert(result == null); // This should not happen.

//            var indexedSideChainBlockInfoResult = new IndexedSideChainBlockDataResult
//            {
//                Height = height,
//                Miner = Context.Self
//            };
//            var binaryMerkleTree = new BinaryMerkleTree();

            var indexedSideChainBlockData = new List<SideChainBlockData>();
            foreach (var blockInfo in sideChainBlockData)
            {
                var chainId = blockInfo.SideChainId;
                var info = State.SideChainInfos[chainId];
                if (info == null || info.SideChainStatus != SideChainStatus.Active)
                    continue;
                var currentSideChainHeight = State.CurrentSideChainHeight[chainId];
                
                var target = currentSideChainHeight != 0
                    ? currentSideChainHeight + 1
                    : KernelConstants.GenesisBlockHeight;
                long sideChainHeight = blockInfo.SideChainHeight;
                if (target != sideChainHeight)
                    continue;

                // indexing fee
                var indexingPrice = info.IndexingPrice;
                var lockedToken = State.IndexingBalance[chainId];

                lockedToken -= indexingPrice;
                State.IndexingBalance[chainId] = lockedToken;
                
                if (lockedToken < indexingPrice)
                {
                    info.SideChainStatus = SideChainStatus.InsufficientBalance;
                }
                State.SideChainInfos[chainId] = info;

                Transfer(new TransferInput
                {
                    To = Context.Sender,
                    Symbol = "ELF",
                    Amount = indexingPrice,
                    Memo = "Index fee."
                });

                State.CurrentSideChainHeight[chainId] = sideChainHeight;
                indexedSideChainBlockData.Add(blockInfo);
                //binaryMerkleTree.AddNode(blockInfo.TransactionMKRoot);
                //indexedSideChainBlockInfoResult.SideChainBlockData.Add(blockInfo);
            }
            return indexedSideChainBlockData;
            //State.IndexedSideChainBlockInfoResult[height] = indexedSideChainBlockInfoResult;

            // calculate merkle tree for side chain txn roots
            //binaryMerkleTree.ComputeRootHash();
            //return binaryMerkleTree.Root;
        }
        
        #endregion Cross chain actions
    }
}