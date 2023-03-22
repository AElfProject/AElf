using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks.Dataflow;
using AElf.Kernel.Proposal.Application;

namespace AElf.Kernel.CodeCheck.Application;

public interface ICodeCheckJobProcessor
{
    Task<bool> SendAsync(CodeCheckJob job);
    Task CompleteAsync();
}

public class CodeCheckJobProcessor : ICodeCheckJobProcessor, ISingletonDependency
{
    private readonly TransformBlock<CodeCheckJob, CodeCheckJob> _codeCheckTransformBlock;
    private List<ActionBlock<CodeCheckJob>> _codeCheckProcessesJobTransformBlock;
    private readonly CodeCheckOptions _codeCheckOptions;
    private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;
    private readonly ICodeCheckService _codeCheckService;
    private readonly IProposalService _proposalService;
    private readonly ICodeCheckProposalService _codeCheckProposalService;
    
    public ILogger<CodeCheckJobProcessor> Logger { get; set; }

    public CodeCheckJobProcessor(IOptionsSnapshot<CodeCheckOptions> codeCheckOptions,
        ICheckedCodeHashProvider checkedCodeHashProvider, IProposalService proposalService,
        ICodeCheckService codeCheckService, ICodeCheckProposalService codeCheckProposalService)
    {
        _checkedCodeHashProvider = checkedCodeHashProvider;
        _proposalService = proposalService;
        _codeCheckService = codeCheckService;
        _codeCheckProposalService = codeCheckProposalService;
        _codeCheckOptions = codeCheckOptions.Value;
        _codeCheckTransformBlock = CreateCodeCheckBufferBlock();

        Logger = NullLogger<CodeCheckJobProcessor>.Instance;
    }

    public async Task<bool> SendAsync(CodeCheckJob job)
    {
        return await _codeCheckTransformBlock.SendAsync(job);
    }

    public async Task CompleteAsync()
    {
        _codeCheckTransformBlock.Complete();
        await Task.WhenAll(_codeCheckProcessesJobTransformBlock.Select(o => o.Completion));
    }

    private TransformBlock<CodeCheckJob, CodeCheckJob> CreateCodeCheckBufferBlock()
    {
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        var updateBucketIndexTransformBlock = new TransformBlock<CodeCheckJob, CodeCheckJob>(UpdateBucketIndex,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = Math.Max(_codeCheckOptions.MaxBoundedCapacity, 1),
                MaxDegreeOfParallelism = _codeCheckOptions.MaxDegreeOfParallelism
            });
        
        _codeCheckProcessesJobTransformBlock = new List<ActionBlock<CodeCheckJob>>();
        for (var i = 0; i < _codeCheckOptions.MaxDegreeOfParallelism; i++)
        {
            var processCodeCheckJobTransformBlock = new ActionBlock<CodeCheckJob>(
                async codeCheckJob => await ProcessCodeCheckJobAsync(codeCheckJob),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = Math.Max(_codeCheckOptions.MaxBoundedCapacity, 1),
                    EnsureOrdered = false
                });
            var index = i;
            updateBucketIndexTransformBlock.LinkTo(processCodeCheckJobTransformBlock, linkOptions,
                codeCheckJob => codeCheckJob.BucketIndex == index);
            _codeCheckProcessesJobTransformBlock.Add(processCodeCheckJobTransformBlock);
        }

        return updateBucketIndexTransformBlock;
    }

    private async Task ProcessCodeCheckJobAsync(CodeCheckJob job)
    {
        var codeCheckResult = await _codeCheckService.PerformCodeCheckAsync(job.ContractCode, job.BlockHash,
            job.BlockHeight, job.ContractCategory, job.IsSystemContract, job.IsUserContract);
        
        var codeHash = HashHelper.ComputeFrom(job.ContractCode);
        Logger.LogInformation("Code check result: {codeCheckResult}, code hash: {codeHash}", codeCheckResult,
            codeHash.ToHex());
        
        if (!codeCheckResult)
            return;

        if (job.IsUserContract)
        {
            _codeCheckProposalService.AddReleasableProposal(job.CodeCheckProposalId, job.ProposedContractInputHash,
                job.BlockHeight);
        }

        // Cache proposal id to generate system approval transaction later
        _proposalService.AddNotApprovedProposal(job.CodeCheckProposalId, job.BlockHeight);

        await _checkedCodeHashProvider.AddCodeHashAsync(new BlockIndex
        {
            BlockHash = job.BlockHash,
            BlockHeight = job.BlockHeight
        }, codeHash);
    }
    
    private CodeCheckJob UpdateBucketIndex(CodeCheckJob job)
    {
        var assemblyLoadContext = new AssemblyLoadContext(null, true);
        var assembly = assemblyLoadContext.LoadFromStream(new MemoryStream(job.ContractCode));
        
        job.BucketIndex =
            Math.Abs(HashHelper.ComputeFrom(assembly.GetName().Name).ToInt64() % _codeCheckOptions.MaxDegreeOfParallelism);
        assemblyLoadContext.Unload();
        
        return job;
    }
}