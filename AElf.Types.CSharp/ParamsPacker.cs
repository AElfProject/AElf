using System;
using System.IO;
using System.Linq;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public class ParamsPacker
    {
        public static byte[] Pack(params object[] objs)
        {
            if (objs.Length == 0)
                return new byte[] { };
            // Put plain clr data in Pb types.
            if (!objs.All(o => o.GetType().IsAllowedType()))
            {
                throw new Exception("Contains invalid type.");
            }

            using (MemoryStream mm = new MemoryStream())
            using (CodedOutputStream stream = new CodedOutputStream(mm))
            {
                int fieldNumber = 1;
                foreach (var o in objs)
                {
                    stream.WriteRawTag((byte) o.GetTagForFieldNumber(fieldNumber));
                    o.WriteToStream(stream);
                    fieldNumber++;
                }

                stream.Flush();
                mm.Position = 0;
                return mm.ToArray();
            }
        }

        public static object[] Unpack(byte[] bytes, Type[] types)
        {
            if (bytes.Length == 0)
                return new object[] { };
            if (types.Length * bytes.Length == 0)
            {
                throw new Exception("Invalid input.");
            }

            var objs = new object[types.Length];
            using (CodedInputStream stream = new CodedInputStream(bytes))
            {
                for (int i = 0; i < types.Length; i++)
                {
                    stream.ReadTag();
                    objs[i] = types[i].ReadFromStream(stream);
                }
            }

            return objs;
        }
    }
}