using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.TestBase;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS
{
    public class IrreversibleBlockDiscoveryServiceTests : AElfIntegratedTest<LibTestModule>
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainService _blockchainService;
        private readonly IIrreversibleBlockDiscoveryService _irreversibleBlockDiscoveryService;
        private readonly KernelTestHelper _kernelTestHelper;
        
        private readonly Address _consensusAddress = Address.FromString("ConsensusAddress");

        public IrreversibleBlockDiscoveryServiceTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _irreversibleBlockDiscoveryService = GetRequiredService<IIrreversibleBlockDiscoveryService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();

            _smartContractAddressService.SetAddress(ConsensusSmartContractAddressNameProvider.Name, _consensusAddress);
        }

        [Fact]
        public async Task Test_HandleEvent()
        {
            {
                // Null event
                //             BestChainHeight: 11
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var index = await _irreversibleBlockDiscoveryService.DiscoverAndSetIrreversibleAsync(chain, new List<Hash>());
                if (index != null)
                {
                    await _blockchainService.SetIrreversibleBlockAsync(chain, index.Height, index.Hash);
                }

                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Transaction execute failed
                //             BestChainHeight: 12
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var transaction = _kernelTestHelper.GenerateTransaction();
                var transactionResult =
                    _kernelTestHelper.GenerateTransactionResult(transaction, TransactionResultStatus.Failed);
                var newBlock = await _kernelTestHelper.AttachBlock(chain.BestChainHeight, chain.LongestChainHash, new
                    List<Transaction> {transaction}, new List<TransactionResult> {transactionResult});
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var executedBlocks = new List<Hash> {newBlock.GetHash()};

                var index = await _irreversibleBlockDiscoveryService.DiscoverAndSetIrreversibleAsync(chain, executedBlocks);
                if (index != null)
                {
                    await _blockchainService.SetIrreversibleBlockAsync(chain, index.Height, index.Hash);
                }
                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Event not from consensus smart contract
                //             BestChainHeight: 13
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var transaction = _kernelTestHelper.GenerateTransaction();
                var logEvent = new AElf.Contracts.Consensus.DPoS.IrreversibleBlockFound()
                {
                    Offset = 6
                }.ToLogEvent(Address.FromString("TokenContract"));

                var transactionResult =
                    _kernelTestHelper.GenerateTransactionResult(transaction, TransactionResultStatus.Mined, logEvent);
                var newBlock = await _kernelTestHelper.AttachBlock(chain.BestChainHeight, chain.LongestChainHash,
                    new List<Transaction> {transaction}, new List<TransactionResult> {transactionResult});
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var executedBlocks = new List<Hash> {newBlock.GetHash()};

                var index = await _irreversibleBlockDiscoveryService.DiscoverAndSetIrreversibleAsync(chain, executedBlocks);
                if (index != null)
                {
                    await _blockchainService.SetIrreversibleBlockAsync(chain, index.Height, index.Hash);
                }
                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // Event from consensus smart contract, not contains 'LIBFound'
                //             BestChainHeight: 14
                // LastIrreversibleBlockHeight: 5
                var chain = await _blockchainService.GetChainAsync();
                var currentLibHeight = chain.LastIrreversibleBlockHeight;
                var currentLibHash = chain.LastIrreversibleBlockHash;

                var transaction = _kernelTestHelper.GenerateTransaction();
                var logEvent = new LogEvent
                {
                    Address = _consensusAddress,
                    Name = "NonExistentEvent"
                };
                var transactionResult =
                    _kernelTestHelper.GenerateTransactionResult(transaction, TransactionResultStatus.Mined, logEvent);
                var newBlock = await _kernelTestHelper.AttachBlock(chain.BestChainHeight, chain.LongestChainHash,
                    new List<Transaction> {transaction}, new List<TransactionResult> {transactionResult});
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var executedBlocks = new List<Hash> {newBlock.GetHash()};

                var index = await _irreversibleBlockDiscoveryService.DiscoverAndSetIrreversibleAsync(chain, executedBlocks);
                if (index != null)
                {
                    await _blockchainService.SetIrreversibleBlockAsync(chain, index.Height, index.Hash);
                }
                LibShouldBe(currentLibHeight, currentLibHash);
            }

            {
                // LIBFound
                //             BestChainHeight: 15
                // LastIrreversibleBlockHeight: 10
                var chain = await _blockchainService.GetChainAsync();

                var transaction = _kernelTestHelper.GenerateTransaction();
                var offset = 5;
                var logEvent = new AElf.Contracts.Consensus.DPoS.IrreversibleBlockFound()
                {
                    Offset = offset
                }.ToLogEvent(_consensusAddress);
                var transactionResult =
                    _kernelTestHelper.GenerateTransactionResult(transaction, TransactionResultStatus.Mined, logEvent);
                var newBlock = await _kernelTestHelper.AttachBlock(chain.BestChainHeight, chain.BestChainHash,
                    new List<Transaction> {transaction}, new List<TransactionResult> {transactionResult});
                await _blockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());

                var libHash = await _blockchainService.GetBlockHashByHeightAsync(chain, 10, chain.LongestChainHash);

                var executedBlocks = new List<Hash> {newBlock.GetHash()};

                var index = await _irreversibleBlockDiscoveryService.DiscoverAndSetIrreversibleAsync(chain, executedBlocks);
                if (index != null)
                {
                    await _blockchainService.SetIrreversibleBlockAsync(chain, index.Height, index.Hash);
                }
                LibShouldBe(10, libHash);
            }
        }

        #region check methods

        private void LibShouldBe(long libHeight, Hash libHash)
        {
            var chain = _blockchainService.GetChainAsync().Result;
            chain.LastIrreversibleBlockHeight.ShouldBe(libHeight);
            chain.LastIrreversibleBlockHash.ShouldBe(libHash);
        }

        #endregion
    }
}