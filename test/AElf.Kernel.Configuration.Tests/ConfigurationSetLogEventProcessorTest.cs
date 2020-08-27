using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Configuration.Tests
{
    public partial class ConfigurationServiceTest
    {
        [Fact]
        public async Task GetInterestedEventAsync_Without_DeployContract_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var configurationSetLogEventProcessor = GetRequiredService<IBlockAcceptedLogEventProcessor>();
            var eventData = await configurationSetLogEventProcessor.GetInterestedEventAsync(chainContext);
            eventData.ShouldBeNull();
        }
        
        [Fact]
        public async Task GetInterestedEventAsync_Repeat_Get_Test()
        {
            await InitializeContractsAndSetLib();
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var configurationSetLogEventProcessor = GetRequiredService<IBlockAcceptedLogEventProcessor>();
            await configurationSetLogEventProcessor.GetInterestedEventAsync(chainContext);
            var eventData = await configurationSetLogEventProcessor.GetInterestedEventAsync(null);
            eventData.ShouldNotBeNull();
        }
        
        [Fact]
        public async Task GetInterestedEventAsync_Test()
        {
            await InitializeContractsAndSetLib();
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var configurationSetLogEventProcessor = GetRequiredService<IBlockAcceptedLogEventProcessor>();
            var eventData = await configurationSetLogEventProcessor.GetInterestedEventAsync(chainContext);
            eventData.LogEvent.Address.ShouldBe(ConfigurationContractAddress);
        }

        [Fact]
        public async Task ProcessLogEventAsync_Test()
        {
            var blockTransactionLimitConfigurationProcessor = GetRequiredService<IConfigurationProcessor>();
            var configurationName = blockTransactionLimitConfigurationProcessor.ConfigurationName;
            var key = configurationName;
            var value = new Int32Value
            {
                Value = 100
            };
            var setting = new ConfigurationSet
            {
                Key = key,
                Value = value.ToByteString()
            };
            var logEvent = setting.ToLogEvent();
            var chain = await _blockchainService.GetChainAsync();
            var block = await _blockchainService.GetBlockByHeightInBestChainBranchAsync(chain.BestChainHeight);
            var configurationSetLogEventProcessor = GetRequiredService<IBlockAcceptedLogEventProcessor>();
            var logEventDic = new Dictionary<TransactionResult,List<LogEvent>>();
            var transactionRet = new TransactionResult
            {
                BlockHash = block.GetHash()
            };
            logEventDic[transactionRet] = new List<LogEvent>{logEvent};
            await configurationSetLogEventProcessor.ProcessAsync(block, logEventDic);
            await _blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);
            var getValue = await _blockTransactionLimitProvider.GetLimitAsync(new BlockIndex
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            });
            getValue.ShouldBe(value.Value);
        }
    }
}