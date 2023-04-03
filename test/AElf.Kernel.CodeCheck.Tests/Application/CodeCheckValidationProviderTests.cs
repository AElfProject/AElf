using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.CodeCheck.Tests;
using AElf.Kernel.SmartContract.Domain;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Application;

public class CodeCheckValidationProviderTests: CodeCheckTestBase
{
    private readonly IBlockValidationProvider _blockValidationProvider;
    private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;
    private readonly IBlockStateSetManger _blockStateSetManger;

    public CodeCheckValidationProviderTests()
    {
        _blockValidationProvider = GetRequiredService<IBlockValidationProvider>();
        _checkedCodeHashProvider = GetRequiredService<ICheckedCodeHashProvider>();
        _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
    }

    [Fact]
    public async Task ValidateBlockAfterExecuteTest()
    {
        var block = new Block
        {
            Header = new BlockHeader
            {
                Height = 1,
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                MerkleTreeRootOfTransactions = HashHelper.ComputeFrom("MerkleTreeRootOfTransactions"),
                MerkleTreeRootOfWorldState = HashHelper.ComputeFrom("MerkleTreeRootOfWorldState"),
                MerkleTreeRootOfTransactionStatus = HashHelper.ComputeFrom("MerkleTreeRootOfTransactionStatus"),
                Time = TimestampHelper.GetUtcNow(),
                SignerPubkey = ByteString.CopyFromUtf8("SignerPubkey")
            }
        };
        var validationResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block);
        validationResult.ShouldBeTrue();

        block.Header.Height = 2;
        validationResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block);
        validationResult.ShouldBeTrue();
        
        var logEvent = new LogEvent
        {
            Name = "ContractDeployed",
            Address = ZeroContractFakeAddress
        };
        var bloom = logEvent.GetBloom();
        block.Header.Bloom = ByteString.CopyFrom(bloom.Data);
        validationResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block);
        validationResult.ShouldBeTrue();

        block.Header.Height = 4;
        validationResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block);
        validationResult.ShouldBeFalse();

        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height,
            PreviousHash = block.Header.PreviousBlockHash
        });
        await _checkedCodeHashProvider.AddCodeHashAsync(new BlockIndex
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        }, HashHelper.ComputeFrom(block.Height));
        
        validationResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block);
        validationResult.ShouldBeTrue();
    }
}