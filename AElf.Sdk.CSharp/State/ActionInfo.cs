using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using AElf.Common;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;

namespace AElf.Sdk.CSharp.State
{
    public class ActionInfo
    {
        private readonly ContractReferenceState _owner;
        private readonly string _name;

        public ActionInfo(ContractReferenceState owner, string name)
        {
            _owner = owner;
            _name = name;
        }

        internal void Send(params object[] args)
        {
            _owner.Context.SendInline(_owner.Value, _name, args);
        }
    }
}