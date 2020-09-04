using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.Cryptography;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public sealed class BlockAttachServiceTests : SmartContractExecutionTestBase
    {
        private readonly IBlockAttachService _blockAttachService;
        private readonly SmartContractExecutionHelper _smartContractExecutionHelper;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public BlockAttachServiceTests()
        {
            _blockAttachService = GetRequiredService<IBlockAttachService>();
            _smartContractExecutionHelper = GetRequiredService<SmartContractExecutionHelper>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        }

        [Fact]
        public async Task AttachBlockAsync_Test()
        {
            var chain = await _smartContractExecutionHelper.CreateChainAsync();
            await _blockAttachService.AttachBlockAsync(_kernelTestHelper.GenerateBlock(0, Hash.Empty));
            
            var blockHeader = new BlockHeader
            {
                Height = chain.BestChainHeight + 1,
                PreviousBlockHash = chain.BestChainHash,
                Time = TimestampHelper.GetUtcNow(),
                SignerPubkey = ByteString.CopyFrom(CryptoHelper.GenerateKeyPair().PublicKey)
            };
            
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    From = _smartContractAddressService.GetZeroSmartContractAddress(),
                    To = _smartContractAddressService.GetZeroSmartContractAddress(),
                    MethodName = nameof(ACS0Container.ACS0Stub.DeploySmartContract),
                    Params = new ContractDeploymentInput
                    {
                        Category = KernelConstants.DefaultRunnerCategory,
                        Code = ByteString.CopyFrom(
                            _smartContractExecutionHelper.ContractCodes["AElf.Contracts.MultiToken"])
                    }.ToByteString()
                }
            };

            var blockExecutedSet = await _blockExecutingService.ExecuteBlockAsync(blockHeader, transactions);
            await _blockchainService.AddBlockAsync(blockExecutedSet.Block);
            _blockAttachService.AttachBlockAsync(blockExecutedSet.Block).ShouldThrow<Exception>();
            
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockAttachService.AttachBlockAsync(blockExecutedSet.Block);
            var newChain = await _blockchainService.GetChainAsync();
            newChain.BestChainHeight.ShouldBe(chain.BestChainHeight + 1);
        }
    }
}