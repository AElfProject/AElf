using System.Text;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public static class SafeHashCode
    {
        // Called from contracts after patching
        public static int GetHashCode(object obj)
        {
            switch (obj)
            {
                // If it is a string; 
                case string str:
                    // call deterministic GetHashCode for strings
                    return GetHashCode(str);
                
                case StringValue str: 
                    return GetHashCode(str.Value);
                
                // If it is a IMessage inherited type;
                case IMessage _:
                {
                    // Call overriden method from the message implementation
                    var ret = obj.GetType().GetMethod("GetHashCode")
                        ?.Invoke(obj, new object[0]);

                    // If no overriden method, then return 0
                    return ret != null ? (int) ret : 0;
                }
                
                // If it is a byte string;
                case ByteString bString:
                    // It is ok to call protobuf implemented one, since it is also deterministic
                    return bString.GetHashCode();
                
                // If it is an unusual type
                default:
                    // Just return a fixed value
                    return 0;
            }
        }
        
        private static int GetHashCode(string str)
        {
            unchecked
            {
                var hash1 = 5381;
                var hash2 = hash1;

                for(var i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}