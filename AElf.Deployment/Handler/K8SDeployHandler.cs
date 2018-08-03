namespace AElf.Deployment.Handler
{
    public class K8SDeployHandler : IDeployHandler
    {
        private static readonly IDeployHandler _instance = new K8SDeployHandler();
        public static IDeployHandler Instance
        {
            get { return _instance; }
        }

        private K8SDeployHandler()
        {
        }
        
        public void Execute()
        {
            // deploy db service and statefulset
            
            // deploy node  service and deployment
            
            
            throw new System.NotImplementedException();
        }
    }
}