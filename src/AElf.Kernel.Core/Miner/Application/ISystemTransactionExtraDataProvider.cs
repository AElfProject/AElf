using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Miner.Application
{
    public interface ISystemTransactionExtraDataProvider
    {
        bool TryGetSystemTransactionCount(BlockHeader blockHeader, out int count);

        void SetSystemTransactionCount(int count, BlockHeader blockHeader);
    }

    public class SystemTransactionExtraDataProvider : ISystemTransactionExtraDataProvider, ISingletonDependency
    {
        private const string BlockHeaderExtraDataKey = "SystemTransactionCount";
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly EvilTriggerOptions _evilTriggerOptions;
        public ILogger<SystemTransactionExtraDataProvider> Logger { get; set; }

        public SystemTransactionExtraDataProvider(IBlockExtraDataService blockExtraDataService,
            IOptionsMonitor<EvilTriggerOptions> evilTriggerOptions)
        {
            _blockExtraDataService = blockExtraDataService;
            _evilTriggerOptions = evilTriggerOptions.CurrentValue;
            Logger = NullLogger<SystemTransactionExtraDataProvider>.Instance;
        }

        public bool TryGetSystemTransactionCount(BlockHeader blockHeader, out int count)
        {
            count = 0;
            var byteString = _blockExtraDataService.GetExtraDataFromBlockHeader(BlockHeaderExtraDataKey, blockHeader);
            if (byteString == null) return false;
            count = Int32Value.Parser.ParseFrom(byteString).Value;
            return true;
        }

        public void SetSystemTransactionCount(int count, BlockHeader blockHeader)
        {
            if (_evilTriggerOptions.ErrorSystemTransactionCount)
            {
                var number = _evilTriggerOptions.EvilTriggerNumber;
                var origin = count;
                switch (blockHeader.Height % number)
                {
                    case 0:
                        count = count - 1;
                        Logger.LogWarning(
                            $"EVIL TRIGGER - ErrorSystemTransactionCount - {origin} -> {count} ");
                        break;
                    case 1:
                        count = count + 1;
                        Logger.LogWarning(
                            $"EVIL TRIGGER - ErrorSystemTransactionCount - {origin} -> {count} ");
                        break;
                    case 2:
                        count = 0;
                        Logger.LogWarning(
                            $"EVIL TRIGGER - ErrorSystemTransactionCount - {origin} -> {count}");
                        break;
                }
            }

            blockHeader.ExtraData.Add(BlockHeaderExtraDataKey, new Int32Value {Value = count}.ToByteString());
        }
    }
}