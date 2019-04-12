using System.Collections.Generic;
using AElf.Management.Request;
using Newtonsoft.Json;

namespace AElf.Management.Models
{
    public class ActorStateResult
    {
        public string Roles { get; set; }

        public string Address { get; set; }

        public string Status { get; set; }

        public bool Reachable { get; set; }

        public bool ClusterLeader { get; set; }

        public bool RoleLeader { get; set; }
    }
}