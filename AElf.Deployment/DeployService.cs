using System;
using System.Collections.Generic;
using AElf.Deployment.Handler;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace AElf.Deployment
{
    public class SideChainService : ISideChainService
    {
        public void Deploy(string chainId, DeployArg arg)
        {
            GetHandler().Execute(DeployType.Deploy, chainId, arg);
        }

        public void Remove(string chainId)
        {
            GetHandler().Execute(DeployType.Remove, chainId);
        }

        private IDeployHandler GetHandler()
        {
            var type = "k8s";
            return DeployHandlerFactory.GetHandler(type);
        }
    }
}