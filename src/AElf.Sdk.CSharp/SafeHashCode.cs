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
                // If it is a string; 
                case string str:
                    // call deterministic GetHashCode for strings
                    return GetHashCode(str);
                
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