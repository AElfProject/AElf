using System;
using AElf.CSharp.Core.Extension;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner.Application;

public interface IMiningRequestService
{
    Task<Block> RequestMiningAsync(ConsensusRequestMiningDto requestMiningDto);
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

    public MiningRequestService(IMinerService minerService)
    {
        _minerService = minerService;
    }

    public ILogger<MiningRequestService> Logger { get; set; }

    public async Task<Block> RequestMiningAsync(ConsensusRequestMiningDto requestMiningDto)
    {
        if (!ValidateBlockMiningTime(requestMiningDto.BlockTime, requestMiningDto.MiningDueTime,
                requestMiningDto.BlockExecutionTime))
            return null;

        var blockExecutionDuration =
            CalculateBlockMiningDuration(requestMiningDto.BlockTime, requestMiningDto.BlockExecutionTime);

        var block = (await _minerService.MineAsync(requestMiningDto.PreviousBlockHash,
            requestMiningDto.PreviousBlockHeight, requestMiningDto.BlockTime, blockExecutionDuration)).Block;

        return block;
    }

    private bool ValidateBlockMiningTime(Timestamp blockTime, Timestamp miningDueTime,
        Duration blockExecutionDuration)
    {
        if (miningDueTime - Duration.FromTimeSpan(TimeSpan.FromMilliseconds(250)) <
            blockTime + blockExecutionDuration)
        {
            Logger.LogDebug(
                "Mining canceled because mining time slot expired. MiningDueTime: {MiningDueTime}, BlockTime: {BlockTime}, Duration: {BlockExecutionDuration}",
                miningDueTime, blockTime, blockExecutionDuration);
            return false;
        }

        if (blockTime + blockExecutionDuration >= TimestampHelper.GetUtcNow()) return true;
        Logger.LogDebug(
            "Will cancel mining due to timeout: Actual mining time: {BlockTime}, execution limit: {BlockExecutionDuration} ms",
            blockTime, blockExecutionDuration.Milliseconds());
        return false;
    }

    private Duration CalculateBlockMiningDuration(Timestamp blockTime, Duration expectedDuration)
    {
        return blockTime + expectedDuration - TimestampHelper.GetUtcNow();
    }
}