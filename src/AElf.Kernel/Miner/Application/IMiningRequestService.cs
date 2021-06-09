using System;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Miner.Application
{
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
            if (!ValidateBlockMiningTime(requestMiningDto.BlockTime, requestMiningDto.MiningDueTime,
                requestMiningDto.BlockExecutionTime))
                return null;

            var blockExecutionDuration =
                CalculateBlockMiningDuration(requestMiningDto.BlockTime, requestMiningDto.BlockExecutionTime);

            var blockExecutedSet = await _minerService.MineAsync(requestMiningDto.PreviousBlockHash,
                requestMiningDto.PreviousBlockHeight, requestMiningDto.BlockTime, blockExecutionDuration);

            Logger.LogTrace("End MiningRequestService.RequestMiningAsync");
            return blockExecutedSet;
        }

        private bool ValidateBlockMiningTime(Timestamp blockTime, Timestamp miningDueTime,
            Duration blockExecutionDuration)
        {
            if (miningDueTime - Duration.FromTimeSpan(TimeSpan.FromMilliseconds(250)) <
                blockTime + blockExecutionDuration)
            {
                Logger.LogDebug(
                    $"Mining canceled because mining time slot expired. MiningDueTime: {miningDueTime}, BlockTime: {blockTime}, Duration: {blockExecutionDuration}");
                return false;
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
}