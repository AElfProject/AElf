using AElf.Management.Database;
using AElf.Management.Models;

namespace AElf.Management.Commands
{
    public class AddMonitorDBCommand:IDeployCommand
    {
        public void Action(DeployArg arg)
        {
            InfluxDBHelper.AddDatabase(arg.SideChainId);
        }
    }
}