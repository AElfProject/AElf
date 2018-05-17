using System;

namespace AElf.Kernel
{
    public static class ParamsHandler
    {
        public static object Value(this Param param)
        {
            object val = null;
            switch (param.DataCase)
            {
                case Param.DataOneofCase.IntVal:
                    val = param.IntVal;
                    break;
                case Param.DataOneofCase.StrVal:
                    val = param.StrVal;
                    break;
                case Param.DataOneofCase.HashVal:
                    val = param.HashVal;
                    break;
                case Param.DataOneofCase.DVal:
                    val = param.DVal;
                    break;
                case Param.DataOneofCase.LongVal:
                    val = param.LongVal;
                    break;
                case Param.DataOneofCase.RegisterVal:
                    val = param.RegisterVal;
                    break;
                case Param.DataOneofCase.DeploymentVal:
                    val = param.DeploymentVal;
                    break;
                case Param.DataOneofCase.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return val;
        }
    }
}