using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Types.Proposal;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Crypto.Prng.Drbg;
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

        private readonly Map<Hash, ChainCreationRequest> _requestInfo =
            new Map<Hash, ChainCreationRequest>(FieldNames.NewChainCreationRequest);

        private readonly MapToUInt64<Address> _indexingBalance = new MapToUInt64<Address>(FieldNames.IndexingBalance);

        private static string CreateSideChainMethodName { get; } = "CreateSideChain";

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
            Api.Assert(info.Status != (SideChainStatus) 3, "Disposed side chain.");
            return info.LockedToken;
        }
        
        public byte[] LockedAddress(Hash chainId)
        {
            Api.Assert(!_sideChainInfos[chainId].Equals(new SideChainInfo()), "Not existed side chain.");
            var info = _sideChainInfos[chainId];
            Api.Assert(info.Status != (SideChainStatus) 3, "Disposed side chain.");
            return info.LockedAddress.DumpByteArray();
        }

        #region Side chain lifetime actions

        public byte[] ReuqestChainCreation(ChainCreationRequest request)
        {
            Api.Assert(request.Proposer != null && Api.GetFromAddress().Equals(request.Proposer),
                "Invalid chain creation request.");
            Hash requestHash = request.GetHash();
            Api.Assert(_requestInfo[requestHash].Equals(new ChainCreationRequest()),
                "Chain creation request already exists.");
            foreach (var resourceBalance in request.ResourceBalances)
            {
                var balance = Api.GetResourceBalance(request.Proposer, resourceBalance.Type);
                Api.Assert(balance >= resourceBalance.Amount, "Not enough resource.");
            }
            
            Api.Assert(Api.GetTokenBalance(request.Proposer) >= request.LockedTokenAmount, "Not enough balance.");
            
            // Todo: temp chainId calculation method
            Hash chainId = Hash.Generate();
            // side chain creation proposal
            Api.Propose("ChainCreation", RequestChainCreationWaitingPeriod, CreateSideChainMethodName, chainId, requestHash);
            
            _requestInfo.SetValue(requestHash, request);
            return requestHash.DumpByteArray();
        }

        public byte[] CreateSideChain(Hash chainId, Hash requestHash)
        {
            // side chain creation should be triggered by multi sig txn from system address.
            Api.CheckAuthority(Api.Genesis);
            var request = _requestInfo[requestHash];
            Api.Assert(!request.Equals(new ChainCreationRequest()), "Side chain creation request not found.");
            Api.Assert(_sideChainInfos[chainId].Equals(new SideChainInfo()), "Side chain already created."); //This should not happen. 
            
            // lock token and resource
            LockTokenAndResource(request);

            ulong serialNumber = _sideChainSerialNumber.Increment().Value;
            var info = new SideChainInfo
            {
                ChainId = chainId,
                SerialNumer = serialNumber,
                Status = SideChainStatus.Active,
                LockedAddress = request.Proposer,
                CreationHeight = Api.GetCurrentHeight() + 1
            };
            info.ResourceBalances.AddRange(request.ResourceBalances);          
            _sideChainInfos[chainId] = info;
            
            // remove request
            // Todo: null or new object?
            _requestInfo[requestHash] = new ChainCreationRequest();
            
            // fire event
            new SideChainCreationRequested
            {
                ChainId = chainId,
                Creator = Api.GetTransaction().From
            }.Fire();
            return chainId.DumpByteArray();
        }

        public void DisposeSideChain(Hash chainId)
        {            
            // side chain disposal should be triggered by multi sig txn from system address.
            Api.CheckAuthority(Api.Genesis);
            Api.Assert(!_sideChainInfos[chainId].Equals(new SideChainInfo()), "Not existed side chain.");
            // TODO: Only privileged account can trigger this method
            var info = _sideChainInfos[chainId];
            Api.Assert(info.Status < SideChainStatus.Terminated, "Side chain already terminated.");
            WithdrawTokenAndResource(info);
            info.Status = SideChainStatus.Terminated;
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
            return (int) info.Status;
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
        
        private void LockTokenAndResource(ChainCreationRequest request)
        {
            //Api.Assert(request.Proposer.Equals(Api.GetFromAddress()), "Unable to lock token or resource.");
            
            // update locked token balance
            _indexingBalance[request.Proposer] = _indexingBalance[request.Proposer].Add(request.LockedTokenAmount);
            Api.LockToken(request.LockedTokenAmount);
            
            // lock 
            foreach (var resourceBalance in request.ResourceBalances)
            {
                Api.LockResource(resourceBalance.Amount, resourceBalance.Type);
            }
        }
        
        private void WithdrawTokenAndResource(SideChainInfo sideChainInfo)
        {
            //Api.Assert(sideChainInfo.LockedAddress.Equals(Api.GetFromAddress()), "Unable to withdraw token or resource.");
            // withdraw token
            var balance = _indexingBalance[sideChainInfo.LockedAddress];
            Api.WithdrawToken(balance);
            _indexingBalance[sideChainInfo.LockedAddress] = 0;
            
            // unlock resource 
            foreach (var resourceBalance in sideChainInfo.ResourceBalances)
            {
                Api.WithdrawResource(resourceBalance.Amount, resourceBalance.Type);
            }
        }

        #endregion
        
    }
}