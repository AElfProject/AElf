using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public sealed class SmartContractAddressServiceTests : SmartContractTestBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ChainOptions _chainOptions;
        private readonly SmartContractHelper _smartContractHelper;
            
        public SmartContractAddressServiceTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
            _smartContractHelper = GetRequiredService<SmartContractHelper>();
        }

        [Fact]
        public async Task SmartContractAddress_Set_And_Get_Test()
        {
            var chain = await _smartContractHelper.CreateChainWithGenesisContractAsync();

            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var address = await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                TestSmartContractAddressNameProvider.StringName);
            address.ShouldBeNull();
            var dto = await _smartContractAddressService.GetSmartContractAddressAsync(chainContext,
                TestSmartContractAddressNameProvider.StringName);
            dto.ShouldBeNull();

            dto = await _smartContractAddressService.GetSmartContractAddressAsync(chainContext,
                ZeroSmartContractAddressNameProvider.StringName);
            dto.SmartContractAddress.Address.ShouldBe(_smartContractAddressService.GetZeroSmartContractAddress());
            dto.Irreversible.ShouldBeFalse();
            address = await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                ZeroSmartContractAddressNameProvider.StringName);
            address.ShouldBe(_smartContractAddressService.GetZeroSmartContractAddress());

            
            await _smartContractAddressService.SetSmartContractAddressAsync(chainContext, TestSmartContractAddressNameProvider.StringName,
                SampleAddress.AddressList[0]);

            address = await _smartContractAddressService.GetAddressByContractNameAsync(chainContext, TestSmartContractAddressNameProvider.StringName);
            address.ShouldBe(SampleAddress.AddressList[0]);

            var smartContractAddressDto =
                await _smartContractAddressService.GetSmartContractAddressAsync(chainContext, TestSmartContractAddressNameProvider.StringName);
            smartContractAddressDto.SmartContractAddress.ShouldBe(new SmartContractAddress
            {
                Address = SampleAddress.AddressList[0],
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            });
            smartContractAddressDto.Irreversible.ShouldBeTrue();

            var map = await _smartContractAddressService.GetSystemContractNameToAddressMappingAsync(chainContext);
            map.Count.ShouldBe(2);
            map[ZeroSmartContractAddressNameProvider.Name].ShouldBe(_smartContractAddressService.GetZeroSmartContractAddress());
            map[TestSmartContractAddressNameProvider.Name].ShouldBe(SampleAddress.AddressList[0]);
        }

        [Fact]
        public async Task SmartContractAddress_Get_WithHeightLargeThanLIB_Test()
        {
            await _smartContractHelper.CreateChainAsync();

            var block = await _kernelTestHelper.AttachBlockToBestChain();
            var chainContext = new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            var contractName = Hash.Empty.ToStorageKey();
            await _smartContractAddressService.SetSmartContractAddressAsync(chainContext, contractName,
                SampleAddress.AddressList[0]);

            var address = await _smartContractAddressService.GetAddressByContractNameAsync(chainContext, contractName);
            address.ShouldBe(SampleAddress.AddressList[0]);

            var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(chainContext, contractName);
            smartContractAddressDto.SmartContractAddress.ShouldBe(new SmartContractAddress
            {
                Address = SampleAddress.AddressList[0],
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            });
            smartContractAddressDto.Irreversible.ShouldBeFalse();
        }

        [Fact]
        public async Task SmartContractAddress_Get_WithFork_Test()
        {
            var chain = await _smartContractHelper.CreateChainAsync();

            var block = await _kernelTestHelper.AttachBlockToBestChain();
            await _blockchainService.SetIrreversibleBlockAsync(chain, block.Height,
                block.GetHash());
            block = _kernelTestHelper.GenerateBlock(block.Header.Height - 1, block.Header.PreviousBlockHash);
            await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            });
            var chainContext = new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            var contractName = Hash.Empty.ToStorageKey();
            await _smartContractAddressService.SetSmartContractAddressAsync(chainContext, contractName,
                SampleAddress.AddressList[0]);
            var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(chainContext, contractName);
            smartContractAddressDto.SmartContractAddress.ShouldBe(new SmartContractAddress
            {
                Address = SampleAddress.AddressList[0],
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            });
            smartContractAddressDto.Irreversible.ShouldBeFalse();
        }

        [Fact]
        public void GetZeroSmartContractAddress_Test()
        {
            var zeroSmartContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();
            zeroSmartContractAddress.ShouldBe(_smartContractAddressService.GetZeroSmartContractAddress(_chainOptions.ChainId));
        }
    }
}