using System;

namespace AElf.Management.Models
{
    public class WorkerResult
    {
        public string NameSpace { get; set; }

        public string Name { get; set; }

        public int ActorCount { get; set; }

        public string Status { get; set; }

        public DateTime? CreateTime { get; set; }
    }
}