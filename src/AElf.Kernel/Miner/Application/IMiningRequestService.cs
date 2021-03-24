using System;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Miner.Application
{
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
        public ILogger<MiningRequestService> Logger { get; set; }

        public MiningRequestService(IMinerService minerService)
        {
            _minerService = minerService;
        }

        public async Task<Block> RequestMiningAsync(ConsensusRequestMiningDto requestMiningDto)
        {
            var dur = requestMiningDto.BlockExecutionTime;
            if (!ValidateBlockMiningTime(requestMiningDto.BlockTime, requestMiningDto.MiningDueTime,
                ref dur))
                return null;
            
            var blockExecutionDuration =
                CalculateBlockMiningDuration(requestMiningDto.BlockTime, dur);

            var block = (await _minerService.MineAsync(requestMiningDto.PreviousBlockHash,
                requestMiningDto.PreviousBlockHeight, requestMiningDto.BlockTime, blockExecutionDuration)).Block;

            return block;
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
}