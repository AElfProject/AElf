using System;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;

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
                ContractBytes = ByteString.CopyFromUtf8(smartContractZero.FullName),
                ContractHash = Hash.Zero
            };

            var hash = Hash.Zero;
            var bytes = await smartContractMap.GetAsync(hash); 
            var reg = SmartContractRegistration.Parser.ParseFrom(bytes);
            
            // throw exception if not registered
            Assert.Equal(reg, copy);

        }
        
        
        public void DeployContract()
        {
            var deployTx = new Transaction
            {
                IncrementId = 1,
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                From = Hash.Zero,
                To = Hash.Zero,
                Params = ByteString.CopyFrom(
                    new Parameters
                        {
                            Params = { 
                                new Param
                                {
                                    HashVal = Hash.Zero
                                },
                                new Param
                                {
                                    DeploymentVal = new SmartContractDeployment
                                    {
                                        ContractHash = Hash.Zero
                                    }
                                }}
                        }
                        .ToByteArray()
                )
            };
        }
    }
}