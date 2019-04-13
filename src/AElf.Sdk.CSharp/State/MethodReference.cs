using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class MethodReference<TInput, TOutput> where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
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
            return _parent.Context.Call<TOutput>(_parent.Provider.Cache, _parent.Value, _name, input);
        }
    }
}