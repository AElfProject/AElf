using System;
using System.Linq;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public partial class ParamsHolder
    {
        /// <summary>
        /// Pack plain CLR type data into Protobuf messages.
        /// </summary>
        /// <returns>Packed <see cref="ParamsHolder"/> message.</returns>
        /// <param name="objs">Objects of plain CLR types.</param>
        public static ParamsHolder Pack(params object[] objs)
        {
            if (objs.Length == 0)
                return new ParamsHolder();
            // Put plain clr data in Pb types.
            if (!objs.All(o => o.GetType().IsAllowedType()))
            {
                throw new Exception("Contains invalid type.");
            }
            var ph = new ParamsHolder();
            foreach (var o in objs)
            {
                if (o is bool)
                {
                    ph.Params.Add(((bool)o).ToAny());
                }
                else if (o is int)
                {
                    ph.Params.Add(((int)o).ToAny());
                }
                else if (o is uint)
                {
                    ph.Params.Add(((uint)o).ToAny());
                }
                else if (o is long)
                {
                    ph.Params.Add(((long)o).ToAny());
                }
                else if (o is ulong)
                {
                    ph.Params.Add(((ulong)o).ToAny());
                }
                else if (o is string)
                {
                    ph.Params.Add(((string)o).ToAny());
                }
                else if (o is byte[])
                {
                    ph.Params.Add(((byte[])o).ToAny());
                }
                else if (o is IMessage)
                {
                    ph.Params.Add(((IMessage)o).ToAny());
                }
                else if (o is UserType)
                {
                    ph.Params.Add(((UserType)o).ToAny());
                }
            }
            return ph;
        }

        public object[] Unpack(params Type[] types)
        {
            if (types.Length != Params.Count)
            {
                throw new Exception("Length doesn't match.");
            }
            if (types.Length == 0)
                return new object[0];
            if (!types.All(t => t.IsAllowedType()))
            {
                throw new Exception("Contains invalid type.");
            }
            object[] objs = new object[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == typeof(bool))
                {
                    objs[i] = Params[i].AnyToBool();
                }
                else if (types[i] == typeof(int))
                {
                    objs[i] = Params[i].AnyToInt32();
                }
                else if (types[i] == typeof(uint))
                {
                    objs[i] = Params[i].AnyToUInt32();
                }
                else if (types[i] == typeof(long))
                {
                    objs[i] = Params[i].AnyToInt64();
                }
                else if (types[i] == typeof(ulong))
                {
                    objs[i] = Params[i].AnyToUInt64();
                }
                else if (types[i] == typeof(string))
                {
                    objs[i] = Params[i].AnyToString();
                }
                else if (types[i] == typeof(byte[]))
                {
                    objs[i] = Params[i].AnyToBytes();
                }
                else if (types[i].IsPbMessageType())
                {
                    objs[i] = Params[i].AnyToPbMessage(types[i]);
                }
                else if (types[i].IsUserType())
                {
                    objs[i] = Params[i].AnyToUserType(types[i]);
                }
            }
            return objs;
        }
    }
}
