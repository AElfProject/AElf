using System.IO;
using System.Threading.Tasks;
using AElf.Common.Application;
using AElf.Configuration;
using AElf.Configuration.Config.Management;
using AElf.Management.Models;

namespace AElf.Management.Commands
{
    public class SaveApiKeyCommand : IDeployCommand
    {
        public async Task Action(DeployArg arg)
        {
            // Todo temp solution 
            ApiKeyConfig.Instance.ChainKeys.Add(arg.SideChainId, arg.ApiKey);
            var configJson = JsonSerializer.Instance.Serialize(ApiKeyConfig.Instance);
            using (var sw = new StreamWriter(Path.Combine(ApplicationHelpers.ConfigPath, "config", "api-key.json")))
            {
                await sw.WriteAsync(configJson);
            }
        }
    }
}