using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace AElf.Test.ChainCreator
{
    public class AccountStateInitializer
    {
        public MapField<string, ulong> Balances(int count)
        {
            var map = new MapField<string, ulong>();
            while (count -- > 0)
            {
                map.Add(Hash.Generate().ToAccount().ToByteString()., 100);
            }
            return map;
        }
    }
}