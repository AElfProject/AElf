using AElf.Management.Helper;
using AElf.Management.Models;

namespace AElf.Management.Commands
{
    public class AddMonitorDBCommand:IDeployCommand
    {
        public void Action(DeployArg arg)
        {
            InfluxDBHelper.CreateDatabase(arg.SideChainId);
        }
    }
}