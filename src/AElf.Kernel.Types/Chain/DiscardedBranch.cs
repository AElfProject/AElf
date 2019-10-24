using System.Collections.Generic;

namespace AElf.Kernel
{
    public class DiscardedBranch
    {
        public List<string> BranchKeys { get; set; }

        public List<string> NotLinkedKeys { get; set; }
    }
}