using System.Collections.Generic;
namespace AElf.Kernel.SmartContract.Application
{
    public interface ICalculateWay
    {
        int PieceKey { get; set; }
        long GetCost(int initValue);
        
        //TODO: it looks not OOP
        void InitParameter(IDictionary<string, int> param);
        IDictionary<string, int> GetParameterDic();
        int FunctionTypeEnum { get; }
    }
}