using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public sealed class SmartContractBridgeServiceTests : SmartContractTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractBridgeService _smartContractBridgeService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ChainOptions _chainOptions;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly SmartContractHelper _smartContractHelper;

        public SmartContractBridgeServiceTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractBridgeService = GetRequiredService<ISmartContractBridgeService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _smartContractHelper = GetRequiredService<SmartContractHelper>();
        }

        [Fact]
        public async Task GetBlockTransactions_Test()
        {
            var transactions = new List<Transaction>();
            for (var i = 0; i < 3; i++)
            {
                var transaction = _kernelTestHelper.GenerateTransaction();
                transactions.Add(transaction);
            }

            var block = _kernelTestHelper.GenerateBlock(0, Hash.Empty, transactions);

            await _blockchainService.AddBlockAsync(block);
            await _blockchainService.AddTransactionsAsync(transactions);

            var blockTransactions = await _smartContractBridgeService.GetBlockTransactions(block.GetHash());
            blockTransactions.ShouldBe(transactions);
        }

        [Fact]
        public void GetChainId_Test()
        {
            _smartContractBridgeService.GetChainId().ShouldBe(_chainOptions.ChainId);
        }

        [Fact]
        public void GetZeroSmartContractAddress_Test()
        {
            _smartContractBridgeService.GetZeroSmartContractAddress()
                .ShouldBe(_smartContractBridgeService.GetZeroSmartContractAddress(_chainOptions.ChainId));
        }

        [Fact]
        public async Task GetStateAsync_Test()
        {
            var chain = await _smartContractHelper.CreateChainAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _smartContractBridgeService.GetStateAsync(
                SampleAddress.AddressList[0], string.Empty,
                chain.BestChainHeight,
                chain.BestChainHash));
            
            
            var scopedStatePath = new ScopedStatePath
            {
                Address = SampleAddress.AddressList[0],
                Path = new StatePath
                {
                    Parts = { "part"}
                }
            };
            var state = await _smartContractBridgeService.GetStateAsync(SampleAddress.AddressList[0], scopedStatePath.ToStateKey(),
                chain.BestChainHeight, chain.BestChainHash);
            state.ShouldBeNull();
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            blockStateSet.Changes[scopedStatePath.ToStateKey()] = ByteString.Empty;
            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet);
            
            state = await _smartContractBridgeService.GetStateAsync(SampleAddress.AddressList[0], scopedStatePath.ToStateKey(),
                chain.BestChainHeight, chain.BestChainHash);
            state.ShouldBe(ByteString.Empty);
        }
    }
}