using System.IO;
using AElf.Common.Application;
using AElf.Configuration;
using AElf.Configuration.Config.Management;
using AElf.Management.Models;

namespace AElf.Management.Commands
{
    public class SaveApiKeyCommand:IDeployCommand
    {
        public void Action(DeployArg arg)
        {
            // Todo temp solution 
            ApiKeyConfig.Instance.ChainKeys.Add(arg.SideChainId, arg.ApiKey);
            var configJson =  JsonSerializer.Instance.Serialize(ApiKeyConfig.Instance);
            File.WriteAllText(Path.Combine(ApplicationHelpers.GetDefaultConfigPath(), "config", "api-key.json"), configJson);
        }
    }
}