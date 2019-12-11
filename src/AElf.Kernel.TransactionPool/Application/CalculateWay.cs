using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;

namespace AElf.Kernel.TransactionPool.Application
{
    #region ICalculateWay implemention   

    public class LnCalculateWay : ICalculateWay
    {
        public int ChangeSpanBase { get; set; }
        public int Weight { get; set; }
        public int WeightBase { get; set; }
        public long Precision { get; set; } = 100000000L;
        public int FunctionTypeEnum { get; } = (int) CalculateFunctionTypeEnum.Ln;

        public bool TryInitParameter(IDictionary<string, int> param)
        {
            param.TryGetValue(nameof(ChangeSpanBase).ToLower(), out var changeSpanBase);
            if (changeSpanBase <= 0)
                return false;
            param.TryGetValue(nameof(Weight).ToLower(), out var weight);
            if (weight <= 0)
                return false;
            param.TryGetValue(nameof(WeightBase).ToLower(), out var weightBase);
            if (weightBase <= 0)
                return false;
            ChangeSpanBase = changeSpanBase;
            Weight = weight;
            WeightBase = weightBase;
            return true;
        }

        public long GetCost(int cost)
        {
            int diff = cost + 1;
            double weightChange = (double) diff / ChangeSpanBase;
            double unitValue = (double) Weight / WeightBase;
            if (weightChange <= 1)
                return 0;
            return Precision.Mul((long) (weightChange * unitValue * Math.Log(weightChange, Math.E)));
        }

        public IDictionary<string, int> GetParameterDic()
        {
            var paraDic = new Dictionary<string, int>
            {
                [nameof(ChangeSpanBase).ToLower()] = ChangeSpanBase,
                [nameof(Weight).ToLower()] = Weight,
                [nameof(WeightBase).ToLower()] = WeightBase
            };
            return paraDic;
        }
    }

    public class PowerCalculateWay : ICalculateWay
    {
        public int Power { get; set; } = 2;
        public int ChangeSpanBase { get; set; } = 1;
        public int Weight { get; set; }
        public int WeightBase { get; set; }
        public long Precision { get; set; } = 100000000L;
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;
        public int FunctionTypeEnum { get; } = (int) CalculateFunctionTypeEnum.Power;

        public bool TryInitParameter(IDictionary<string, int> param)
        {
            param.TryGetValue(nameof(Power).ToLower(), out var power);
            if (power <= 0)
                return false;
            param.TryGetValue(nameof(ChangeSpanBase).ToLower(), out var changeSpanBase);
            if (changeSpanBase <= 0)
                return false;
            param.TryGetValue(nameof(Weight).ToLower(), out var weight);
            if (weight <= 0)
                return false;
            param.TryGetValue(nameof(WeightBase).ToLower(), out var weightBase);
            if (weightBase <= 0)
                return false;
            param.TryGetValue(nameof(Numerator).ToLower(), out var numerator);
            if (numerator <= 0)
                return false;
            param.TryGetValue(nameof(Denominator).ToLower(), out var denominator);
            if (denominator <= 0)
                return false;
            ChangeSpanBase = changeSpanBase;
            Weight = weight;
            WeightBase = weightBase;
            Numerator = numerator;
            Denominator = denominator;
            Power = power;
            return true;
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

    public class ConstCalculateWay : ICalculateWay
    {
        public long Precision { get; set; } = 100000000L;
        public int ConstantValue { get; set; }
        public int FunctionTypeEnum { get; } = (int) CalculateFunctionTypeEnum.Constant;

        public bool TryInitParameter(IDictionary<string, int> param)
        {
            param.TryGetValue(nameof(ConstantValue).ToLower(), out var constantValue);
            if (constantValue <= 0)
                return false;
            ConstantValue = constantValue;
            param.TryGetValue(nameof(Precision).ToLower(), out var precision);
            Precision = precision > 0 ? precision : Precision;
            return true;
        }

        public long GetCost(int cost)
        {
            return Precision.Mul(ConstantValue);
        }

        public IDictionary<string, int> GetParameterDic()
        {
            var paraDic = new Dictionary<string, int>
            {
                [nameof(ConstantValue).ToLower()] = ConstantValue
            };
            return paraDic;
        }
    }

    public class LinerCalculateWay : ICalculateWay
    {
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;
        public int ConstantValue { get; set; }
        public long Precision { get; set; } = 100000000L;
        public int FunctionTypeEnum { get; } = (int) CalculateFunctionTypeEnum.Liner;

        public bool TryInitParameter(IDictionary<string, int> param)
        {
            param.TryGetValue(nameof(Numerator).ToLower(), out var numerator);
            if (numerator <= 0)
                return false;
            param.TryGetValue(nameof(Denominator).ToLower(), out var denominator);
            if (denominator <= 0)
                return false;
            param.TryGetValue(nameof(ConstantValue).ToLower(), out var constantValue);
            if (constantValue < 0)
                return false;
            Numerator = numerator;
            Denominator = denominator;
            ConstantValue = constantValue;
            return true;
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