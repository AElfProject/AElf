using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
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

[Ump]
public class MiningRequestService : IMiningRequestService
{
    private readonly IMinerService _minerService;
    private readonly Counter<long> _miningDurationCounter;
    private readonly Counter<long> _timeslotDurationCounter;

    public MiningRequestService(IMinerService minerService, Instrumentation instrumentation)
    {
        _minerService = minerService;
        _miningDurationCounter = instrumentation.MiningDurationCounter;
        _timeslotDurationCounter = instrumentation.TimeslotDurationCounter;

        Logger = NullLogger<MiningRequestService>.Instance;
    }

    public ILogger<MiningRequestService> Logger { get; set; }

    [Ump]
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
        var duration = blockTime + expectedDuration - TimestampHelper.GetUtcNow();
        Logger.LogCritical($"Mining duration: {duration}");
        _miningDurationCounter.Add(duration.Milliseconds());
        _timeslotDurationCounter.Add(500);
        return duration;
    }
}