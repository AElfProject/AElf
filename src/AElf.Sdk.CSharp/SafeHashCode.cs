using System.Text;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public static class SafeHashCode
    {
        // Called from contracts after patching
        public static int GetHashCode(object obj)
        {
            switch (obj)
            {
                case string str:
                    return GetHashCode(str);
                
                case IMessage _:
                {
                    // Call overriden method from the message implementation
                    var ret = obj.GetType().GetMethod("GetHashCode")
                        ?.Invoke(obj, new object[0]);

                    // If no overriden method, then return 0
                    return ret != null ? (int) ret : 0;
                }
                
                case ByteString bString:
                    return bString.GetHashCode();
                
                default:
                    // If an usual type, return 0
                    return 0;
            }
        }
        
        private static int GetHashCode(string str)
        {
            var bytes = Encoding.Unicode.GetBytes(str);
            
            var ret = 23;
            foreach (var b in bytes)
            {
                ret = (ret * 31) + b;
            }
            
            return ret;
        }
    }
}