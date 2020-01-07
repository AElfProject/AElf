using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicUpdate
{
    public partial class BasicUpdateContract
    {
        public override Int32Value GetHashCodeBytesValue(BytesValue input)
        {
            var result = input.GetHashCode();
            Context.LogDebug(()=>$"## GetHashCodeBytesValue {input.Value.ToBase64()} => {result}");
            
            return new Int32Value
            {
                Value = result
            };
        }

        public override Int32Value GetHashCodeInt32Value(Int32Value input)
        {
            var result = input.GetHashCode();
            Context.LogDebug(()=>$"## GetHashCodeInt32Value {input.Value} => {result}");
            
            return new Int32Value
            {
                Value = result
            };
        }

        public override Int32Value GetHashCodeInt64Value(Int64Value input)
        {
            var result = input.GetHashCode();
            Context.LogDebug(()=>$"## GetHashCodeInt64Value {input.Value} => {result}");
            
            return new Int32Value
            {
                Value = result
            };
        }

        public override Int32Value GetHashCodeEnumValue(EnumInput input)
        {
            var result = input.Info.GetHashCode();
            Context.LogDebug(()=>$"## GetHashCodeEnumValue {input.Info.ToString()} => {result}");
            
            return new Int32Value
            {
                Value = result
            };
        }

        public override Int32Value GetHashCodeStringValue(StringValue input)
        {
            var result = input.GetHashCode();
            Context.LogDebug(()=>$"## GetHashCodeStringValue {input.Value} => {result}");
            
            return new Int32Value
            {
                Value = result
            };
        }

        public override Int32Value GetHashCodeComplexValue(ComplexInput input)
        {
            var result = input.GetHashCode();
            Context.LogDebug(()=>$"## GetHashCodeComplexValue {input} => {result}");
            
            return new Int32Value
            {
                Value = result
            };
        }
    }
}