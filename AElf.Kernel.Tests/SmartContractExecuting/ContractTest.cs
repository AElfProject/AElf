using System;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;
using Type = System.Type;

namespace AElf.Kernel.Tests.SmartContractExecuting
{
    [UseAutofacTestFramework]
    public class ContractTest
    {

        private IWorldStateManager _worldStateManager;
        private IChainCreationService _chainCreationService;
        private IChainContextService _chainContextService;
        private IBlockManager _blockManager;
        private ITransactionManager _transactionManager;
        private ISmartContractService _smartContractService;

        private Hash ChainId { get; } = Hash.Generate();

        public ContractTest(IWorldStateManager worldStateManager,
            IChainCreationService chainCreationService, IBlockManager blockManager, 
            ITransactionManager transactionManager, ISmartContractService smartContractService, 
            IChainContextService chainContextService)
        {
            _worldStateManager = worldStateManager;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _transactionManager = transactionManager;
            _smartContractService = smartContractService;
            _chainContextService = chainContextService;
        }
        

        [Fact]
        public async Task RegisterContract()
        {
            var smartContractZero = typeof(Class1);
            Assert.Equal(smartContractZero, typeof(Class1));
            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, smartContractZero);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);
            var txs = genesis.Body.Transactions;
            var register = await _transactionManager.GetTransaction(txs[0]);
            var adp = (await _worldStateManager.OfChain(ChainId)).GetAccountDataProvider(Path.CalculatePointerForAccountZero(ChainId));
            
            var chainContext = _chainContextService.GetChainContext(ChainId);

            var inovkeContext = new SmartContractInvokeContext
            {
                Caller = register.From,
                IncrementId = register.IncrementId,
                MethodName = register.MethodName,
                Params = register.Params
                
            };
            var sm = await _smartContractService.GetAsync(inovkeContext.Caller, chainContext);
            await sm.InvokeAsync(inovkeContext);

            var smartContractMap = adp.GetDataProvider().GetDataProvider("SmartContractMap");

            var copy = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFromUtf8(smartContractZero.AssemblyQualifiedName),
                ContractHash = Hash.Zero
            };

            var hash = Hash.Zero;
            var bytes = await smartContractMap.GetAsync(hash); 
            var reg = SmartContractRegistration.Parser.ParseFrom(bytes);
            
            // throw exception if not registered
            Assert.Equal(reg, copy);

        }

        public SmartContractInvokeContext RegisterContext(Type smartContractZero)
        {
            // register context
            var registerContext = new SmartContractInvokeContext
            {
                Caller = Hash.Zero,
                IncrementId = 0,
                MethodName = nameof(ISmartContractZero.RegisterSmartContract),
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = 
                        {
                            new Param
                            {
                                RegisterVal = new SmartContractRegistration
                                {
                                    Category = 1,
                                    ContractBytes = new StringValue
                                    {
                                        Value = smartContractZero.AssemblyQualifiedName
                                    }.ToByteString(),
                                    ContractHash = Hash.Zero
                                }
                            }
                        }
                    }.ToByteArray()
                )
            };

            return registerContext;
        }

        public SmartContractInvokeContext DeploymentContext(string name)
        {
            var deployContext = new SmartContractInvokeContext
            {
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                Caller = Hash.Zero,
                Params = ByteString.CopyFrom(
                    new Parameters
                        {
                            Params = {
                                new Param
                                {
                                    DeploymentVal = new SmartContractDeployment
                                    {
                                        ContractHash = Hash.Zero,
                                        Caller = Hash.Zero,
                                        ConstructParams = ByteString.CopyFrom(
                                            new Parameters
                                            {
                                                Params =
                                                {
                                                    new Param
                                                    {
                                                        StrVal = name
                                                    }
                                                }
                                            }.ToByteArray()
                                        ),
                                        IncrementId = 1
                                    }
                                }
                            }
                        }
                        .ToByteArray()
                )
            };

            return deployContext;
        }
        
        [Fact]
        public async Task DeployContract()
        {
            // register smart contract
            var smartContractZero = typeof(Class1);
            
            // create chain
            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, smartContractZero);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);
            var chainContext = _chainContextService.GetChainContext(ChainId);

            var registerContext = RegisterContext(smartContractZero);
            
            // register
            var sm1 = await _smartContractService.GetAsync(registerContext.Caller, chainContext);
            await sm1.InvokeAsync(registerContext);

            // deploy contract

            var name = "Sam";
            var deployContext = DeploymentContext(name);
            var sm2 = await _smartContractService.GetAsync(deployContext.Caller, chainContext);
            var smartcontract = (CSharpSmartContract)await sm2.InvokeAsync(deployContext);
            Assert.Equal(typeof(CSharpSmartContract), smartcontract.GetType());

            
            Assert.Equal(name, ((Class1)smartcontract.Instance).Name);
        }


        [Fact]
        public async Task Invoke()
        {
            // register smart contract
            var smartContractZero = typeof(Class1);
            
            // create chain
            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, smartContractZero);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);
            var chainContext = _chainContextService.GetChainContext(ChainId);

            var registerContext = RegisterContext(smartContractZero);
            
            // register
            var sm1 = await _smartContractService.GetAsync(registerContext.Caller, chainContext);
            await sm1.InvokeAsync(registerContext);

            // deploy contract
            var name = "Sam";
            var deployContext = DeploymentContext(name);
            var sm2 = await _smartContractService.GetAsync(deployContext.Caller, chainContext);
            var smartcontract = (ISmartContract) await sm2.InvokeAsync(deployContext);

            //var sm3 =(CSharpSmartContract) await _smartContractService.GetAsync((Hash) account, chainContext);


            var yours = "Wk";
            var inokeContext = new SmartContractInvokeContext
            {
                Caller = Hash.Generate(),
                IncrementId = 0,
                MethodName = "SayHello",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params =
                        {
                            new Param
                            {
                                StrVal = yours
                            }
                        }
                    }.ToByteArray()
                )
            };
            
            var str = await smartcontract.InvokeAsync(inokeContext);
            
            Assert.Equal(name, str);

        }
    }
}