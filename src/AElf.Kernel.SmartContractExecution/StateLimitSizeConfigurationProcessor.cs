using System.Threading.Tasks;
using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContractExecution
{
    public class StateLimitSizeConfigurationProcessor : IConfigurationProcessor
    {
        private readonly IStateSizeLimitProvider _stateSizeLimitProvider;

        public StateLimitSizeConfigurationProcessor(IStateSizeLimitProvider stateSizeLimitProvider)
        {
            _stateSizeLimitProvider = stateSizeLimitProvider;
        }

        public string ConfigurationName => "StateSizeLimit";
        
        public async Task ProcessConfigurationAsync(ByteString byteString, BlockIndex blockIndex)
        {
            var limit = Int32Value.Parser.ParseFrom(byteString).Value;
            await _stateSizeLimitProvider.SetStateSizeLimitAsync(blockIndex, limit);
        }
    }
}