using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.CrossChain
{
    
    #region Field Names

    public static class FieldNames
    {
        public static readonly string SideChainSerialNumber = "__SideChainSerialNumber__";
        public static readonly string SideChainInfos = "__SideChainInfos__";
        public static readonly string ParentChainBlockInfo = GlobalConfig.AElfParentChainBlockInfo;
        //public static readonly string SideChainBlockInfo = GlobalConfig.AElfSideChainBlockInfo;
        public static readonly string AElfBoundParentChainHeight = GlobalConfig.AElfBoundParentChainHeight;
        public static readonly string TxRootMerklePathInParentChain = GlobalConfig.AElfTxRootMerklePathInParentChain;
        public static readonly string CurrentParentChainHeight = GlobalConfig.AElfCurrentParentChainHeight;
        public static readonly string NewChainCreationRequest = "_NewChainCreationRequest_";
        public static readonly string IndexingBalance = "__IndexingBalance__";
        public static readonly string SideChainHeight = GlobalConfig.AElfCurrentSideChainHeight;
        public static readonly string BinaryMerkleTreeForSideChainTxnRoot = GlobalConfig.AElfBinaryMerkleTreeForSideChainTxnRoot;
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
        private readonly Map<UInt64Value, BinaryMerkleTree> _sideChainTxnRootMerklePath =
            new Map<UInt64Value, BinaryMerkleTree>(FieldNames.BinaryMerkleTreeForSideChainTxnRoot);
        private readonly MapToUInt64<Hash> _indexingBalance = new MapToUInt64<Hash>(FieldNames.IndexingBalance);
        #endregion

        #region parent chain 
        private readonly Map<UInt64Value, ParentChainBlockInfo> _parentChainBlockInfo =
            new Map<UInt64Value, ParentChainBlockInfo>(FieldNames.ParentChainBlockInfo);
        
        // record self height 
        private readonly MapToUInt64<UInt64Value> _childHeightToParentChainHeight =
            new MapToUInt64<UInt64Value>(FieldNames.AElfBoundParentChainHeight);

        private readonly Map<UInt64Value, MerklePath> _txRootMerklePathInParentChain =
            new Map<UInt64Value, MerklePath>(FieldNames.TxRootMerklePathInParentChain);

        private readonly UInt64Field _currentParentChainHeight = new UInt64Field(FieldNames.CurrentParentChainHeight);
        
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
            Api.Assert(!_sideChainInfos[chainIdHash].Equals(new SideChainInfo()), "Not existed side chain.");
            var info = _sideChainInfos[chainIdHash];
            Api.Assert(info.SideChainStatus != (SideChainStatus) 3, "Disposed side chain.");
            return info.LockedTokenAmount;
        }
        
        public byte[] LockedAddress(string chainId)
        {
            var chainIdHash = Hash.LoadBase58(chainId);
            Api.Assert(!_sideChainInfos[chainIdHash].Equals(new SideChainInfo()), "Not existed side chain.");
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
        public byte[] ReuqestChainCreation(SideChainInfo request)
        {
            // no need to check authority since invoked in transaction from normal address
            Api.Assert(
                request.SideChainStatus == SideChainStatus.Apply && request.Proposer != null &&
                Api.GetFromAddress().Equals(request.Proposer), "Invalid chain creation request.");
            
            var serialNumber = _sideChainSerialNumber.Increment().Value;
            var raw = ChainHelpers.GetChainId(serialNumber);
            var chainId = Hash.LoadByteArray(raw);
            
            Api.Assert(_sideChainInfos[chainId].Equals(new SideChainInfo()),
                "Chain creation request already exists.");
            
            // lock token and resource
            request.ChainId = chainId;
            Api.Assert(_indexingBalance[chainId] == 0, "Chain Id already used"); // This should not happen.  
            LockTokenAndResource(request);

            // side chain creation proposal
            Hash hash = Api.Propose("ChainCreation", RequestChainCreationWaitingPeriod, Api.GetContractAddress(),
                CreateSideChainMethodName, raw.ToPlainBase58());
            request.SideChainStatus = SideChainStatus.Review;
            _sideChainInfos.SetValue(chainId, request);
            return hash.DumpByteArray();
        }

        public void WithdrawRequest(string chainId)
        {
            // no need to check authority since invoked in transaction from normal address
            var chainIdHash = Hash.LoadBase58(chainId);
            var sideChainInfo = _sideChainInfos[chainIdHash];
            
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address. 
            Api.Assert(!sideChainInfo.Equals(new SideChainInfo()) && sideChainInfo.SideChainStatus == SideChainStatus.Review,
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
        public string CreateSideChain(string chainId)
        {
            // side chain creation should be triggered by multi sig txn from system address.
            var chainIdHash = Hash.LoadBase58(chainId);
            Api.CheckAuthority(Api.Genesis);
            var request = _sideChainInfos[chainIdHash];
            
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address. 
            Api.Assert(!request.Equals(new SideChainInfo()) && request.SideChainStatus == SideChainStatus.Review,
                "Side chain creation request not found.");

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
        /// Request form normal address to dispose side chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public byte[] RequestChainDisposal(string chainId)
        {
            // no need to check authority since invoked in transaction from normal address
            var chainIdHash = Hash.LoadBase58(chainId);
            var request = _sideChainInfos[chainIdHash];
            Api.Assert(!request.Equals(new SideChainInfo()) && request.SideChainStatus == SideChainStatus.Active,
                "Side chain not found");
            Api.Assert(Api.GetFromAddress().Equals(request.Proposer), "Not authorized to dispose.");
                        
            // side chain disposal
            Hash proposalHash = Api.Propose("DisposeSideChain", RequestChainCreationWaitingPeriod, Api.GetContractAddress(),
                DisposeSideChainMethodName, chainId);
            return proposalHash.DumpByteArray();
        }
        
        /// <summary>
        /// Dispose side chain. It is a proposal result from system address. 
        /// </summary>
        /// <param name="chainId"></param>
        public void DisposeSideChain(string chainId)
        {
            // side chain disposal should be triggered by multi sig txn from system address.
            var chainIdHash = Hash.LoadBase58(chainId);
            Api.CheckAuthority(Api.Genesis);
            Api.Assert(!_sideChainInfos[chainIdHash].Equals(new SideChainInfo()), "Not existed side chain.");
            // TODO: Only privileged account can trigger this method
            var info = _sideChainInfos[chainIdHash];
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
            Api.Assert(!_sideChainInfos[chainIdHash].Equals(new SideChainInfo()), "Not existed side chain.");
            var info = _sideChainInfos[chainIdHash];
            return (int) info.SideChainStatus;
        }
        
        #endregion Side chain lifetime actions

        #region Cross chain actions
        /// <summary>
        /// Index parent chain blocks.
        /// </summary>
        /// <param name="parentChainBlockInfo"></param>
        public void IndexParentChainBlockInfo(ParentChainBlockInfo[] parentChainBlockInfo)
        {
            // only miner can do this.
            //Api.IsMiner("Not authorized to do this.");
            Api.Assert(parentChainBlockInfo.Length <= GlobalConfig.MaximalCountForIndexingParentChainBlock,
                "Beyond maximal capacity for once indexing.");
            foreach (var blockInfo in parentChainBlockInfo)
            {
                ulong parentChainHeight = blockInfo.Height;
                var currentHeight = _currentParentChainHeight.GetValue();
                var target = currentHeight != 0 ? currentHeight + 1: GlobalConfig.GenesisBlockHeight; 
                Api.Assert(target == parentChainHeight,
                    $"Parent chain block info at height {target} is needed, not {parentChainHeight}");
                
                Console.WriteLine("ParentChainBlockInfo.Height is correct."); // Todo: only for debug
            
                var key = new UInt64Value {Value = parentChainHeight};
                Api.Assert(_parentChainBlockInfo[key].Equals(new ParentChainBlockInfo()),
                    $"Already written parent chain block info at height {parentChainHeight}");
                Console.WriteLine("Writing ParentChainBlockInfo..");
                foreach (var indexedBlockInfo in blockInfo.IndexedBlockInfo)
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
        /// <param name="sideChainBlockInfo"></param>
        /// <returns>Root of merkle tree created from side chain txn roots.</returns>
        public byte[] IndexSideChainBlockInfo(SideChainBlockInfo[] sideChainBlockInfo)
        {
            // only miner can do this.
            //Api.IsMiner("Not authorized to do this.");
            Api.Assert(sideChainBlockInfo.Length > 0, "Empty side chain block information.");
            var binaryMerkleTree = new BinaryMerkleTree();
            //Console.WriteLine("Index side chain block..");
            foreach (var blockInfo in sideChainBlockInfo)
            {
                //Console.WriteLine("Side chain height: {0}", blockInfo.Height);
                ulong sideChainHeight = blockInfo.Height;
                Hash chainId = Hash.LoadByteArray(blockInfo.ChainId.DumpByteArray());
                // Api.NotEqual(new SideChainInfo(), _sideChainInfos[chainId], "Not registered side chain");
                // Api.Equal(SideChainStatus.Active, _sideChainInfos[chainId].SideChainStatus, "Side chain is not active.");
                var currentHeight = _sideChainHeight.GetValue(chainId);
                var target = currentHeight != 0 ? currentHeight + 1: GlobalConfig.GenesisBlockHeight;
                var chainIdBase58 = blockInfo.ChainId.DumpByteArray().ToPlainBase58();
                //Console.WriteLine("Side chain Id: {0}", chainIdBase58);
                Api.Equal(target, sideChainHeight,
                    $"Side chain block info at height {target} is needed from {chainIdBase58}, not {sideChainHeight}");
            
                _sideChainHeight[chainId] = target;
                // Todo: only for debug
                Console.WriteLine($"Side chain block info at {target}");
                
                // indexing fee
                var indexingPrice = _sideChainInfos[chainId].IndexingPrice; 
                Api.UnlockToken(Api.GetFromAddress(), indexingPrice);
                binaryMerkleTree.AddNode(blockInfo.TransactionMKRoot);
            }

            var height = Api.GetCurrentHeight() + 1;
            var wrappedHeight = new UInt64Value {Value = height};
            Api.Equal(new BinaryMerkleTree(), _sideChainTxnRootMerklePath[wrappedHeight],
                $"Already recorded BinaryMerkleTree at height {height}"); // this should not fail.
            
            // calculate merkle tree for side chain txn roots
            
            binaryMerkleTree.ComputeRootHash();
            _sideChainTxnRootMerklePath[wrappedHeight] = binaryMerkleTree;
            return binaryMerkleTree.Root.DumpByteArray();
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
            Api.Assert(!_parentChainBlockInfo[key].Equals(new ParentChainBlockInfo()),
                $"Parent chain block at height {parentChainHeight} is not recorded.");
            var rootCalculated =  path.ComputeRootWith(tx);
            var parentRoot = _parentChainBlockInfo[key]?.Root?.SideChainTransactionsRoot;
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
            Api.Assert(_txRootMerklePathInParentChain[key].Equals(new MerklePath()), 
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
            // withdraw token
            var chainId = Hash.LoadByteArray(sideChainInfo.ChainId.ToByteArray());
            var balance = _indexingBalance[chainId];
            if(balance != 0 )
                Api.UnlockToken(sideChainInfo.Proposer, balance);
            _indexingBalance[chainId] = 0;

            // unlock resource 
            /*foreach (var resourceBalance in sideChainInfo.ResourceBalances)
            {
                Api.WithdrawResource(resourceBalance.Amount, resourceBalance.Type);
            }*/
        }
        
        #endregion
        
    }
}