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
        public async Task<Chain> CreateChain()
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
            Assert.Equal(chain.CurrentBlockHeight, (ulong)1);
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

        public async Task AppendBlock(Chain chain, Block block)
        {
            await _chainManager.AppendBlockToChainAsync(chain, block);
            Assert.Equal(chain.CurrentBlockHeight, (ulong)2);
            Assert.Equal(chain.CurrentBlockHash, block.GetHash());
        }
    }
}