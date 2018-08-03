using System;
using System.Collections.Generic;
using AElf.Deployment.Handler;
using AElf.Deployment.Helper;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace AElf.Deployment
{
    public class DeployService : IDeployService
    {
        public void DeploySideChain()
        {
            var type = "k8s";
            var handler = DeployHandlerFactory.GetHandler(type);
            handler.Execute();
        }
    }
}