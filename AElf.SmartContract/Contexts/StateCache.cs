using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public class StateCache
    {
        public byte[] CurrentValue { get; set; }
        
        public StateCache(byte[] currentValue)
        {
            CurrentValue = currentValue;
        }
    }
}