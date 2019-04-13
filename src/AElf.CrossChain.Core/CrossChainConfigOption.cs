using System;
using System.Collections.Generic;

namespace AElf.CrossChain
{
    public class CrossChainConfigOption
    {
        public int ParentChainId { get; set; }
        public List<string> ExtraDataSymbols { get; set; }
    }
}