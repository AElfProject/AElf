using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.RandomNumberProvider
{
    // ReSharper disable InconsistentNaming
    public class RandomNumberProviderContract : RandomNumberProviderContractContainer.RandomNumberProviderContractBase
    {
        public override BytesValue GetRandomBytes(BytesValue input)
        {
            var serializedInput = new GetRandomBytesInput();
            serializedInput.MergeFrom(input.Value);
            var value = new Hash();
            value.MergeFrom(serializedInput.Value);
            var randomHashFromContext = Context.GetRandomHash(value);

            return new BytesValue
            {
                Value = serializedInput.Kind == 1
                    ? new BytesValue {Value = randomHashFromContext.Value}.ToByteString()
                    : new Int64Value {Value = Context.ConvertHashToInt64(randomHashFromContext, 1, 10000)}.ToByteString()
            };
        }
    }
}