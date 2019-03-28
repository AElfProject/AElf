using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class ActionInfo
    {
        private readonly string _name;
        private readonly ContractReferenceState _owner;

        public ActionInfo(ContractReferenceState owner, string name)
        {
            _owner = owner;
            _name = name;
        }

        internal void Send(params object[] args)
        {
            _owner.Context.SendInline(_owner.Value, _name, ByteString.CopyFrom(ParamsPacker.Pack(args)));
        }
    }
}