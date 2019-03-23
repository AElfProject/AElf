using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
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

            State.ConsensusContract.Value = input.ConsensusContractAddress;
            State.TokenContract.Value = input.TokenContractAddress;
            //State.AuthorizationContract.Value = authorizationContractAddress;
            State.Initialized.Value = true;
            State.ParentChainId.Value = input.ParentChainId;
            return new Empty();
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

//            var miners = State.ConsensusContract.GetCurrentMiners();
//            Console.WriteLine(miners.PublicKeys.ToString());
            // fire event
            Context.FireEvent(new SideChainCreationRequested
            {
                ChainId = chainId,
                Creator = Context.Sender,
                Miners = new Miners()
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

            State.TokenContract.TransferFrom.Send(new TransferFromInput
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
            Context.FireEvent(new SideChainDisposal
            {
                ChainId = chainId
            });
            return new SInt64Value {Value = chainId};
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
                dict.IdHeighDict.Add(chainId, height);
            }

            return dict;
        }

        public override SideChainIdAndHeightDict GetAllChainsIdAndHeight(Empty input)
        {
            var dict = GetSideChainIdAndHeight(new Empty());

            if (State.ParentChainId.Value == 0)
                return dict;
            var parentChainHeight = State.CurrentParentChainHeight.Value;
            dict.IdHeighDict.Add(State.ParentChainId.Value, parentChainHeight);
            return dict;
        }
        
        #endregion Side chain lifetime actions

        #region Cross chain actions

        public override CrossChainBlockData GetIndexedCrossChainBlockDataByHeight(SInt64Value input)
        {
            var indexedCrossChainBlockData = State.IndexedCrossChainBlockData[input.Value];
            Assert(indexedCrossChainBlockData != null);
            return indexedCrossChainBlockData;
        }

        public override MerklePath GetMerklePathByHeight(SInt64Value input)
        {
            var merklePath = State.TxRootMerklePathInParentChain[input.Value];
            Assert(merklePath != null);
            return merklePath;
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
                var target = currentHeight != 0 ? currentHeight + 1 : CrossChainConsts.GenesisBlockHeight;
                Assert(target == parentChainHeight,
                    $"Parent chain block info at height {target} is needed, not {parentChainHeight}");

                var merkleTreeRoot = State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight];
                Assert(merkleTreeRoot == null,
                    $"Already written parent chain block info at height {parentChainHeight}");
                foreach (var indexedBlockInfo in blockInfo.IndexedMerklePath)
                {
                    BindParentChainHeight(indexedBlockInfo.Key, parentChainHeight);
                    AddIndexedTxRootMerklePathInParentChain(indexedBlockInfo.Key, indexedBlockInfo.Value);
                }

                // send consensus data shared from main chain  
                if (blockInfo.ExtraData.TryGetValue("Consensus", out var bytes))
                {
                    State.ConsensusContract.UpdateMainChainConsensus.Send(DPoSHeaderInformation.Parser.ParseFrom(bytes));
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
                    : CrossChainConsts.GenesisBlockHeight;
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

                State.TokenContract.Transfer.Send(new TransferInput
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

        /// <summary>
        /// Cross chain txn verification.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override BoolValue VerifyTransaction(VerifyTransactionInput input)
        {
            var parentChainHeight = input.ParentChainHeight;
            var path = input.MerklePath;
            var tx = input.TransactionId;
            var key = new Int64Value {Value = parentChainHeight};
            var merkleTreeRoot = State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight];
            Assert(merkleTreeRoot != null,
                $"Parent chain block at height {parentChainHeight} is not recorded.");
            var rootCalculated = path.ComputeRootWith(tx);
            
            //Api.Assert((parentRoot??Hash.Empty).Equals(rootCalculated), "Transaction verification Failed");
            Assert(merkleTreeRoot.Equals(rootCalculated), "Verification Failed.");
            return new BoolValue() {Value = true};
        }

        #endregion Cross chain actions

        #region Private actions

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

        private void LockTokenAndResource(SideChainInfo sideChainInfo)
        {
            //Api.Assert(request.Proposer.Equals(Api.GetFromAddress()), "Unable to lock token or resource.");

//            var balance = State.TokenContract.GetBalance(new GetBalanceInput
//            {
//                Owner = Context.Sender,
//                Symbol = "ELF"
//            });
//            Console.WriteLine($"{balance.Balance} Balance.");
            // update locked token balance
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = sideChainInfo.LockedTokenAmount,
                Symbol = "ELF"
            });
            var chainId = sideChainInfo.SideChainId;
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
                State.TokenContract.Transfer.Send(new TransferInput
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

        #endregion
    }
}