using System;
using System.Collections.Generic;
using AElf.Common;
using NServiceKit.Common.Extensions;

namespace AElf.Synchronization
{
    // ReSharper disable once InconsistentNaming
    public class FSMConfig
    {
        private List<NodeState> States => Enum.GetValues(typeof(NodeState)).ToList<NodeState>();

    }
}