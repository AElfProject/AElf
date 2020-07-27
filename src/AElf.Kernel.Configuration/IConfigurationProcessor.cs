using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Configuration
{
    public interface IConfigurationProcessor
    {
        string ConfigurationName { get; }
        Task ProcessConfigurationAsync(ByteString byteString, BlockIndex blockIndex);
    }
}