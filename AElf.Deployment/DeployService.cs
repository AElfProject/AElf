using System;
using System.Collections.Generic;
using AElf.Deployment.Handler;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace AElf.Deployment
{
    public class DeployService : IDeployService
    {
        public void DeploySideChain(string chainId, DeployArg arg)
        {
            GetHandler().Excute(DeployType.Deploy, chainId, arg);
        }

        public void RemoveSideChain(string chainId)
        {
            GetHandler().Excute(DeployType.Remove, chainId);

            string a = $"dfef{chainId}";
        }

        private IDeployHandler GetHandler()
        {
            var type = "k8s";
            return DeployHandlerFactory.GetHandler(type);
        }
    }
}