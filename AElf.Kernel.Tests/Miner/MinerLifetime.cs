using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using AElf.Kernel.TxMemPool;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Google.Protobuf;
using Moq;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Miner
{
    [UseAutofacTestFramework]
    public class MinerLifetime
    {
        private readonly IBlockGenerationService _blockGenerationService;

        private readonly IParallelTransactionExecutingService _parallelTransactionExecutingService =
            new ParallelTransactionExecutingService(ActorSystem.Create("test"));
        private readonly IChainCreationService _chainCreationService;
        private readonly IChainContextService _chainContextService;
        

        public MinerLifetime(IBlockGenerationService blockGenerationService, 
            IChainCreationService chainCreationService, IChainContextService chainContextService)
        {
            _blockGenerationService = blockGenerationService;
            _chainCreationService = chainCreationService;
            _chainContextService = chainContextService;
        }

        public Mock<ITxPoolService> MockTxPoolService()
        {
            var readyList = new List<ITransaction>();

            var from = Hash.Generate();
            
            var tx1 = new Transaction
            {
                From = from,
                To = Hash.Zero,
                MethodName = "SayHello",
                IncrementId = 0,
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params =
                        {
                            new Param
                            {
                                StrVal = "wk"
                            }
                        }
                    }.ToByteArray()
                )
                
            };

            var tx2 = new Transaction
            {
                From = from,
                To = Hash.Zero,
                MethodName = "SayHello",
                IncrementId = 1,
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params =
                        {
                            new Param
                            {
                                StrVal = "wk"
                            }
                        }
                    }.ToByteArray()
                )
                
            };
            readyList.Add(tx1);
            readyList.Add(tx2);
            
            
            var mock = new Mock<ITxPoolService>();
            mock.Setup((s) => s.GetReadyTxsAsync(It.IsAny<ulong>())).Returns(Task.FromResult(readyList));
            return mock;
        }
        
        public async Task<IChain> CreateChain()
        {
            var smartContract = typeof(Class1);
            var chainId = Hash.Generate();
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, smartContract);
            var chainContext = _chainContextService.GetChainContext(chainId);
            
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFromUtf8(smartContract.AssemblyQualifiedName),
                ContractHash = Hash.Zero
            };

            var deplotment = new SmartContractDeployment
            {
                ContractHash = Hash.Zero
            };
            await chainContext.SmartContractZero.RegisterSmartContract(reg);
            await chainContext.SmartContractZero.DeploySmartContract(deplotment);

            return chain;
        }
        
        public IMiner GetMiner(IMinerConfig config)
        {
            return new Kernel.Miner.Miner(_blockGenerationService, config, MockTxPoolService().Object,
                _parallelTransactionExecutingService);
        }

        public IMinerConfig GetMinerConfig(Hash chainId, ulong txCountLimit)
        {
            return new MinerConifg
            {
                ChainId = chainId,
                TxCountLimit = txCountLimit,
                IsParallel = true
            };
        }
        

        [Fact]
        public async Task MineWithoutStarting()
        {
            var chain = await CreateChain();
            var config = GetMinerConfig(chain.Id, 10);
            var miner = GetMiner(config);
            
            var block = await miner.Mine();
            Assert.Null(block);
        }

        //[Fact]
        public async Task Mine()
        {
            var chain = await CreateChain();
            var config = GetMinerConfig(chain.Id, 10);
            var miner = GetMiner(config);
            
            miner.Start();
            
            
            var block = await miner.Mine();
            Assert.NotNull(block);
        }
        
    }
}