using AElf.Types;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class MethodReference<TInput, TOutput> where TInput : class, IMessage<TInput>, new()
        where TOutput : class, IMessage<TOutput>, new()
    {
        private readonly ContractReferenceState _parent;
        private readonly string _name;

        public MethodReference(ContractReferenceState parent, string name)
        {
            _parent = parent;
            _name = name;
        }

        public void Send(TInput input)
        {
            _parent.Context.SendInline(_parent.Value, _name, input);
        }

        public TOutput Call(TInput input)
        {
            return _parent.Context.Call<TOutput>(_parent.Value, _name, input);
        }

        public TOutput Call(Address fromAddress, TInput input)
        {
            return _parent.Context.Call<TOutput>(fromAddress, _parent.Value, _name, input);
        }

        public void VirtualSend(Hash virtualAddress, TInput input)
        {
            _parent.Context.SendVirtualInline(virtualAddress, _parent.Value, _name, input);
        }

        public TOutput VirtualCall(Hash virtualAddress, TInput input)
        {
            return _parent.Context.Call<TOutput>(_parent.Context.ConvertVirtualAddressToContractAddress(virtualAddress),
                _parent.Value, _name, input);
        }
    }
}