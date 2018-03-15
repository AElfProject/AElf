using System;
using Google.Protobuf;

namespace AElf.RPC
{
    public partial class ParamList
    {
        public void SetParam(object[] objs)
        {
            param_.Clear();
            foreach (var t in objs)
            {
                var parameter = new Parameter();
                parameter.Data = ByteString.CopyFrom(Convert.ToByte(t));
                param_.Add(parameter);
            }
        }
    }
}