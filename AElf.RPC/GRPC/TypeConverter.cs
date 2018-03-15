using System;

namespace AElf.RPC
{
    public class TypeConverter
    {
        public static object Convert(Parameter p)
        {
            object obj = null;
            if (p.Type == 0)
            {
                var b = new byte[4];
                p.Data.ToByteArray().CopyTo(b, 0);
                obj = BitConverter.ToInt32(b, 0);
            }

            return obj;
        }

        public static object[] Convert(ParamList paramList)
        {
            var objs = new object[paramList.Param.Count];
            for (var i = 0; i < objs.Length; i++)
            {
                var p = paramList.Param[i];
                objs[i] = Convert(p);
            }

            return objs;
        }
    }
}