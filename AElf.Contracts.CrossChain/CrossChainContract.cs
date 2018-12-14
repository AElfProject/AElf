using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
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
        public static readonly string AElfBoundParentChainHeight = GlobalConfig.AElfBoundParentChainHeight;
        public static readonly string TxRootMerklePathInParentChain = GlobalConfig.AElfTxRootMerklePathInParentChain;
        public static readonly string CurrentParentChainHeight = GlobalConfig.AElfCurrentParentChainHeight;
        public static readonly string NewChainCreationRequest = "_NewChainCreationRequest_";
        public static readonly string IndexingBalance = "__IndexingBalance__";
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
        private readonly Map<Hash, SideChainInfo> _sideChainInfos =
            new Map<Hash, SideChainInfo>(FieldNames.SideChainInfos);

        private readonly Map<UInt64Value, ParentChainBlockInfo> _parentChainBlockInfo =
            new Map<UInt64Value, ParentChainBlockInfo>(FieldNames.ParentChainBlockInfo);
        
        // record self height 
        private readonly MapToUInt64<UInt64Value> _childHeightToParentChainHeight =
            new MapToUInt64<UInt64Value>(FieldNames.AElfBoundParentChainHeight);

        private readonly Map<UInt64Value, MerklePath> _txRootMerklePathInParentChain =
            new Map<UInt64Value, MerklePath>(FieldNames.TxRootMerklePathInParentChain);

        private readonly UInt64Field _currentParentChainHeight = new UInt64Field(FieldNames.CurrentParentChainHeight);

        private readonly MapToUInt64<Hash> _indexingBalance = new MapToUInt64<Hash>(FieldNames.IndexingBalance);

        private static string CreateSideChainMethodName { get; } = "CreateSideChain";
        private static string DisposeSideChainMethodName { get; } = "DisposeSideChain";
        #endregion Fields

        private double RequestChainCreationWaitingPeriod { get; } = 24 * 60 * 60;

        [View]
        public ulong CurrentSideChainSerialNumber()
        {
            return _sideChainSerialNumber.Value;
        }

        public ulong LockedToken(Hash chainId)
        {
            Api.Assert(!_sideChainInfos[chainId].Equals(new SideChainInfo()), "Not existed side chain.");
            var info = _sideChainInfos[chainId];
            Api.Assert(info.SideChainStatus != (SideChainStatus) 3, "Disposed side chain.");
            return info.LockedTokenAmount;
        }
        
        public byte[] LockedAddress(Hash chainId)
        {
            Api.Assert(!_sideChainInfos[chainId].Equals(new SideChainInfo()), "Not existed side chain.");
            var info = _sideChainInfos[chainId];
            Api.Assert(info.SideChainStatus != (SideChainStatus) 3, "Disposed side chain.");
            return info.Proposer.DumpByteArray();
        }

        #region Side chain lifetime actions

        public byte[] ReuqestChainCreation(SideChainInfo request)
        {
            Api.Assert(
                request.SideChainStatus == SideChainStatus.Apply && request.Proposer != null &&
                Api.GetFromAddress().Equals(request.Proposer), "Invalid chain creation request.");
            
            var serialNumber = _sideChainSerialNumber.Increment().Value;
            Hash chainId = ChainHelpers.GetChainId(serialNumber);
            Api.Assert(_sideChainInfos[chainId].Equals(new SideChainInfo()),
                "Chain creation request already exists.");
            
            // lock token and resource
            request.ChainId = chainId;
            Api.Assert(_indexingBalance[chainId] == 0, "Chain Id already used"); // This should not happen.  
            LockTokenAndResource(request);

            // side chain creation proposal
            Hash hash = Api.Propose("ChainCreation", RequestChainCreationWaitingPeriod, Api.GetContractAddress(),
                CreateSideChainMethodName, chainId);
            request.SideChainStatus = SideChainStatus.Review;
            _sideChainInfos.SetValue(chainId, request);
            return hash.DumpByteArray();
        }

        public void WithdrawRequest(Hash chainId)
        {
            var sideChainInfo = _sideChainInfos[chainId];
            
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address. 
            Api.Assert(!sideChainInfo.Equals(new SideChainInfo()) && sideChainInfo.SideChainStatus == SideChainStatus.Review,
                "Side chain creation request not found.");
            Api.Assert(Api.GetFromAddress().Equals(sideChainInfo.Proposer), "Not authorized to withdraw request.");
            WithdrawTokenAndResource(sideChainInfo);
            sideChainInfo.SideChainStatus = SideChainStatus.Terminated;
            _sideChainInfos[chainId] = sideChainInfo;
        }

        public byte[] CreateSideChain(Hash chainId)
        {
            // side chain creation should be triggered by multi sig txn from system address.
            Api.CheckAuthority(Api.Genesis);
            var request = _sideChainInfos[chainId];
            
            // todo: maybe expired time check is needed, but now it is assumed that creation only can be in a multi signatures transaction from genesis address. 
            Api.Assert(!request.Equals(new SideChainInfo()) && request.SideChainStatus == SideChainStatus.Review,
                "Side chain creation request not found.");

            request.SideChainStatus = SideChainStatus.Active;          
            _sideChainInfos[chainId] = request;
            
            // fire event
            new SideChainCreationRequested
            {
                ChainId = chainId,
                Creator = Api.GetFromAddress()
            }.Fire();
            return chainId.DumpByteArray();
        }

        
        public void ChargeForIndexing()
        {
            // Todo : more changes needed for indexing fee.    
        }

        public byte[] RequestChainDisposal(Hash chainId)
        {
            var request = _sideChainInfos[chainId];
            Api.Assert(!request.Equals(new SideChainInfo()) && request.SideChainStatus == SideChainStatus.Active,
                "Side chain not found");
            Api.Assert(Api.GetFromAddress().Equals(request.Proposer), "Not authorized to dispose.");
                        
            // side chain disposal
            Hash hash = Api.Propose("DisposeSideChain", RequestChainCreationWaitingPeriod, Api.GetContractAddress(),
                DisposeSideChainMethodName, chainId);
            return hash.DumpByteArray();
        }

        public void DisposeSideChain(Hash chainId)
        {            
            // side chain disposal should be triggered by multi sig txn from system address.
            Api.CheckAuthority(Api.Genesis);
            Api.Assert(!_sideChainInfos[chainId].Equals(new SideChainInfo()), "Not existed side chain.");
            // TODO: Only privileged account can trigger this method
            var info = _sideChainInfos[chainId];
            Api.Assert(info.SideChainStatus == SideChainStatus.Active, "Unable to dispose this side chain.");
            
            WithdrawTokenAndResource(info);
            info.SideChainStatus = SideChainStatus.Terminated;
            _sideChainInfos[chainId] = info;
            new SideChainDisposal    
            {
                chainId = chainId
            }.Fire();
        }
        
        public int GetChainStatus(Hash chainId)
        {
            Api.Assert(!_sideChainInfos[chainId].Equals(new SideChainInfo()), "Not existed side chain.");
            var info = _sideChainInfos[chainId];
            return (int) info.SideChainStatus;
        }
        
        #endregion Side chain lifetime actions

        
        #region Cross chain actions
        public void WriteParentChainBlockInfo(ParentChainBlockInfo parentChainBlockInfo)
        {
            ulong parentChainHeight = parentChainBlockInfo.Height;
            var currentHeight = _currentParentChainHeight.GetValue();
            var target = currentHeight != 0 ? currentHeight + 1: GlobalConfig.GenesisBlockHeight; 
            Api.Assert(target == parentChainHeight,
                $"Parent chain block info at height {target} is needed, not {parentChainHeight}");
            // Todo: only for debug
            Console.WriteLine("ParentChainBlockInfo.Height is correct.");
            
            var key = new UInt64Value {Value = parentChainHeight};
            Api.Assert(_parentChainBlockInfo[key].Equals(new ParentChainBlockInfo()),
                $"Already written parent chain block info at height {parentChainHeight}");
            Console.WriteLine("Writing ParentChainBlockInfo..");
            foreach (var _ in parentChainBlockInfo.IndexedBlockInfo)
            {
                BindParentChainHeight(_.Key, parentChainHeight);
                AddIndexedTxRootMerklePathInParentChain(_.Key, _.Value);
            }
            _parentChainBlockInfo[key] = parentChainBlockInfo;
            _currentParentChainHeight.SetValue(parentChainHeight);
            
            // Todo: only for debug
            Console.WriteLine($"WriteParentChainBlockInfo success at {parentChainHeight}");
        }

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

        private void BindParentChainHeight(ulong childHeight, ulong parentHeight)
        {
            var key = new UInt64Value {Value = childHeight};
            Api.Assert(_childHeightToParentChainHeight[key] == 0,
                $"Already bound at height {childHeight} with parent chain");
            _childHeightToParentChainHeight[key] = parentHeight;
        }

        private void AddIndexedTxRootMerklePathInParentChain(ulong height, MerklePath path)
        {
            var key = new UInt64Value {Value = height};
            Api.Assert(_txRootMerklePathInParentChain[key].Equals(new MerklePath()), 
                $"Merkle path already bound at height {height}.");
            _txRootMerklePathInParentChain[key] = path;
        }
        
        private void LockTokenAndResource(SideChainInfo request)
        {
            //Api.Assert(request.Proposer.Equals(Api.GetFromAddress()), "Unable to lock token or resource.");
            
            // update locked token balance
            Api.LockToken(request.LockedTokenAmount);
            _indexingBalance[request.ChainId] = request.LockedTokenAmount;

            // Todo: enable resource
            // lock 
            /*foreach (var resourceBalance in request.ResourceBalances)
            {
                Api.LockResource(resourceBalance.Amount, resourceBalance.Type);
            }*/
        }
        
        private void WithdrawTokenAndResource(SideChainInfo sideChainInfo)
        {
            //Api.Assert(sideChainInfo.LockedAddress.Equals(Api.GetFromAddress()), "Unable to withdraw token or resource.");
            // withdraw token
            var balance = _indexingBalance[sideChainInfo.ChainId];
            if(balance != 0 )
                Api.WithdrawToken(sideChainInfo.Proposer, balance);
            _indexingBalance[sideChainInfo.ChainId] = 0;

            // unlock resource 
            /*foreach (var resourceBalance in sideChainInfo.ResourceBalances)
            {
                Api.WithdrawResource(resourceBalance.Amount, resourceBalance.Type);
            }*/
        }
        
        #endregion
        
    }
}