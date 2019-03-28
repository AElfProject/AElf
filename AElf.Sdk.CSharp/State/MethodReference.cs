using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class MethodReference<TInput, TOutput> where TInput : IMessage<TInput> where TOutput : IMessage<TOutput>
    {
        private readonly string _name;
        private readonly ContractReferenceState _parent;

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