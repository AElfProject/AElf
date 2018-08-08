namespace AElf.Deployment.Models
{
    public class DeployArgument
    {
        public string ChainId { get; set; }

        public DeployDBArgument DbArgument { get; set; }


        public DeployArgument()
        {
            DbArgument=new DeployDBArgument();
        }
    }

    public class DeployDBArgument
    {
        public int Port { get; set; }

        public DeployDBArgument()
        {
            Port = 7001;
        }
    }
}