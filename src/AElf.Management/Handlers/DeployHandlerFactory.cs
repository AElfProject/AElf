using System;

namespace AElf.Management.Handlers
{
    public class DeployHandlerFactory
    {
        public static IDeployHandler GetHandler(string type)
        {
            switch (type.ToLower())
            {
                case "k8s":
                    return K8SDeployHandler.Instance;
                default:
                    throw new ArgumentException("deploy type is incorrect");
            }
        }
    }
}