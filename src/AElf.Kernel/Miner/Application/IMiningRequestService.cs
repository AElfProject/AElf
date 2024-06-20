using System;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner.Application;

public interface IMiningRequestService
{
    Task<BlockExecutedSet> RequestMiningAsync(ConsensusRequestMiningDto requestMiningDto);
}

public class ConsensusRequestMiningDto
{
    public Hash PreviousBlockHash { get; set; }
    public long PreviousBlockHeight { get; set; }
    public Duration BlockExecutionTime { get; set; }
    public Timestamp BlockTime { get; set; }
    public Timestamp MiningDueTime { get; set; }
}

public class MiningRequestService : IMiningRequestService
{
    private readonly IMinerService _minerService;
    public ILogger<MiningRequestService> Logger { get; set; }

    public MiningRequestService(IMinerService minerService)
    {
        _minerService = minerService;
    }

    public async Task<BlockExecutedSet> RequestMiningAsync(ConsensusRequestMiningDto requestMiningDto)
    {
        Logger.LogTrace("Begin MiningRequestService.RequestMiningAsync");
        var dur = requestMiningDto.BlockExecutionTime;
        if (!ValidateBlockMiningTime(requestMiningDto.BlockTime, requestMiningDto.MiningDueTime,
                ref dur))
            return null;
            
        var blockExecutionDuration =
            CalculateBlockMiningDuration(requestMiningDto.BlockTime, dur);

        var blockExecutedSet = await _minerService.MineAsync(requestMiningDto.PreviousBlockHash,
            requestMiningDto.PreviousBlockHeight, requestMiningDto.BlockTime, blockExecutionDuration);

        Logger.LogTrace("End MiningRequestService.RequestMiningAsync");
        return blockExecutedSet;
    }

    private bool ValidateBlockMiningTime(Timestamp blockTime, Timestamp miningDueTime,
        ref Duration blockExecutionDuration)
    {
        if (miningDueTime - Duration.FromTimeSpan(TimeSpan.FromMilliseconds(250)) <
            blockTime + blockExecutionDuration)
        {
            Logger.LogDebug(
                $"Mining time not enough. MiningDueTime: {miningDueTime}, BlockTime: {blockTime}, Duration: {blockExecutionDuration}");

            if (miningDueTime < blockTime + Duration.FromTimeSpan(TimeSpan.FromMilliseconds(250)))
                return false;
                
            blockExecutionDuration = miningDueTime - Duration.FromTimeSpan(TimeSpan.FromMilliseconds(250)) - blockTime;
            return true;
        }

        if (blockTime + blockExecutionDuration >= TimestampHelper.GetUtcNow()) return true;
        Logger.LogDebug($"Will cancel mining due to timeout: Actual mining time: {blockTime}, " +
                        $"execution limit: {blockExecutionDuration.Milliseconds()} ms.");
        return false;
    }

    private Duration CalculateBlockMiningDuration(Timestamp blockTime, Duration expectedDuration)
    {
        return blockTime + expectedDuration - TimestampHelper.GetUtcNow();
    }
}