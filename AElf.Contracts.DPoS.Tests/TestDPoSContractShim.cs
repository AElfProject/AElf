using System;
using System.Collections.Generic;
using System.IO;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;

namespace AElf.Contracts.DPoS.Tests
{
    public class TestDPoSContractShim
    {
        private DPoSMockSetup _mock;
        public Hash ContractAddres = Hash.Generate();
        public IExecutive Executive { get; set; }

        public byte[] Code
        {
            get
            {
                string filePath =
                    "../../../../AElf.Contracts.DPoS/bin/Debug/netstandard2.0/AElf.Contracts.DPoS.dll";
                byte[] code;
                using (var file = File.OpenRead(System.IO.Path.GetFullPath(filePath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        public TestDPoSContractShim(DPoSMockSetup mock)
        {
            _mock = mock;
            Initialize();
        }

        private void Initialize()
        {
            _mock.DeployContractAsync(Code, ContractAddres).Wait();
            var task = _mock.GetExecutiveAsync(ContractAddres);
            task.Wait();
            Executive = task.Result;
        }

        public BlockProducer SetBlockProducers(BlockProducer blockProducer)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "SetBlockProducers",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(blockProducer))
            };
            var tc = new TransactionContext
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToPbMessage<BlockProducer>();
        }
        
        public BlockProducer GetBlockProducers()
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetBlockProducers",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToPbMessage<BlockProducer>();
        }

        public DPoSInfo RandomizeInfoForFirstTwoRounds()
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "RandomizeInfoForFirstTwoRounds",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToPbMessage<DPoSInfo>();
        }

        // ReSharper disable once InconsistentNaming
        public string GetEBPOfCurrentRound()
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetCurrentEBP",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToString();
        }

        public string GenerateNextRoundOrder(string accountAddress)
        {
            var tx = new Transaction
            {
                From = AddressStringToHash(accountAddress),
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GenerateNextRoundOrder",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToString();
        }

        public Hash CalculateSignature(Hash inValue)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "CalculateSignature",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(inValue))
            };
            var tc = new TransactionContext
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToPbMessage<Hash>(); 
        }
        
        public RoundInfo PublishOutValueAndSignature(BlockProducer blockProducer, List<Hash> outValue, List<Hash> signatures)
        {
            var roundInfo = new RoundInfo();
            for (var i = 0; i < blockProducer.Nodes.Count; i++)
            {
                var tx = new Transaction
                {
                    From = AddressStringToHash(blockProducer.Nodes[i]),
                    To = ContractAddres,
                    IncrementId = _mock.NewIncrementId(),
                    MethodName = "PublishOutValueAndSignature",
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(outValue[i], signatures[i]))
                };
                var tc = new TransactionContext
                {
                    Transaction = tx
                };
                Executive.SetTransactionContext(tc).Apply().Wait();
                roundInfo.Info[blockProducer.Nodes[i]] = tc.Trace.RetVal.DeserializeToPbMessage<BPInfo>();
            }

            return roundInfo;
        }
        
        private string AddressHashToString(Hash accountHash)
        {
            return accountHash.ToAccount().Value.ToBase64();
        }

        private Hash AddressStringToHash(string accountAddress)
        {
            return Convert.FromBase64String(accountAddress);
        }
    }
}