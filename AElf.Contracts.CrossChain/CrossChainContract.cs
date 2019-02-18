﻿using System;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.CrossChain
{
    
    #region Field Names

    public static class FieldNames
    {
        public static readonly string SideChainSerialNumber = "__SideChainSerialNumber__";
        public static readonly string SideChainInfos = "__SideChainInfos__";
        public static readonly string ParentChainBlockInfo = GlobalConfig.AElfParentChainBlockInfo;
        public static readonly string AElfBoundParentChainHeight = GlobalConfig.AElfBoundParentChainHeight;
        public static readonly string TxRootMerklePathInParentChain = GlobalConfig.AElfTxRootMerklePathInParentChain;
        public static readonly string CurrentParentChainHeight = GlobalConfig.AElfCurrentParentChainHeight;
        public static readonly string IndexingBalance = "__IndexingBalance__";
        public static readonly string SideChainHeight = GlobalConfig.AElfCurrentSideChainHeight;
        public static readonly string IndexedSideChainBlockInfoResult = GlobalConfig.IndexedSideChainBlockInfoResult;
    }

    #endregion Field Names

    #region Events

    public class SideChainCreationRequested : Event
    {
        public Address Creator;
        public Hash ChainId;
    }

    public class SideChainCreationRequestApproved : Event
    {
        public SideChainInfo Info;
    }

    public class SideChainDisposal : Event
    {
        public Hash chainId;
    }

    #endregion Events

    #region Customized Field Types

    internal class SideChainSerialNumber : UInt64Field
    {
        internal static SideChainSerialNumber Instance { get; } = new SideChainSerialNumber();

        private SideChainSerialNumber() : this(FieldNames.SideChainSerialNumber)
        {
        }

        private SideChainSerialNumber(string name) : base(name)
        {
        }

        private ulong _value;

        public ulong Value
        {
            get
            {
                if (_value == 0)
                {
                    _value = GetValue();
                }

                return _value;
            }
            private set { _value = value; }
        }

        public SideChainSerialNumber Increment()
        {
            this.Value = this.Value + 1;
            SetValue(this.Value);
            return this;
        }
    }

    #endregion Customized Field Types

   
    public class CrossChainContract : CSharpSmartContract
    {
        #region Fields

        private readonly SideChainSerialNumber _sideChainSerialNumber = SideChainSerialNumber.Instance;

        #region side chain
        private readonly Map<Hash, SideChainInfo> _sideChainInfos =
            new Map<Hash, SideChainInfo>(FieldNames.SideChainInfos);
        private readonly MapToUInt64<Hash> _sideChainHeight = new MapToUInt64<Hash>(FieldNames.SideChainHeight);
        private readonly Map<UInt64Value, IndexedSideChainBlockDataResult> _indexedSideChainBlockInfoResult =
            new Map<UInt64Value, IndexedSideChainBlockDataResult>(FieldNames.IndexedSideChainBlockInfoResult);
        private readonly MapToUInt64<Hash> _indexingBalance = new MapToUInt64<Hash>(FieldNames.IndexingBalance);
        #endregion

        #region parent chain 
        private readonly Map<UInt64Value, ParentChainBlockData> _parentChainBlockInfo =
            new Map<UInt64Value, ParentChainBlockData>(FieldNames.ParentChainBlockInfo);
        
        // record self height 
        private readonly MapToUInt64<UInt64Value> _childHeightToParentChainHeight =
            new MapToUInt64<UInt64Value>(FieldNames.AElfBoundParentChainHeight);

        private readonly Map<UInt64Value, MerklePath> _txRootMerklePathInParentChain =
            new Map<UInt64Value, MerklePath>(FieldNames.TxRootMerklePathInParentChain);

        private readonly UInt64Field _currentParentChainHeight = new UInt64Field(FieldNames.CurrentParentChainHeight);
        private readonly UInt64Field _recordedBlockHeight = new UInt64Field("RecordedBlockHeight");
        
        #endregion
        
        private static string CreateSideChainMethodName { get; } = "CreateSideChain";
        private static string DisposeSideChainMethodName { get; } = "DisposeSideChain";
        #endregion Fields

        private double RequestChainCreationWaitingPeriod { get; } = 24 * 60 * 60;

        [View]
        public ulong CurrentSideChainSerialNumber()
        {
            return _sideChainSerialNumber.Value;
        }

        public ulong LockedToken(string chainId)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            Api.Assert(_sideChainInfos.TryGet(chainIdHash, out _), "Not existed side chain.");
            var info = _sideChainInfos[chainIdHash];
            Api.Assert(info.SideChainStatus != (SideChainStatus) 3, "Disposed side chain.");
            return info.LockedTokenAmount;
        }
        
        public byte[] LockedAddress(string chainId)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            Api.Assert(_sideChainInfos.TryGet(chainIdHash, out _), "Not existed side chain.");
            var info = _sideChainInfos[chainIdHash];
            Api.Assert(info.SideChainStatus != (SideChainStatus) 3, "Disposed side chain.");
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
            Api.Assert(
                request.SideChainStatus == SideChainStatus.Apply && request.Proposer != null &&
                Api.GetFromAddress().Equals(request.Proposer), "Invalid chain creation request.");

            var serialNumber = _sideChainSerialNumber.Increment().Value;
            int chainId = ChainHelpers.GetChainId(serialNumber);
            Api.Assert(!_sideChainInfos.TryGet(chainId, out _),"Chain creation request already exists.");

            // lock token and resource
            request.ChainId = chainId;
            LockTokenAndResource(request);

            // side chain creation proposal
            Hash hash = Api.Propose("ChainCreation", RequestChainCreationWaitingPeriod, Api.Genesis,
                Api.GetContractAddress(), CreateSideChainMethodName, chainId.DumpBase58());
            request.SideChainStatus = SideChainStatus.Review;
            request.ProposalHash = hash;
            _sideChainInfos.SetValue(chainId, request);
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
            
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address. 
            Api.Assert(
                _sideChainInfos.TryGet(chainIdHash, out var sideChainInfo) &&
                sideChainInfo.SideChainStatus == SideChainStatus.Review,
                "Side chain creation request not found.");
            
            Api.Assert(Api.GetFromAddress().Equals(sideChainInfo.Proposer), "Not authorized to withdraw request.");
            UnlockTokenAndResource(sideChainInfo);
            sideChainInfo.SideChainStatus = SideChainStatus.Terminated;
            _sideChainInfos[chainIdHash] = sideChainInfo;
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
            Api.CheckAuthority(Api.Genesis);
            
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address. 
            Api.Assert(
                _sideChainInfos.TryGet(chainIdHash, out var request) &&
                request.SideChainStatus == SideChainStatus.Review, "Side chain creation request not found.");

            request.SideChainStatus = SideChainStatus.Active;          
            _sideChainInfos[chainIdHash] = request;
            
            // fire event
            new SideChainCreationRequested
            {
                ChainId = chainIdHash,
                Creator = Api.GetFromAddress()
            }.Fire();
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
            Api.Assert(
                _sideChainInfos.TryGet(chainIdHash, out var sideChainInfo) &&
                (sideChainInfo.SideChainStatus == SideChainStatus.Active ||
                 sideChainInfo.SideChainStatus == SideChainStatus.InsufficientBalance),
                "Side chain not found or not able to be recharged.");
            _indexingBalance[chainIdHash] = _indexingBalance[chainIdHash] + amount;
            if (_indexingBalance[chainIdHash] > sideChainInfo.IndexingPrice)
            {
                sideChainInfo.SideChainStatus = SideChainStatus.Active;
                _sideChainInfos[chainIdHash] = sideChainInfo;
            }
            Api.LockToken(amount);
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
            Api.Assert(
                _sideChainInfos.TryGet(chainIdHash, out var request) &&
                request.SideChainStatus == SideChainStatus.Active,"Side chain not found");
            
            Api.Assert(Api.GetFromAddress().Equals(request.Proposer), "Not authorized to dispose.");

            // side chain disposal
            Hash proposalHash = Api.Propose("DisposeSideChain", RequestChainCreationWaitingPeriod, Api.Genesis,
                Api.GetContractAddress(), DisposeSideChainMethodName, chainId);
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
            Api.CheckAuthority(Api.Genesis);
            Api.Assert(_sideChainInfos.TryGet(chainIdHash, out var info), "Not existed side chain.");
            
            // TODO: Only privileged account can trigger this method
            Api.Assert(info.SideChainStatus == SideChainStatus.Active, "Unable to dispose this side chain.");
            
            UnlockTokenAndResource(info);
            info.SideChainStatus = SideChainStatus.Terminated;
            _sideChainInfos[chainIdHash] = info;
            new SideChainDisposal    
            {
                chainId = chainIdHash
            }.Fire();
        }
        
        [View]
        public int GetChainStatus(string chainId)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            Api.Assert(_sideChainInfos.TryGet(chainIdHash, out var info), "Not existed side chain.");
            return (int) info.SideChainStatus;
        }

        [View]
        public ulong LockedBalance(string chainId)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            Api.Assert(_sideChainInfos.TryGet(chainIdHash, out var sideChainInfo), "Not existed side chain.");
            Api.Equal(Api.GetFromAddress(), sideChainInfo.Proposer, "Unable to check balance.");
            return _indexingBalance[chainIdHash];
        }
        
        #endregion Side chain lifetime actions

        #region Cross chain actions

        [Fee(0)]
        public void RecordCrossChainData()
        {
            Api.IsMiner("Not authorized to do this.");
            int i = 0;
            while (i++ < 32)
            {
                var targetHeight = _recordedBlockHeight.GetValue();
                var blk = Api.GetBlockByHeight(targetHeight);
                if(blk == null)
                    return;
                var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(blk.Body.TransactionList.Last().Params);
                if (blk.Header.BlockExtraData.SideChainTransactionsRoot != null)
                {
                    var sideChainBlockData = crossChainBlockData.SideChainBlockData;
                    if (sideChainBlockData.Count > 0)
                    {
                        var calculatedRoot = IndexSideChainBlockInfo(sideChainBlockData.ToArray(),
                            Address.FromPublicKey(blk.Header.P.ToByteArray()));
                        Api.Assert(calculatedRoot.Equals(blk.Header.BlockExtraData.SideChainTransactionsRoot), 
                            "Incorrect side chain transaction root."); // this should not happen.
                    }
                }
                
                if (crossChainBlockData.ParentChainBlockData.Count > 0)
                    IndexParentChainBlockInfo(crossChainBlockData.ParentChainBlockData.ToArray());
                _recordedBlockHeight.SetValue(targetHeight + 1);
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
            Api.Assert(parentChainBlockData.Length <= GlobalConfig.MaximalCountForIndexingParentChainBlock,
                "Beyond maximal capacity for once indexing.");
            foreach (var blockInfo in parentChainBlockData)
            {
                ulong parentChainHeight = blockInfo.Height;
                var currentHeight = _currentParentChainHeight.GetValue();
                var target = currentHeight != 0 ? currentHeight + 1: GlobalConfig.GenesisBlockHeight;
                Api.Assert(target == parentChainHeight,
                    $"Parent chain block info at height {target} is needed, not {parentChainHeight}");
                
                Console.WriteLine("ParentChainBlockData.Height is correct."); // Todo: only for debug
            
                var key = new UInt64Value {Value = parentChainHeight};
                Api.Assert(!_parentChainBlockInfo.TryGet(key, out _),
                    $"Already written parent chain block info at height {parentChainHeight}");
                Console.WriteLine("Writing ParentChainBlockData..");
                foreach (var indexedBlockInfo in blockInfo.IndexedMerklePath)
                {
                    BindParentChainHeight(indexedBlockInfo.Key, parentChainHeight);
                    AddIndexedTxRootMerklePathInParentChain(indexedBlockInfo.Key, indexedBlockInfo.Value);
                }
                _parentChainBlockInfo[key] = blockInfo;
                _currentParentChainHeight.SetValue(parentChainHeight);
            
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
            var currentHeight = Api.GetCurrentHeight();
            var height = currentHeight + 1;
            var wrappedHeight = new UInt64Value {Value = height};
            Api.Assert(!_indexedSideChainBlockInfoResult.TryGet(wrappedHeight, out _)); // This should not happen.
            
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
                if(!_sideChainInfos.TryGet(chainId, out var info) || info.SideChainStatus != SideChainStatus.Active)
                    continue;
                var currentSideChainHeight = _sideChainHeight.GetValue(chainId);
                var target = currentSideChainHeight != 0 ? currentSideChainHeight + 1: GlobalConfig.GenesisBlockHeight;
                if(target != sideChainHeight)
                    continue;
                
                // indexing fee
                var indexingPrice = info.IndexingPrice;
                var lockedToken = _indexingBalance.GetValue(chainId);
                // locked token not enough 
                if (lockedToken < indexingPrice)
                {
                    info.SideChainStatus = SideChainStatus.InsufficientBalance;
                    _sideChainInfos[chainId] = info;
                    continue;
                }
                _indexingBalance[chainId] = lockedToken - indexingPrice;
                Api.UnlockToken(miner, indexingPrice);
                
                _sideChainHeight[chainId] = target;
                binaryMerkleTree.AddNode(blockInfo.TransactionMKRoot);
                indexedSideChainBlockInfoResult.SideChainBlockData.Add(blockInfo);
                // Todo: only for debug
                Console.WriteLine($"Side chain block info at {target}");
            }

            _indexedSideChainBlockInfoResult[wrappedHeight] = indexedSideChainBlockInfoResult;
            
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
            Api.Assert(_parentChainBlockInfo.TryGet(key, out var parentChainBlockInfo),
                $"Parent chain block at height {parentChainHeight} is not recorded.");
            var rootCalculated =  path.ComputeRootWith(tx);
            var parentRoot = parentChainBlockInfo.Root.SideChainTransactionsRoot;
            //Api.Assert((parentRoot??Hash.Zero).Equals(rootCalculated), "Transaction verification Failed");
            return (parentRoot??Hash.Zero).Equals(rootCalculated);
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
            var key = new UInt64Value {Value = childHeight};
            Api.Assert(_childHeightToParentChainHeight[key] == 0,
                $"Already bound at height {childHeight} with parent chain");
            _childHeightToParentChainHeight[key] = parentHeight;
        }

        /// <summary>
        /// Record merkle path of self chain block, which is from parent chain. 
        /// </summary>
        /// <param name="height"></param>
        /// <param name="path"></param>
        private void AddIndexedTxRootMerklePathInParentChain(ulong height, MerklePath path)
        {
            var key = new UInt64Value {Value = height};
            Api.Assert(!_txRootMerklePathInParentChain.TryGet(key, out _), 
                $"Merkle path already bound at height {height}.");
            _txRootMerklePathInParentChain[key] = path;
        }
        
        private void LockTokenAndResource(SideChainInfo sideChainInfo)
        {
            //Api.Assert(request.Proposer.Equals(Api.GetFromAddress()), "Unable to lock token or resource.");
            
            // update locked token balance
            Api.LockToken(sideChainInfo.LockedTokenAmount);
            var chainId = sideChainInfo.ChainId;
            _indexingBalance[chainId] = sideChainInfo.LockedTokenAmount;
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
            var balance = _indexingBalance[chainId];
            if(balance != 0 )
                Api.UnlockToken(sideChainInfo.Proposer, balance);
            _indexingBalance[chainId] = 0;

            // unlock resource 
            /*foreach (var resourceBalance in sideChainInfo.ResourceBalances)
            {
                Api.UnlockResource(resourceBalance.Amount, resourceBalance.Type);
            }*/
        }
        
        #endregion
        
    }
}