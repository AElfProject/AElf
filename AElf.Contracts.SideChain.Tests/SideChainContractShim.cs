﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.SmartContract;
using AElf.Kernel;
using AElf.Kernel.Types;
using Google.Protobuf;
using AElf.Types.CSharp;
using Org.BouncyCastle.Asn1.Mozilla;

namespace AElf.Contracts.SideChain.Tests
{
    public class SideChainContractShim
    {
        private MockSetup _mock;
        public Address ContractAddres = Address.FromString("SideChainContract");
        public IExecutive _executive;
        public IExecutive Executive {
            get
            {
                _executive?.SetDataCache(new Dictionary<StatePath, StateCache>());
                return _executive;
            }
      
            set => _executive = value;
        }

        public ITransactionContext TransactionContext { get; private set; }

        public Address Sender { get; } = Address.FromString("Sender");

        public Address SideChainContractAddress { get; set; }

        public SideChainContractShim(MockSetup mock, Address sideChainContractAddress)
        {
            _mock = mock;
            SideChainContractAddress = sideChainContractAddress;
            Init();
        }

        private void Init()
        {
            var task = _mock.GetExecutiveAsync(SideChainContractAddress);
            task.Wait();
            Executive = task.Result;
        }

        #region ABI (Public) Methods

        #region View Only Methods

        public async Task<int?> GetChainStatus(int chainId)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetChainStatus",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(chainId))
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            await CommitChangesAsync(TransactionContext.Trace);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToInt32();
        }

        public async Task<ulong?> GetLockedToken(int chainId)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "LockedToken",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(chainId))
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            await CommitChangesAsync(TransactionContext.Trace);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64();
        }

        public async Task<byte[]> GetLockedAddress(int chainId)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "LockedAddress",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(chainId))
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            await CommitChangesAsync(TransactionContext.Trace);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBytes();
        }

        public async Task<ulong?> GetCurrentSideChainSerialNumber()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "CurrentSideChainSerialNumber"
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            await CommitChangesAsync(TransactionContext.Trace);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64();
        }

        #endregion View Only Methods


        #region Actions

        private async Task CommitChangesAsync(TransactionTrace trace)
        {
            await trace.SmartCommitChangesAsync(_mock.StateManager);
        }
        
        public async Task<byte[]> CreateSideChain(int chainId, Address lockedAddress, ulong lockedToken)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "CreateSideChain",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(chainId, lockedAddress, lockedToken))
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            await CommitChangesAsync(TransactionContext.Trace);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBytes();
        }


        public async Task ApproveSideChain(int chainId)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "ApproveSideChain",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(chainId))
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            await CommitChangesAsync(TransactionContext.Trace);
        }

        public async Task DisposeSideChain(int chainId)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "DisposeSideChain",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(chainId))
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            await CommitChangesAsync(TransactionContext.Trace);
        }

        public async Task WriteParentChainBLockInfo(ParentChainBlockData[] parentChainBlockData)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = ContractHelpers.IndexingParentChainMethodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(new object[]{parentChainBlockData}))
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            Console.WriteLine($"\r\n{TransactionContext.Trace}\r\n");
            await CommitChangesAsync(TransactionContext.Trace);
        }

        public async Task<bool?> VerifyTransaction(Hash txHash, MerklePath path, ulong height)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "VerifyTransaction",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(txHash, path, height))
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            await CommitChangesAsync(TransactionContext.Trace);
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBool();
        }

        public async Task GetMerklePath(ulong height)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = SideChainContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "WriteParentChainBlockInfo",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(height))
            };
            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            await Executive.SetTransactionContext(TransactionContext).Apply();
            await CommitChangesAsync(TransactionContext.Trace);
        }

        #endregion Actions

        #endregion ABI (Public) Methods
    }
}