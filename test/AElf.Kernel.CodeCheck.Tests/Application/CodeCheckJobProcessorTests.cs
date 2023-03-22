using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.CodeCheck.Tests;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Kernel.SmartContract.Domain;
using Google.Protobuf;
using Nito.AsyncEx;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.CodeCheck.Application;

public class CodeCheckJobProcessorTests: CodeCheckParallelTestBase
{
    private readonly ICodeCheckJobProcessor _codeCheckJobProcessor;
    private readonly IContractPatcher _contractPatcher;
    private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;
    private readonly ICodeCheckProposalProvider _codeCheckProposalProvider;
    private readonly IProposalProvider _proposalProvider;
    private readonly IBlockStateSetManger _blockStateSetManger;
    private readonly ICodeCheckService _codeCheckService;

    public CodeCheckJobProcessorTests()
    {
        _contractPatcher = GetRequiredService<IContractPatcher>();
        _codeCheckJobProcessor = GetRequiredService<ICodeCheckJobProcessor>();
        _checkedCodeHashProvider = GetRequiredService<ICheckedCodeHashProvider>();
        _codeCheckProposalProvider = GetRequiredService<ICodeCheckProposalProvider>();
        _proposalProvider = GetRequiredService<IProposalProvider>();
        _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        _codeCheckService = GetRequiredService<ICodeCheckService>();
    }

    [Theory(Skip = "Only fails in ci and resolves later.")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CodeCheckTest(bool isUserContract)
    {
        var block = MockBlock();
        await InitBlockStateSetAsync(block);
        
        var job = new CodeCheckJob
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height,
            ContractCategory = 0,
            ContractCode = GetPatchedContactCode("AElf.Contracts.Configuration"),
            IsSystemContract = false,
            IsUserContract = true,
            CodeCheckProposalId = HashHelper.ComputeFrom("CodeCheckProposalId"),
            ProposedContractInputHash = HashHelper.ComputeFrom("ProposedContractInputHash"),
        };
        var sendResult = await _codeCheckJobProcessor.SendAsync(job);
        sendResult.ShouldBeTrue();
        await _codeCheckJobProcessor.CompleteAsync();

        var notApprovedProposalIdList = _proposalProvider.GetAllProposals();
        notApprovedProposalIdList.Count.ShouldBe(1);
        notApprovedProposalIdList[0].ShouldBe(job.CodeCheckProposalId);

        if (isUserContract)
        {
            var codeCheckProposals = _codeCheckProposalProvider.GetAllProposals();
            codeCheckProposals.Count.ShouldBe(1);
            codeCheckProposals[0].ProposalId.ShouldBe(job.CodeCheckProposalId);
            codeCheckProposals[0].ProposedContractInputHash.ShouldBe(job.ProposedContractInputHash);
        }

        var codeHashExists = _checkedCodeHashProvider.IsCodeHashExists(new BlockIndex
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        }, HashHelper.ComputeFrom(job.ContractCode));
        codeHashExists.ShouldBeTrue();
    }
    
    [Fact(Skip = "Only fails in ci and resolves later.")]
    public async Task CodeCheck_Failed_Test()
    {
        var block = MockBlock();
        await InitBlockStateSetAsync(block);
        
        var job = new CodeCheckJob
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height,
            ContractCategory = 0,
            ContractCode = GetContactCode("AElf.Contracts.Configuration"),
            IsSystemContract = false,
            IsUserContract = true,
            CodeCheckProposalId = HashHelper.ComputeFrom("CodeCheckProposalId"),
            ProposedContractInputHash = HashHelper.ComputeFrom("ProposedContractInputHash"),
        };
        var sendResult = await _codeCheckJobProcessor.SendAsync(job);
        sendResult.ShouldBeTrue();
        await _codeCheckJobProcessor.CompleteAsync();

        var notApprovedProposalIdList = _proposalProvider.GetAllProposals();
        notApprovedProposalIdList.Count.ShouldBe(0);

        var codeCheckProposals = _codeCheckProposalProvider.GetAllProposals();
        codeCheckProposals.Count.ShouldBe(0);

        var codeHashExists = _checkedCodeHashProvider.IsCodeHashExists(new BlockIndex
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        }, HashHelper.ComputeFrom(job.ContractCode));
        codeHashExists.ShouldBeFalse();
    }

    [Fact(Skip = "Only fails in ci and resolves later.")]
    public async Task CodeCheck_Parallel_Test()
    {
        var block = MockBlock();
        await InitBlockStateSetAsync(block);

        var jobs = new List<CodeCheckJob>();
        for (var i = 0; i < 5; i++)
        {
            jobs.Add(new CodeCheckJob
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height,
                ContractCategory = 0,
                ContractCode = GetPatchedContactCode("AElf.Contracts.Configuration"),
                IsSystemContract = false,
                IsUserContract = true,
                CodeCheckProposalId = HashHelper.ComputeFrom("CodeCheckProposalId" + i),
                ProposedContractInputHash = HashHelper.ComputeFrom("ProposedContractInputHash" + i)
            });
        }
        
        var jobTasks = jobs.Select(async job => await _codeCheckJobProcessor.SendAsync(job));
        await jobTasks.WhenAll();
        await _codeCheckJobProcessor.CompleteAsync();

        var notApprovedProposalIdList = _proposalProvider.GetAllProposals();
        notApprovedProposalIdList.Count.ShouldBe(jobs.Count);

        var codeCheckProposals = _codeCheckProposalProvider.GetAllProposals();
        codeCheckProposals.Count.ShouldBe(jobs.Count);

        foreach (var codeHashExists in jobs.Select(job => _checkedCodeHashProvider.IsCodeHashExists(new BlockIndex
                 {
                     BlockHash = block.GetHash(),
                     BlockHeight = block.Height
                 }, HashHelper.ComputeFrom(job.ContractCode))))
        {
            codeHashExists.ShouldBeTrue();
        }
    }

    private byte[] GetPatchedContactCode(string name)
    {
        var code = GetContactCode(name);
        var patchedCode = _contractPatcher.Patch(code, false);
        return patchedCode;
    }
    
    private byte[] GetContactCode(string name)
    {
        var path = Path.Combine(Environment.CurrentDirectory, name + ".dll");
        return File.ReadAllBytes(path);
    }

    private Block MockBlock()
    {
       return new Block
        {
            Header = new BlockHeader
            {
                Height = 100,
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                MerkleTreeRootOfTransactions = HashHelper.ComputeFrom("MerkleTreeRootOfTransactions"),
                MerkleTreeRootOfWorldState = HashHelper.ComputeFrom("MerkleTreeRootOfWorldState"),
                MerkleTreeRootOfTransactionStatus = HashHelper.ComputeFrom("MerkleTreeRootOfTransactionStatus"),
                Time = TimestampHelper.GetUtcNow(),
                SignerPubkey = ByteString.CopyFromUtf8("SignerPubkey")
            }
        };
    }

    private async Task InitBlockStateSetAsync(Block block)
    {
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height,
            PreviousHash = block.Header.PreviousBlockHash
        });
    }
}