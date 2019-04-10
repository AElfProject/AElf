using System.Collections.Generic;

namespace AElf.Management.Models
{
    public class DeployTestChainResult
    {
        public string ChainId { get; set; }

        public int NodePort { get; set; }

        public int RpcPort { get; set; }

        public List<string> NodeHost { get; set; }
    }
}