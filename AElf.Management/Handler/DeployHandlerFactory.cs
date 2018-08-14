using System;

namespace AElf.Management.Handler
{
    public class DeployHandlerFactory
    {
        public static IDeployHandler GetHandler(string type)
        {
            type = type.ToLower();
            switch (type)
            {
                case "k8s":
                    return K8SDeployHandler.Instance;
                default:
                    throw new Exception();
            }
        }

    }
}