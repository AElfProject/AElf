using System;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class ChainTest
    {
        private readonly IChainCreationService _chainCreationService;
        private readonly ISmartContractZero _smartContractZero;
        private readonly IChainManager _chainManager;
        private readonly IChainContextService _chainContextService;

        public ChainTest(ISmartContractZero smartContractZero, IChainCreationService chainCreationService,
            IChainManager chainManager, IChainContextService chainContextService)
        {
            _smartContractZero = smartContractZero;
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
            _chainContextService = chainContextService;
        }

        [Fact]
        public async Task<IChain> CreateChain()
        {
            var chainId = Hash.Generate();
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, _smartContractZero);
            var adp = new AccountDataProvider
            {
                Context =
                {
                    Address = new Hash(new byte[] {1, 2, 3}),
                    ChainId = chainId
                }
            };
            await _smartContractZero.InititalizeAsync(adp);
            Assert.Equal(1, chain.CurrentBlockHeight);
            return chain;
        }

        public async Task ChainStore(Hash chainId)
        {
            await _chainManager.AddChainAsync(chainId);
            Assert.NotNull(_chainManager.GetChainAsync(chainId).Result);
        }

        public async Task ChainContext()
        {
            var chain = await CreateChain();
            await _chainManager.AddChainAsync(chain.Id);
            chain = await _chainManager.GetChainAsync(chain.Id);
            var context = _chainContextService.GetChainContext(chain.Id);
            Assert.NotNull(context);
            Assert.Equal(context.SmartContractZero, _smartContractZero);
        }

        public async Task AppendBlock()
        {
            var chain = await CreateChain();
            Assert.Equal(chain.CurrentBlockHeight, 1);
            var block = new Block(chain.CurrentBlockHash);
            await _chainManager.AppendBlockToChainAsync(chain, block);
            Assert.Equal(chain.CurrentBlockHeight, 2);
            Assert.Equal(chain.CurrentBlockHash, block.GetHash());
        }
    }
}