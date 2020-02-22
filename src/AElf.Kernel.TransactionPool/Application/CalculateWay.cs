using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
//TODO: should not implement here

namespace AElf.Kernel.TransactionPool.Application
{
    #region ICalculateWay implemention   
    public class PowerCalculateWay : ICalculateWay
    {
        public int PieceKey { get; set; }
        public int Power { get; set; } = 2;
        public int ChangeSpanBase { get; set; } = 1;
        public int Weight { get; set; }
        public int WeightBase { get; set; }
        public long Precision { get; set; } = 100000000L;
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;
        public int FunctionTypeEnum { get; } = (int) CalculateFunctionTypeEnum.Power;

        public void InitParameter(IDictionary<string, int> param)
        {
            param.TryGetValue(nameof(Power).ToLower(), out var power);
            param.TryGetValue(nameof(ChangeSpanBase).ToLower(), out var changeSpanBase);
            param.TryGetValue(nameof(Weight).ToLower(), out var weight);
            param.TryGetValue(nameof(WeightBase).ToLower(), out var weightBase);
            param.TryGetValue(nameof(Numerator).ToLower(), out var numerator);
            param.TryGetValue(nameof(Denominator).ToLower(), out var denominator);
            ChangeSpanBase = changeSpanBase;
            Weight = weight;
            WeightBase = weightBase;
            Numerator = numerator;
            Denominator = denominator;
            Power = power;
        }

        public long GetCost(int cost)
        {
            return ((long) (Math.Pow((double) cost / ChangeSpanBase, Power) * Precision)).Mul(Weight).Div(WeightBase)
                .Add(Precision.Mul(Numerator).Div(Denominator).Mul(cost));
        }

        public IDictionary<string, int> GetParameterDic()
        {
            var paraDic = new Dictionary<string, int>
            {
                [nameof(Power).ToLower()] = (int) Power,
                [nameof(ChangeSpanBase).ToLower()] = ChangeSpanBase,
                [nameof(Weight).ToLower()] = Weight,
                [nameof(WeightBase).ToLower()] = WeightBase,
                [nameof(Numerator).ToLower()] = Numerator,
                [nameof(Denominator).ToLower()] = Denominator
            };
            return paraDic;
        }
    }
    public class LinerCalculateWay : ICalculateWay
    {
        public int PieceKey { get; set; }
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;
        public int ConstantValue { get; set; }
        public long Precision { get; set; } = 100000000L;
        public int FunctionTypeEnum { get; } = (int) CalculateFunctionTypeEnum.Liner;

        public void InitParameter(IDictionary<string, int> param)
        {
            param.TryGetValue(nameof(Numerator).ToLower(), out var numerator);
            param.TryGetValue(nameof(Denominator).ToLower(), out var denominator);
            param.TryGetValue(nameof(ConstantValue).ToLower(), out var constantValue);
            Numerator = numerator;
            Denominator = denominator;
            ConstantValue = constantValue;
        }

        public long GetCost(int cost)
        {
            return Precision.Mul(cost).Mul(Numerator).Div(Denominator).Add(ConstantValue);
        }

        public IDictionary<string, int> GetParameterDic()
        {
            var paraDic = new Dictionary<string, int>
            {
                [nameof(ConstantValue).ToLower()] = ConstantValue,
                [nameof(Numerator).ToLower()] = Numerator,
                [nameof(Denominator).ToLower()] = Denominator
            };
            return paraDic;
        }
    }

    #endregion
}