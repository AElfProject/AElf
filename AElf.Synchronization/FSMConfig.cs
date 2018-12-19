using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;

namespace AElf.Synchronization
{
    // ReSharper disable once InconsistentNaming
    public class FSMConfig
    {
        private List<NodeState> States => Enum.GetValues(typeof(NodeState)).Cast<NodeState>().ToList();
    }
}