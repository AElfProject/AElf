using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Cryptography.ECDSA;
using AElf.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Miner.Miner;
using AElf.Miner.Tests.Grpc;
using AElf.Runtime.CSharp;
using AElf.Types.CSharp;
using AElf.SmartContract;
using AElf.SmartContract.Metadata;
using AElf.Types.CSharp;
using Akka.Actor;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;
using ByteString = Google.Protobuf.ByteString;

namespace AElf.Miner.Tests
{
    [UseAutofacTestFramework]
    public class Synchronizer
    {
        private MockSetup _mock;

        public Synchronizer(MockSetup mock, ILogger logger)
        {
            _mock = mock;
        }


        private byte[] ExampleContractCode
        {
            get
            {
                return ContractCodes.TestContractCode;
            }
        }
        
        
        public Block GenerateBlock(Hash chainId, Hash previousHash, ulong index)
        {
            var block = new Block(previousHash)
            {
                Header = new BlockHeader
                {
                    ChainId = chainId,
                    Index = index,
                    PreviousBlockHash = previousHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    MerkleTreeRootOfWorldState = Hash.Default
                }
            };
            block.FillTxsMerkleTreeRootInHeader();
            return block;
        }


        public List<Transaction> CreateTxs(Hash chainId)
        {
            var contractAddressZero = new Hash(chainId.CalculateHashWith(Globals.GenesisBasicContract)).ToAccount();

            var code = ExampleContractCode;
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            var txnDep = new Transaction()
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack((int)0, code)),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1,
                Type = TransactionType.ContractTransaction
            };
            
            Hash hash = txnDep.GetHash();

            ECSignature signature1 = signer.Sign(keyPair, hash.GetHashBytes());
            txnDep.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txnDep.R = ByteString.CopyFrom(signature1.R); 
            txnDep.S = ByteString.CopyFrom(signature1.S);
            
            var txInv_1 = new Transaction
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = 1,
                MethodName = "Print",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("AElf")),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1,
                Type = TransactionType.ContractTransaction
            };
            ECSignature signature2 = signer.Sign(keyPair, txInv_1.GetHash().GetHashBytes());
            txInv_1.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txInv_1.R = ByteString.CopyFrom(signature2.R); 
            txInv_1.S = ByteString.CopyFrom(signature2.S);
            
            var txInv_2 = new Transaction
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId =txInv_1.IncrementId,
                MethodName = "Print",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("Hoopox")),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1,
                Type = TransactionType.ContractTransaction
            };
            
            ECSignature signature3 = signer.Sign(keyPair, txInv_2.GetHash().GetHashBytes());
            txInv_2.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txInv_2.R = ByteString.CopyFrom(signature3.R); 
            txInv_2.S = ByteString.CopyFrom(signature3.S);
            
            var txs = new List<Transaction>(){
                txnDep, txInv_1, txInv_2
            };

            return txs;
        }
        [Fact]
        public async Task SyncGenesisBlock_False_Rollback()
        {
            var poolconfig = TxPoolConfig.Default;
            var chain = await _mock.CreateChain();
            poolconfig.ChainId = chain.Id;
            
            var poolService = _mock.CreateTxPoolService(chain.Id);
            poolService.Start();
            var block = GenerateBlock(chain.Id, chain.GenesisBlockHash, 1);
            
            var txs = CreateTxs(chain.Id);
            foreach (var transaction in txs)
            {
                await poolService.AddTxAsync(transaction);
            }
            
            Assert.Equal((ulong)0, await poolService.GetWaitingSizeAsync());
            Assert.Equal((ulong)2, await poolService.GetExecutableSizeAsync());
            Assert.True(poolService.TryGetTx(txs[2].GetHash(), out var tx));
            
            block.Body.Transactions.Add(txs[0].GetHash());
            block.Body.Transactions.Add(txs[2].GetHash());

            block.FillTxsMerkleTreeRootInHeader();
            block.Body.BlockHeader = block.Header.GetHash();
            var manager = _mock.MinerClientManager();
            var synchronizer = _mock.GetBlockExecutor(poolService, manager);

            synchronizer.Start();
            var res = await synchronizer.ExecuteBlock(block);
            Assert.False(res);

            Assert.Equal((ulong)0, await poolService.GetWaitingSizeAsync());
            Assert.Equal((ulong)2, await poolService.GetExecutableSizeAsync());
            //Assert.False(poolService.TryGetTx(txs[2].GetHash(), out tx));
            Assert.True(poolService.TryGetTx(txs[1].GetHash(), out tx));

            var blockchain = _mock.GetBlockChain(chain.Id); 
            var curHash = await blockchain.GetCurrentBlockHashAsync();
            var index = ((BlockHeader) await blockchain.GetHeaderByHashAsync(curHash)).Index;
            Assert.Equal((ulong)0, index);
            Assert.Equal(chain.GenesisBlockHash.ToHex(), curHash.ToHex());
        }
    }
}