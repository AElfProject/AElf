using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Cli.Core
{
    public class AElfCliService
    {
        public ILogger<AElfCliService> Logger { get; set; }

        public AElfCliService()
        {
            Logger = NullLogger<AElfCliService>.Instance;
        }

        public async Task RunAsync(string[] args)
        {

        }
    }
}