using System;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;

namespace AElf.Contracts.CrossChain2
{
    public partial class CrossChainContract : CSharpSmartContract<CrossChainContractState>
    {
        private static string CreateSideChainMethodName { get; } = "CreateSideChain";
        private static string DisposeSideChainMethodName { get; } = "DisposeSideChain";

        private double RequestChainCreationWaitingPeriod { get; } = 24 * 60 * 60;

        [View]
        public ulong CurrentSideChainSerialNumber()
        {
            return State.SideChainSerialNumber.Value;
        }

        public ulong LockedToken(string chainId)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            var info = State.SideChainInfos[chainIdHash];
            Assert(info.IsNotEmpty(), "Not existed side chain.");
            Assert(info.SideChainStatus != (SideChainStatus) 3, "Disposed side chain.");
            return info.LockedTokenAmount;
        }

        public byte[] LockedAddress(string chainId)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            var info = State.SideChainInfos[chainIdHash];
            Assert(info.IsNotEmpty(), "Not existed side chain.");
            Assert(info.SideChainStatus != (SideChainStatus) 3, "Disposed side chain.");
            return info.Proposer.DumpByteArray();
        }

        #region Side chain lifetime actions

        /// <summary>
        /// Request from normal address to create side chain. 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public string ReuqestChainCreation(SideChainInfo request)
        {
            // no need to check authority since invoked in transaction from normal address
            Assert(
                request.SideChainStatus == SideChainStatus.Apply && request.Proposer != null &&
                Context.Sender.Equals(request.Proposer), "Invalid chain creation request.");

            State.SideChainSerialNumber.Value = State.SideChainSerialNumber.Value + 1;
            var serialNumber = State.SideChainSerialNumber.Value;
            int chainId = ChainHelpers.GetChainId(serialNumber);
            var info = State.SideChainInfos[chainId];
            Assert(info.IsEmpty(), "Chain creation request already exists.");

            // lock token and resource
            request.ChainId = chainId;
            LockTokenAndResource(request);

            // side chain creation proposal
            Hash hash = Propose("ChainCreation", RequestChainCreationWaitingPeriod, Context.Genesis,
                Context.Self, CreateSideChainMethodName, chainId.DumpBase58());
            request.SideChainStatus = SideChainStatus.Review;
            request.ProposalHash = hash;
            State.SideChainInfos[chainId] = request;
            var res = new JObject
            {
                ["proposal_hash"] = hash.ToHex(),
                ["chain_id"] = chainId.DumpBase58()
            };
            return res.ToString();
        }

        public void WithdrawRequest(string chainId)
        {
            // no need to check authority since invoked in transaction from normal address
            var chainIdHash = Hash.LoadBase58(chainId);

            var sideChainInfo = State.SideChainInfos[chainIdHash];
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address. 
            Assert(sideChainInfo.IsNotEmpty() &&
                   sideChainInfo.SideChainStatus == SideChainStatus.Review,
                "Side chain creation request not found.");

            Assert(Context.Sender.Equals(sideChainInfo.Proposer), "Not authorized to withdraw request.");
            UnlockTokenAndResource(sideChainInfo);
            sideChainInfo.SideChainStatus = SideChainStatus.Terminated;
            State.SideChainInfos[chainIdHash] = sideChainInfo;
        }

        /// <summary>
        /// Create side chain. It is a proposal result from system address. 
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        [Fee(0)]
        public string CreateSideChain(string chainId)
        {
            // side chain creation should be triggered by multi sig txn from system address.
            var chainIdHash = Hash.LoadBase58(chainId);
            CheckAuthority(Context.Genesis);

            var request = State.SideChainInfos[chainIdHash];
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address.
            Assert(
                request.IsNotEmpty() &&
                request.SideChainStatus == SideChainStatus.Review, "Side chain creation request not found.");

            request.SideChainStatus = SideChainStatus.Active;
            State.SideChainInfos[chainIdHash] = request;

            // fire event
            Context.FireEvent(new SideChainCreationRequested
            {
                ChainId = chainIdHash,
                Creator = Context.Sender
            });
            return chainId;
        }

        /// <summary>
        /// Recharge for side chain.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="amount"></param>
        public void Recharge(string chainId, ulong amount)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            var sideChainInfo = State.SideChainInfos[chainIdHash];
            Assert(
                sideChainInfo.IsNotEmpty() &&
                (sideChainInfo.SideChainStatus == SideChainStatus.Active ||
                 sideChainInfo.SideChainStatus == SideChainStatus.InsufficientBalance),
                "Side chain not found or not able to be recharged.");
            State.IndexingBalance[chainIdHash] = State.IndexingBalance[chainIdHash] + amount
            if (State.IndexingBalance[chainIdHash] > sideChainInfo.IndexingPrice)
            {
                sideChainInfo.SideChainStatus = SideChainStatus.Active;
                State.SideChainInfos[chainIdHash] = sideChainInfo;
            }

            State.TokenContract.Lock(Context.Sender, amount);
        }

        /// <summary>
        /// Request form normal address to dispose side chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public byte[] RequestChainDisposal(string chainId)
        {
            // no need to check authority since invoked in transaction from normal address
            var chainIdHash = Hash.LoadBase58(chainId);
            var request = State.SideChainInfos[chainIdHash];
            Assert(
                request.IsNotEmpty() &&
                request.SideChainStatus == SideChainStatus.Active, "Side chain not found");

            Assert(Context.Sender.Equals(request.Proposer), "Not authorized to dispose.");

            // side chain disposal
            Hash proposalHash = Propose("DisposeSideChain", RequestChainCreationWaitingPeriod, Context.Genesis,
                Context.Self, DisposeSideChainMethodName, chainId);
            return proposalHash.DumpByteArray();
        }

        /// <summary>
        /// Dispose side chain. It is a proposal result from system address. 
        /// </summary>
        /// <param name="chainId"></param>
        [Fee(0)]
        public void DisposeSideChain(string chainId)
        {
            // side chain disposal should be triggered by multi sig txn from system address.
            var chainIdHash = Hash.LoadBase58(chainId);
            CheckAuthority(Context.Genesis);
            var info = State.SideChainInfos[chainIdHash];
            Assert(info.IsNotEmpty(), "Not existed side chain.");

            // TODO: Only privileged account can trigger this method
            Assert(info.SideChainStatus == SideChainStatus.Active, "Unable to dispose this side chain.");

            UnlockTokenAndResource(info);
            info.SideChainStatus = SideChainStatus.Terminated;
            State.SideChainInfos[chainIdHash] = info;
            Context.FireEvent(new SideChainDisposal
            {
                chainId = chainIdHash
            });
        }

        [View]
        public int GetChainStatus(string chainId)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            var info = State.SideChainInfos[chainIdHash];
            Assert(info.IsNotEmpty(), "Not existed side chain.");
            return (int) info.SideChainStatus;
        }

        [View]
        public ulong LockedBalance(string chainId)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            var sideChainInfo = State.SideChainInfos[chainIdHash];
            Assert(sideChainInfo.IsNotEmpty(), "Not existed side chain.");
            Assert(Context.Sender.Equals(sideChainInfo.Proposer), "Unable to check balance.");
            return State.IndexingBalance[chainIdHash];
        }

        #endregion Side chain lifetime actions

        #region Cross chain actions

        [Fee(0)]
        public void RecordCrossChainData()
        {
            Assert(IsMiner(),"Not authorized to do this.");
            int i = 0;
            while (i++ < 32)
            {
                var targetHeight = State.RecordedBlockHeight.Value;
                var blk = Context.GetBlockByHeight(targetHeight);
                if (blk == null)
                    return;
                var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(blk.Body.TransactionList.Last().Params);
                if (blk.Header.BlockExtraData.SideChainTransactionsRoot != null)
                {
                    var sideChainBlockData = crossChainBlockData.SideChainBlockData;
                    if (sideChainBlockData.Count > 0)
                    {
                        var calculatedRoot = IndexSideChainBlockInfo(sideChainBlockData.ToArray(),
                            Address.FromPublicKey(blk.Header.P.ToByteArray()));
                        Assert(calculatedRoot.Equals(blk.Header.BlockExtraData.SideChainTransactionsRoot),
                            "Incorrect side chain transaction root."); // this should not happen.
                    }
                }

                if (crossChainBlockData.ParentChainBlockData.Count > 0)
                    IndexParentChainBlockInfo(crossChainBlockData.ParentChainBlockData.ToArray());
                State.RecordedBlockHeight.Value = targetHeight + 1;
            }
        }

        /// <summary>
        /// Index parent chain blocks.
        /// </summary>
        /// <param name="parentChainBlockData"></param>
        private void IndexParentChainBlockInfo(ParentChainBlockData[] parentChainBlockData)
        {
            // only miner can do this.
            //Api.IsMiner("Not authorized to do this.");
            Assert(parentChainBlockData.Length <= GlobalConfig.MaximalCountForIndexingParentChainBlock,
                "Beyond maximal capacity for once indexing.");
            foreach (var blockInfo in parentChainBlockData)
            {
                ulong parentChainHeight = blockInfo.Height;
                var currentHeight = State.CurrentParentChainHeight.Value;
                var target = currentHeight != 0 ? currentHeight + 1 : GlobalConfig.GenesisBlockHeight;
                Assert(target == parentChainHeight,
                    $"Parent chain block info at height {target} is needed, not {parentChainHeight}");

                Console.WriteLine("ParentChainBlockData.Height is correct."); // Todo: only for debug

                var parentInfo = State.ParentChainBlockInfo[parentChainHeight];
                Assert(parentInfo.IsEmpty(),
                    $"Already written parent chain block info at height {parentChainHeight}");
                Console.WriteLine("Writing ParentChainBlockData..");
                foreach (var indexedBlockInfo in blockInfo.IndexedMerklePath)
                {
                    BindParentChainHeight(indexedBlockInfo.Key, parentChainHeight);
                    AddIndexedTxRootMerklePathInParentChain(indexedBlockInfo.Key, indexedBlockInfo.Value);
                }

                State.ParentChainBlockInfo[parentChainHeight] = blockInfo;
                State.CurrentParentChainHeight.Value = parentChainHeight;

                Console.WriteLine($"WriteParentChainBlockInfo success at {parentChainHeight}"); // Todo: only for debug
            }
        }

        /// <summary>
        /// Index side chain block(s).
        /// </summary>
        /// <param name="sideChainBlockData"></param>
        /// <param name="miner">Miner indexing cross chain data.</param>
        /// <returns>Root of merkle tree created from side chain txn roots.</returns>
        private Hash IndexSideChainBlockInfo(SideChainBlockData[] sideChainBlockData, Address miner)
        {
            // only miner can do this.
//            Api.IsMiner("Not authorized to do this.");
//            Api.Assert(sideChainBlockData.Length > 0, "Empty side chain block information.");
            var binaryMerkleTree = new BinaryMerkleTree();
            var currentHeight = Context.CurrentHeight;
            var height = currentHeight + 1;
            var result = State.IndexedSideChainBlockInfoResult[height];
            Assert(result.IsEmpty()); // This should not happen.

            var indexedSideChainBlockInfoResult = new IndexedSideChainBlockDataResult
            {
                Height = height,
                Miner = miner
            };
            foreach (var blockInfo in sideChainBlockData)
            {
                //Console.WriteLine("Side chain height: {0}", blockInfo.Height);
                ulong sideChainHeight = blockInfo.Height;
                Hash chainId = Hash.LoadByteArray(blockInfo.ChainId.DumpByteArray());
                var info = State.SideChainInfos[chainId];
                if (info.IsEmpty() || info.SideChainStatus != SideChainStatus.Active)
                    continue;
                var currentSideChainHeight = State.SideChainHeight[chainId];
                var target = currentSideChainHeight != 0 ? currentSideChainHeight + 1 : GlobalConfig.GenesisBlockHeight;
                if (target != sideChainHeight)
                    continue;

                // indexing fee
                var indexingPrice = info.IndexingPrice;
                var lockedToken = State.IndexingBalance[chainId];
                // locked token not enough 
                if (lockedToken < indexingPrice)
                {
                    info.SideChainStatus = SideChainStatus.InsufficientBalance;
                    State.SideChainInfos[chainId] = info;
                    continue;
                }

                State.IndexingBalance[chainId] = lockedToken - indexingPrice;
                State.TokenContract.Unlock(miner, indexingPrice);

                State.SideChainHeight[chainId] = target;
                binaryMerkleTree.AddNode(blockInfo.TransactionMKRoot);
                indexedSideChainBlockInfoResult.SideChainBlockData.Add(blockInfo);
                // Todo: only for debug
                Console.WriteLine($"Side chain block info at {target}");
            }

            State.IndexedSideChainBlockInfoResult[height] = indexedSideChainBlockInfoResult;

            // calculate merkle tree for side chain txn roots
            binaryMerkleTree.ComputeRootHash();
            return binaryMerkleTree.Root;
        }


        /// <summary>
        /// Cross chain txn verification.
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="path"></param>
        /// <param name="parentChainHeight"></param>
        /// <returns></returns>
        public bool VerifyTransaction(Hash tx, MerklePath path, ulong parentChainHeight)
        {
            var key = new UInt64Value {Value = parentChainHeight};
            var parentChainBlockInfo = State.ParentChainBlockInfo[parentChainHeight];
            Assert(parentChainBlockInfo.IsNotEmpty(),
                $"Parent chain block at height {parentChainHeight} is not recorded.");
            var rootCalculated = path.ComputeRootWith(tx);
            var parentRoot = parentChainBlockInfo.Root.SideChainTransactionsRoot;
            //Api.Assert((parentRoot??Hash.Zero).Equals(rootCalculated), "Transaction verification Failed");
            return (parentRoot ?? Hash.Zero).Equals(rootCalculated);
        }

        #endregion Cross chain actions

        #region Private actions

        /// <summary>
        /// Bind parent chain height together with self height.
        /// </summary>
        /// <param name="childHeight"></param>
        /// <param name="parentHeight"></param>
        private void BindParentChainHeight(ulong childHeight, ulong parentHeight)
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
        private void AddIndexedTxRootMerklePathInParentChain(ulong height, MerklePath path)
        {
            var existing = State.TxRootMerklePathInParentChain[height];
            Assert(existing.IsEmpty(),
                $"Merkle path already bound at height {height}.");
            State.TxRootMerklePathInParentChain[height] = path;
        }

        private void LockTokenAndResource(SideChainInfo sideChainInfo)
        {
            //Api.Assert(request.Proposer.Equals(Api.GetFromAddress()), "Unable to lock token or resource.");

            // update locked token balance
            State.TokenContract.Lock(Context.Sender, sideChainInfo.LockedTokenAmount);
            var chainId = sideChainInfo.ChainId;
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
            var chainId = sideChainInfo.ChainId;
            var balance = State.IndexingBalance[chainId];
            if (balance != 0)
                State.TokenContract.Unlock(sideChainInfo.Proposer, balance);
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