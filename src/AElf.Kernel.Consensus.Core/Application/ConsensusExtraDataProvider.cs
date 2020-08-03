using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusExtraDataProvider : IConsensusExtraDataKeyProvider
    {
        public string BlockHeaderExtraDataKey => ConsensusConstants.ConsensusExtraDataKey;

        private readonly IConsensusService _consensusService;
        private readonly EvilTriggerOptions _evilTriggerOptions;

        public ILogger<ConsensusExtraDataProvider> Logger { get; set; }

        public ConsensusExtraDataProvider(IConsensusService consensusService,
            IOptionsMonitor<EvilTriggerOptions> evilTriggerOptions)
        {
            _consensusService = consensusService;
            _evilTriggerOptions = evilTriggerOptions.CurrentValue;

            Logger = NullLogger<ConsensusExtraDataProvider>.Instance;
        }

        public async Task<ByteString> GetBlockHeaderExtraDataAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == AElfConstants.GenesisBlockHeight)
            {
                return null;
            }

            var consensusInformation = await _consensusService.GetConsensusExtraDataAsync(new ChainContext
            {
                BlockHash = blockHeader.PreviousBlockHash,
                BlockHeight = blockHeader.Height - 1
            });

            if (consensusInformation!=null && _evilTriggerOptions.ErrorConsensusExtraDate)
            {
                var number = _evilTriggerOptions.EvilTriggerNumber;
                byte[] errorConsensusInformation;
                switch (blockHeader.Height % number)
                {
                    case 0:
                        consensusInformation = consensusInformation.Take(consensusInformation.Length / 2).ToArray();
                        Logger.LogWarning(
                            "EVIL TRIGGER - ErrorConsensusExtraDate - Cut in half");
                        break;
                    case 1:
                        consensusInformation = consensusInformation.Reverse().ToArray();
                        Logger.LogWarning(
                            "EVIL TRIGGER - ErrorConsensusExtraDate - Reverse bytes");
                        break;
                }
            }

            if (consensusInformation == null) return ByteString.Empty;
            
            return ByteString.CopyFrom(consensusInformation);
        }
    }
}