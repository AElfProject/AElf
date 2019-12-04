using System;
using System.Collections.Generic;
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
        public CalculateFunctionType FunctionType { get; } = CalculateFunctionType.Ln;

        public bool InitParameter(IDictionary<string, string> param)
        {
            param.TryGetValue(nameof(ChangeSpanBase).ToLower(), out var changeSpanBaseStr);
            int.TryParse(changeSpanBaseStr, out var changeSpanBase);
            if (changeSpanBase <= 0)
                return false;
            param.TryGetValue(nameof(Weight).ToLower(), out var weightStr);
            int.TryParse(weightStr, out var weight);
            if (weight <= 0)
                return false;
            param.TryGetValue(nameof(WeightBase).ToLower(), out var weightBaseStr);
            int.TryParse(weightBaseStr, out var weightBase);
            if (weightBase <= 0)
                return false;
            ChangeSpanBase = changeSpanBase;
            Weight = weight;
            WeightBase = weightBase;
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
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

        public IDictionary<string, string> GetParameterDic()
        {
            var paraDic = new Dictionary<string, string>
            {
                [nameof(ChangeSpanBase).ToLower()] = ChangeSpanBase.ToString(),
                [nameof(Weight).ToLower()] = Weight.ToString(),
                [nameof(WeightBase).ToLower()] = WeightBase.ToString(),
                [nameof(Precision).ToLower()] = Precision.ToString()
            };
            return paraDic;
        }
    }

    public class PowerCalculateWay : ICalculateWay
    {
        public double Power { get; set; }
        public int ChangeSpanBase { get; set; }
        public int Weight { get; set; }
        public int WeightBase { get; set; }
        public long Precision { get; set; } = 100000000L;
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;
        public CalculateFunctionType FunctionType { get; } = CalculateFunctionType.Power;

        public bool InitParameter(IDictionary<string, string> param)
        {
            param.TryGetValue(nameof(Power).ToLower(), out var powerStr);
            double.TryParse(powerStr, out var power);
            if (power <= 0)
                return false;
            param.TryGetValue(nameof(ChangeSpanBase).ToLower(), out var changeSpanBaseStr);
            int.TryParse(changeSpanBaseStr, out var changeSpanBase);
            if (changeSpanBase <= 0)
                return false;
            param.TryGetValue(nameof(Weight).ToLower(), out var weightStr);
            int.TryParse(weightStr, out var weight);
            if (weight <= 0)
                return false;
            param.TryGetValue(nameof(WeightBase).ToLower(), out var weightBaseStr);
            int.TryParse(weightBaseStr, out var weightBase);
            if (weightBase <= 0)
                return false;
            ChangeSpanBase = changeSpanBase;
            Weight = weight;
            WeightBase = weightBase;
            if (param.TryGetValue(nameof(Numerator).ToLower(), out var numeratorStr))
            {
                int.TryParse(numeratorStr, out var numerator);
                Numerator = numerator;
            }

            param.TryGetValue(nameof(Denominator).ToLower(), out var denominatorStr);
            int.TryParse(denominatorStr, out var denominator);
            if (denominator != 0)
                Denominator = denominator;
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
            Power = power;
            return true;
        }

        public long GetCost(int cost)
        {
            return ((long) (Math.Pow((double) cost / ChangeSpanBase, Power) * Precision)).Mul(Weight).Div(WeightBase)
                .Add(Precision.Mul(Numerator).Div(Denominator).Mul(cost));
        }

        public IDictionary<string, string> GetParameterDic()
        {
            var paraDic = new Dictionary<string, string>
            {
                [nameof(Power).ToLower()] = Power.ToString("0.00"),
                [nameof(ChangeSpanBase).ToLower()] = ChangeSpanBase.ToString(),
                [nameof(Weight).ToLower()] = Weight.ToString(),
                [nameof(WeightBase).ToLower()] = WeightBase.ToString(),
                [nameof(Precision).ToLower()] = Precision.ToString(),
                [nameof(Numerator).ToLower()] = Numerator.ToString(),
                [nameof(Denominator).ToLower()] = Denominator.ToString()
            };
            return paraDic;
        }
    }

    public class ConstCalculateWay : ICalculateWay
    {
        public long Precision { get; set; } = 100000000L;
        public int ConstantValue { get; set; }
        public CalculateFunctionType FunctionType { get; } = CalculateFunctionType.Constant;

        public bool InitParameter(IDictionary<string, string> param)
        {
            param.TryGetValue(nameof(ConstantValue).ToLower(), out var constantValueStr);
            int.TryParse(constantValueStr, out var constantValue);
            if (constantValue <= 0)
                return false;
            ConstantValue = constantValue;
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
            return true;
        }

        public long GetCost(int cost)
        {
            return Precision.Mul(ConstantValue);
        }

        public IDictionary<string, string> GetParameterDic()
        {
            var paraDic = new Dictionary<string, string>
            {
                [nameof(ConstantValue).ToLower()] = ConstantValue.ToString(),
                [nameof(Precision).ToLower()] = Precision.ToString(),
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
        public CalculateFunctionType FunctionType { get; } = CalculateFunctionType.Liner;

        public bool InitParameter(IDictionary<string, string> param)
        {
            param.TryGetValue(nameof(Numerator).ToLower(), out var numeratorStr);
            int.TryParse(numeratorStr, out var numerator);
            if (numerator <= 0)
                return false;
            param.TryGetValue(nameof(Denominator).ToLower(), out var denominatorStr);
            int.TryParse(denominatorStr, out var denominator);
            if (denominator <= 0)
                return false;
            param.TryGetValue(nameof(ConstantValue).ToLower(), out var constantValueStr);
            int.TryParse(constantValueStr, out var constantValue);
            if (constantValue < 0)
                return false;
            Numerator = numerator;
            Denominator = denominator;
            ConstantValue = constantValue;
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
            return true;
        }

        public long GetCost(int cost)
        {
            return Precision.Mul(cost).Mul(Numerator).Div(Denominator).Add(ConstantValue);
        }

        public IDictionary<string, string> GetParameterDic()
        {
            var paraDic = new Dictionary<string, string>
            {
                [nameof(ConstantValue).ToLower()] = ConstantValue.ToString(),
                [nameof(Precision).ToLower()] = Precision.ToString(),
                [nameof(Numerator).ToLower()] = Numerator.ToString(),
                [nameof(Denominator).ToLower()] = Denominator.ToString()
            };
            return paraDic;
        }
    }

    public class BancorCalculateWay : ICalculateWay
    {
        public decimal ResourceWeight { get; set; }
        public decimal TokenWeight { get; set; }
        public long ResourceConnectorBalance { get; set; }
        public long TokenConnectorBalance { get; set; }
        public long Precision { get; set; } = 100000000L;
        public CalculateFunctionType FunctionType { get; } = CalculateFunctionType.Bancor;

        public bool InitParameter(IDictionary<string, string> param)
        {
            throw new NotImplementedException();
        }

        public long GetCost(int cost)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, string> GetParameterDic()
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}