namespace AElf.Deployment.Models
{
    public class DeployArg
    {
        public DeployDBArg DBArg { get; set; }

        public DeployArg()
        {
            DBArg=new DeployDBArg();
        }
    }

    public class DeployDBArg
    {
        public int Port { get; set; }

        public DeployDBArg()
        {
            Port = 7001;
        }
    }
}