using System.Threading.Tasks;
using AElf.Management.Helper;
using AElf.Management.Models;

namespace AElf.Management.Commands
{
    public class AddMonitorDbCommand : IDeployCommand
    {
        public async Task Action(DeployArg arg)
        {
            await InfluxDBHelper.CreateDatabase(arg.SideChainId);
        }
    }
}