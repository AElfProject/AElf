using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ITransactionSizeFeeUnitPriceProvider
    {
        void SetUnitPrice(long unitPrice);
        Task<long> GetUnitPriceAsync();
    }

    /// <summary>
    /// For testing.
    /// </summary>
    public class DefaultTransactionSizeFeeUnitPriceProvider : ITransactionSizeFeeUnitPriceProvider
    {
        private long _unitPrice;

        public ILogger<DefaultTransactionSizeFeeUnitPriceProvider> Logger { get; set; }

        public DefaultTransactionSizeFeeUnitPriceProvider()
        {
            Logger = new NullLogger<DefaultTransactionSizeFeeUnitPriceProvider>();
        }

        public void SetUnitPrice(long unitPrice)
        {
            Logger.LogError("Set tx size unit price wrongly.");
            _unitPrice = unitPrice;
        }

        public Task<long> GetUnitPriceAsync()
        {
            Logger.LogError("Get tx size unit price wrongly.");
            return Task.FromResult(_unitPrice);
        }
    }

    public enum FeeType
    {
        Tx = 0,
        Cpu,
        Sto,
        Ram,
        Net
    }

    public interface ICalculateFeeService : ISingletonDependency
    {
        long CalculateFee(FeeType feeType, int cost);

        void UpdateFeeCal(FeeType feeType, int pieceKey, CalculateFunctionType funcTyoe,
            Dictionary<string, string> para);

        void DeleteFeeCal(FeeType feeType, int pieceKey);
        void AddFeeCal(FeeType feeType, int pieceKey, CalculateFunctionType funcTyoe, Dictionary<string, string> param);
    }

    class CalculateFeeService : ICalculateFeeService
    {
        private readonly ICalculateStrategyProvider _calculateStrategyProvider;

        public CalculateFeeService(ICalculateStrategyProvider calculateStrategyProvider)
        {
            _calculateStrategyProvider = calculateStrategyProvider;
        }

        public long CalculateFee(FeeType feeType, int cost)
        {
            return _calculateStrategyProvider.GetCalculator(feeType).GetCost(cost);
        }

        public void UpdateFeeCal(FeeType feeType, int pieceKey, CalculateFunctionType funcTyoe,
            Dictionary<string, string> param)
        {
            _calculateStrategyProvider.GetCalculator(feeType)
                .UpdateAlgorithm(AlgorithmOpCode.UpdateFunc, pieceKey, funcTyoe, param);
        }

        public void DeleteFeeCal(FeeType feeType, int pieceKey)
        {
            _calculateStrategyProvider.GetCalculator(feeType).UpdateAlgorithm(AlgorithmOpCode.DeleteFunc, pieceKey);
        }

        public void AddFeeCal(FeeType feeType, int pieceKey, CalculateFunctionType funcTyoe,
            Dictionary<string, string> param)
        {
            _calculateStrategyProvider.GetCalculator(feeType)
                .UpdateAlgorithm(AlgorithmOpCode.AddFunc, pieceKey, funcTyoe, param);
        }
    }

    interface ICalculateStrategyProvider : ISingletonDependency
    {
        ICalculateCostStrategy GetCalculator(FeeType feeType);
    }

    class CalculateStrategyProvider : ICalculateStrategyProvider
    {
        private Dictionary<FeeType, ICalculateCostStrategy> CalculatorDic { get; set; }

        public CalculateStrategyProvider()
        {
            CalculatorDic = new Dictionary<FeeType, ICalculateCostStrategy>
            {
                [FeeType.Cpu] = new CpuCalculateCostStrategy(),
                [FeeType.Sto] = new StoCalculateCostStrategy(),
                [FeeType.Net] = new NetCalculateCostStrategy(),
                [FeeType.Ram] = new RamCalculateCostStrategy(),
                [FeeType.Tx] = new TxCalculateCostStrategy()
            };
        }

        public ICalculateCostStrategy GetCalculator(FeeType feeType)
        {
            CalculatorDic.TryGetValue(feeType, out var cal);
            return cal;
        }
    }

    enum AlgorithmOpCode
    {
        AddFunc,
        DeleteFunc,
        UpdateFunc
    }

    interface ICalculateCostStrategy
    {
        long GetCost(int cost);

        void UpdateAlgorithm(AlgorithmOpCode opCode, int pieceKey,
            CalculateFunctionType funcType = CalculateFunctionType.Default,
            Dictionary<string, string> param = null);
    }
    abstract class CalculateCostStrategyBase : ICalculateCostStrategy
    {
        protected ICalculateAlgorithm CalculateAlgorithm { get; set; }

        public long GetCost(int cost)
        {
            return CalculateAlgorithm.Calculate(cost);
        }

        public void UpdateAlgorithm(AlgorithmOpCode opCode, int pieceKey, CalculateFunctionType funcType,
            Dictionary<string, string> param)
        {
            switch (opCode)
            {
                case AlgorithmOpCode.AddFunc:
                    CalculateAlgorithm.AddByParam(pieceKey, funcType, param);
                    break;
                case AlgorithmOpCode.DeleteFunc:
                    CalculateAlgorithm.Delete(pieceKey);
                    break;
                case AlgorithmOpCode.UpdateFunc:
                    CalculateAlgorithm.Update(pieceKey, funcType, param);
                    break;
            }
        }
    }

    #region concrete strategys

    class CpuCalculateCostStrategy : CalculateCostStrategyBase
    {
        public CpuCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(10, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            }).Add(100, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 4
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 4, // scale  x axis         
                Weight = 250, // unit weight,  means  (10 cpu count = 333 weight) 
                WeightBase = 40,
                Numerator = 1,
                Denominator = 4,
                Precision = 100000000L // 1 token = 100000000
            }).Prepare();
        }
    }

    class StoCalculateCostStrategy : CalculateCostStrategyBase
    {
        public StoCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(1000000, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 64,
                ConstantValue = 10000
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 250,
                WeightBase = 500,
                Numerator = 1,
                Denominator = 64,
                Precision = 100000000L
            }).Prepare();
        }
    }

    class RamCalculateCostStrategy : CalculateCostStrategyBase
    {
        public RamCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(10, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            }).Add(100, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 4
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 2,
                Weight = 250,
                Numerator = 1,
                Denominator = 4,
                WeightBase = 40,
            }).Prepare();
        }
    }

    class NetCalculateCostStrategy : CalculateCostStrategyBase
    {
        public NetCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(1000000, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 64,
                ConstantValue = 10000
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 250,
                WeightBase = 500,
                Numerator = 1,
                Denominator = 64,
                Precision = 100000000L
            }).Prepare();
        }
    }

    class TxCalculateCostStrategy : CalculateCostStrategyBase
    {
        public TxCalculateCostStrategy()
        {
            CalculateAlgorithm = new CalculateAlgorithm().Add(1000000, new LinerCalculateWay
            {
                Numerator = 1,
                Denominator = 16 * 50,
                ConstantValue = 10000
            }).Add(int.MaxValue, new PowerCalculateWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 1,
                WeightBase = 1,
                Numerator = 1,
                Denominator = 16 * 50,
                Precision = 100000000L
            }).Prepare();
        }
    }

    #endregion

    interface ICalculateAlgorithm
    {
        long Calculate(int count);
        ICalculateAlgorithm Add(int limit, ICalculateWay func);
        ICalculateAlgorithm Prepare();
        void Delete(int pieceKey);
        void Update(int pieceKey, CalculateFunctionType funcType, Dictionary<string, string> parameters);
        void AddByParam(int pieceKey, CalculateFunctionType funcType, Dictionary<string, string> parameters);
    }

    #region ICalculateAlgorithm implemention

    class CalculateAlgorithm : ICalculateAlgorithm
    {
        public ILogger<CalculateAlgorithm> Logger { get; set; }

        public CalculateAlgorithm()
        {
            Logger = new NullLogger<CalculateAlgorithm>();
        }

        public Dictionary<int, ICalculateWay> PieceWise { get; set; } = new Dictionary<int, ICalculateWay>();

        public ICalculateAlgorithm Add(int limit, ICalculateWay func)
        {
            // to do
            PieceWise[limit] = func;
            return this;
        }

        public ICalculateAlgorithm Prepare()
        {
            if (!PieceWise.Any() || PieceWise.Any(x => x.Key <= 0))
            {
                Logger.LogError("piece key wrong");
            }

            PieceWise = PieceWise.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            return this;
        }

        public long Calculate(int count)
        {
            long totalCost = 0;
            int prePieceKey = 0;
            foreach (var piece in PieceWise)
            {
                if (count < piece.Key)
                {
                    totalCost = piece.Value.GetCost(count.Sub(prePieceKey)).Add(totalCost);
                    break;
                }

                var span = piece.Key.Sub(prePieceKey);
                totalCost = piece.Value.GetCost(span).Add(totalCost);
                prePieceKey = piece.Key;
                if (count == piece.Key)
                    break;
            }

            return totalCost;
        }

        public void Delete(int pieceKey)
        {
            if (PieceWise.ContainsKey(pieceKey))
                PieceWise.Remove(pieceKey);
        }

        public void Update(int pieceKey, CalculateFunctionType funcType, Dictionary<string, string> parameters)
        {
            if (!PieceWise.ContainsKey(pieceKey))
                return;
            AddPieceFunction(pieceKey, funcType, parameters);
        }

        public void AddByParam(int pieceKey, CalculateFunctionType funcType, Dictionary<string, string> parameters)
        {
            if (PieceWise.ContainsKey(pieceKey) || pieceKey <= 0)
                return;
            AddPieceFunction(pieceKey, funcType, parameters);
        }

        private void AddPieceFunction(int pieceKey, CalculateFunctionType funcType,
            Dictionary<string, string> parameters)
        {
            ICalculateWay newCalculateWay = null;
            switch (funcType)
            {
                case CalculateFunctionType.Constrant:
                    newCalculateWay = new ConstCalculateWay();
                    break;
                case CalculateFunctionType.Liner:
                    newCalculateWay = new LinerCalculateWay();
                    break;
                case CalculateFunctionType.Power:
                    newCalculateWay = new PowerCalculateWay();
                    break;
                case CalculateFunctionType.Ln:
                    newCalculateWay = new LnCalculateWay();
                    break;
            }

            if (newCalculateWay != null && newCalculateWay.InitParameter(parameters))
                PieceWise[pieceKey] = newCalculateWay;
        }
    }

    #endregion


    public enum CalculateFunctionType
    {
        Default = 0,
        Constrant,
        Liner,
        Power,
        Ln,
        Bancor
    }

    public interface ICalculateWay
    {
        long GetCost(int initValue);
        bool InitParameter(Dictionary<string, string> param);
    }

    #region ICalculateWay implemention   

    public class LnCalculateWay : ICalculateWay
    {
        public int ChangeSpanBase { get; set; }
        public int Weight { get; set; }
        public int WeightBase { get; set; }
        public long Precision { get; set; } = 100000000L;

        public bool InitParameter(Dictionary<string, string> param)
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
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
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

        public bool InitParameter(Dictionary<string, string> param)
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
            ChangeSpanBase = changeSpanBase;
            Weight = weight;
            WeightBase = weightBase;

            return true;
        }

        public long GetCost(int cost)
        {
            return ((long) (Math.Pow((double) cost / ChangeSpanBase, Power) * Precision)).Mul(Weight).Div(WeightBase)
                .Add(Precision.Mul(Numerator).Div(Denominator).Mul(cost));
        }
    }

    public class BancorCalculateWay : ICalculateWay
    {
        public decimal ResourceWeight { get; set; }
        public decimal TokenWeight { get; set; }
        public long ResourceConnectorBalance { get; set; }
        public long TokenConnectorBalance { get; set; }
        public long Precision { get; set; } = 100000000L;

        public bool InitParameter(Dictionary<string, string> param)
        {
            throw new NotImplementedException();
        }

        public long GetCost(int cost)
        {
            throw new NotImplementedException();
        }
    }

    public class ConstCalculateWay : ICalculateWay
    {
        public long Precision { get; set; } = 100000000L;

        public bool InitParameter(Dictionary<string, string> param)
        {
            param.TryGetValue(nameof(ConstantValue).ToLower(), out var constantValueStr);
            int.TryParse(constantValueStr, out var constantValue);
            if (constantValue <= 0)
                return false;
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
            ConstantValue = constantValue;
            return true;
        }

        public int ConstantValue { get; set; }

        public long GetCost(int cost)
        {
            return Precision.Mul(ConstantValue);
        }
    }

    public class LinerCalculateWay : ICalculateWay
    {
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;
        public int ConstantValue { get; set; }
        public long Precision { get; set; } = 100000000L;

        public bool InitParameter(Dictionary<string, string> param)
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
            param.TryGetValue(nameof(Precision).ToLower(), out var precisionStr);
            long.TryParse(precisionStr, out var precision);
            Precision = precision > 0 ? precision : Precision;
            Numerator = numerator;
            Denominator = denominator;
            ConstantValue = constantValue;
            return true;
        }

        public long GetCost(int cost)
        {
            return Precision.Mul(cost).Mul(Numerator).Div(Denominator).Add(ConstantValue);
        }
    }

    #endregion
}