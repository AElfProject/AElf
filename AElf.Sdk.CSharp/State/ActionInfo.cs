using AElf.Types.CSharp;
using Google.Protobuf;

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
            _owner.Context.SendInline(_owner.Value, _name, ByteString.CopyFrom(ParamsPacker.Pack(args)));
        }
    }
}