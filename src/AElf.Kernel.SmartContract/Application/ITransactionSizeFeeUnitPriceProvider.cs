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
        void UpdateFeeCal(FeeType feeType, int pieceKey, Dictionary<string, string> para);
        void DeleteFeeCal(FeeType feeType, int pieceKey);
        void AddFeeCal(FeeType feeType, int pieceKey, Dictionary<string, string> param);
        void RemoveStradegy(FeeType feeType);
    }

    public class CalculateFeeService : ICalculateFeeService
    {
        private readonly ICalStradegyProvider _calStradegyProvider;

        public CalculateFeeService(ICalStradegyProvider calStradegyProvider)
        {
            _calStradegyProvider = calStradegyProvider;
        }

        public long CalculateFee(FeeType feeType, int cost)
        {
            return _calStradegyProvider.GetCalculator(feeType).GetCost(cost);
        }

        public void RemoveStradegy(FeeType feeType)
        {
            throw new NotImplementedException();
        }

        public void UpdateFeeCal(FeeType feeType, int pieceKey, Dictionary<string, string> param)
        {
            _calStradegyProvider.GetCalculator(feeType).UpdateAlgorithm(AlgorithmOpCode.UpdateFunc, pieceKey, param);
        }

        public void DeleteFeeCal(FeeType feeType, int pieceKey)
        {
            _calStradegyProvider.GetCalculator(feeType).UpdateAlgorithm(AlgorithmOpCode.DeleteFunc, pieceKey);
        }

        public void AddFeeCal(FeeType feeType, int pieceKey, Dictionary<string, string> param)
        {
            _calStradegyProvider.GetCalculator(feeType).UpdateAlgorithm(AlgorithmOpCode.AddFunc, pieceKey, param);
        }
    }

    public interface ICalStradegyProvider : ISingletonDependency
    {
        ICalCostStrategy GetCalculator(FeeType feeType);
    }

    public class CalStradegyProvider : ICalStradegyProvider
    {
        private Dictionary<FeeType, ICalCostStrategy> CalDic { get; set; }

        public CalStradegyProvider()
        {
            CalDic = new Dictionary<FeeType, ICalCostStrategy>
            {
                [FeeType.Cpu] = new CpuCalCostStrategy(),
                [FeeType.Sto] = new StoCalCostStrategy(),
                [FeeType.Net] = new NetCalCostStrategy(),
                [FeeType.Ram] = new RamCalCostStrategy(),
                [FeeType.Tx] = new TxCalCostStrategy()
            };
        }

        public ICalCostStrategy GetCalculator(FeeType feeType)
        {
            CalDic.TryGetValue(feeType, out var cal);
            return cal;
        }
    }

    public enum AlgorithmOpCode
    {
        AddFunc,
        DeleteFunc,
        UpdateFunc
    }

    public interface ICalCostStrategy
    {
        long GetCost(int cost);

        //ICalAlgorithm CalAlgorithm { get; set; }
        void UpdateAlgorithm(AlgorithmOpCode opCode, int pieceKey, Dictionary<string, string> param = null);
    }

    abstract class CalCostStrategyBase : ICalCostStrategy
    {
        protected ICalAlgorithm CalAlgorithm { get; set; }

        public long GetCost(int cost)
        {
            return CalAlgorithm.Calculate(cost);
        }

        public void UpdateAlgorithm(AlgorithmOpCode opCode, int pieceKey, Dictionary<string, string> param = null)
        {
            throw new NotImplementedException();
        }
    }

    class CpuCalCostStrategy : CalCostStrategyBase
    {
        public CpuCalCostStrategy()
        {
            CalAlgorithm = new CalAlgorithm().Add(10, new LinerCalWay
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            }).Add(100, new LinerCalWay
            {
                Numerator = 1,
                Denominator = 4
            }).Add(int.MaxValue, new PowCalWay
            {
                Power = 2,
                ChangeSpanBase = 4, // scale  x axis         
                Weight = 250, // unit weight,  means  (10 cpu count = 333 weight) 
                WeightBase = 40,
                Numerator = 1,
                Denominator = 4,
                Decimal = 100000000L // 1 token = 100000000
            }).Prepare();
        }
    }

    class StoCalCostStrategy : CalCostStrategyBase
    {
        public StoCalCostStrategy()
        {
            CalAlgorithm = new CalAlgorithm().Add(1000000, new LinerCalWay
            {
                Numerator = 1,
                Denominator = 64,
                ConstantValue = 10000
            }).Add(int.MaxValue, new PowCalWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 250,
                WeightBase = 500,
                Numerator = 1,
                Denominator = 64,
                Decimal = 100000000L
            }).Prepare();
        }
    }

    class RamCalCostStrategy : CalCostStrategyBase
    {
        public RamCalCostStrategy()
        {
            CalAlgorithm = new CalAlgorithm().Add(10, new LinerCalWay
            {
                Numerator = 1,
                Denominator = 8,
                ConstantValue = 10000
            }).Add(100, new LinerCalWay
            {
                Numerator = 1,
                Denominator = 4
            }).Add(int.MaxValue, new PowCalWay
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

    class NetCalCostStrategy : CalCostStrategyBase
    {
        public NetCalCostStrategy()
        {
            CalAlgorithm = new CalAlgorithm().Add(1000000, new LinerCalWay
            {
                Numerator = 1,
                Denominator = 64,
                ConstantValue = 10000
            }).Add(int.MaxValue, new PowCalWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 250,
                WeightBase = 500,
                Numerator = 1,
                Denominator = 64,
                Decimal = 100000000L
            }).Prepare();
        }
    }

    class TxCalCostStrategy : CalCostStrategyBase
    {
        public TxCalCostStrategy()
        {
            CalAlgorithm = new CalAlgorithm().Add(1000000, new LinerCalWay
            {
                Numerator = 1,
                Denominator = 16 * 50,
                ConstantValue = 10000
            }).Add(int.MaxValue, new PowCalWay
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 1,
                WeightBase = 1,
                Numerator = 1,
                Denominator = 16 * 50,
                Decimal = 100000000L
            }).Prepare();
        }
    }

    public interface ICalAlgorithm
    {
        Dictionary<int, ICalWay> PieceWise { get; set; }
        long Calculate(int count);
        ICalAlgorithm Add(int limit, ICalWay func);
        ICalAlgorithm Prepare();
        void Delete(int pieceKey);
        void Update(int pieceKey, CalFunctionType funcType, Dictionary<string, string> parameters);
        void AddPieceFunction(int pieceKey, CalFunctionType funcType, Dictionary<string, string> parameters);
    }

    public class CalAlgorithm : ICalAlgorithm
    {
        public ILogger<CalAlgorithm> Logger { get; set; }

        public CalAlgorithm()
        {
            Logger = new NullLogger<CalAlgorithm>();
        }

        public Dictionary<int, ICalWay> PieceWise { get; set; } = new Dictionary<int, ICalWay>();

        public ICalAlgorithm Add(int limit, ICalWay func)
        {
            // to do
            PieceWise[limit] = func;
            return this;
        }

        public ICalAlgorithm Prepare()
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

        public void Update(int pieceKey, CalFunctionType funcType, Dictionary<string, string> parameters)
        {
            Delete(pieceKey);
            AddPieceFunction(pieceKey, funcType, parameters);
        }

        public void AddPieceFunction(int pieceKey, CalFunctionType funcType, Dictionary<string, string> parameters)
        {
            
        }
    }

    #region cal imp     

    public enum CalFunctionType
    {
        Constrant = 0,
        Liner,
        Power,
        Ln,
        Bancor
    }

    public interface ICalWay
    {
        long GetCost(int initValue);
        long Decimal { get; set; }
        void UpdateParameter(Dictionary<string, string> param);
    }

    public class LnCalWay : ICalWay
    {
        public int ChangeSpanBase { get; set; }
        public long Weight { get; set; }
        public int WeightBase { get; set; }
        public long Decimal { get; set; } = 100000000L;

        public void UpdateParameter(Dictionary<string, string> param)
        {
            throw new NotImplementedException();
        }

        public long GetCost(int cost)
        {
            int diff = cost + 1;
            double weightChange = (double) diff / ChangeSpanBase;
            double unitValue = (double) Weight / WeightBase;
            if (weightChange <= 1)
                return 0;
            return Decimal.Mul((long) (weightChange * unitValue * Math.Log(weightChange, Math.E)));
        }
    }

    public class PowCalWay : ICalWay
    {
        public double Power { get; set; }
        public int ChangeSpanBase { get; set; }
        public long Weight { get; set; }
        public int WeightBase { get; set; }
        public long Decimal { get; set; } = 100000000L;

        public void UpdateParameter(Dictionary<string, string> param)
        {
            throw new NotImplementedException();
        }

        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;

        public long GetCost(int cost)
        {
            return ((long) (Math.Pow((double) cost / ChangeSpanBase, Power) * Decimal)).Mul(Weight).Div(WeightBase)
                .Add(Decimal.Mul(Numerator).Div(Denominator).Mul(cost));
        }
    }

    public class BancorCalWay : ICalWay
    {
        public decimal ResourceWeight { get; set; }
        public decimal TokenWeight { get; set; }
        public long ResourceConnectorBalance { get; set; }
        public long TokenConnectorBalance { get; set; }
        public long Decimal { get; set; } = 100000000L;

        public void UpdateParameter(Dictionary<string, string> param)
        {
            throw new NotImplementedException();
        }

        public long GetCost(int cost)
        {
            throw new NotImplementedException();
        }
    }

    public class ConstCalWay : ICalWay
    {
        public long Decimal { get; set; } = 100000000L;

        public void UpdateParameter(Dictionary<string, string> param)
        {
            if (param.TryGetValue(nameof(ConstantValue), out var constValue))
            {
                
            }
                
        }

        public int ConstantValue { get; set; }

        public long GetCost(int cost)
        {
            return Decimal.Mul(ConstantValue);
        }
    }

    public class LinerCalWay : ICalWay
    {
        public int Numerator { get; set; }
        public int Denominator { get; set; } = 1;
        public int ConstantValue { get; set; }
        public long Decimal { get; set; } = 100000000L;

        public void UpdateParameter(Dictionary<string, string> param)
        {
            throw new NotImplementedException();
        }

        public long GetCost(int cost)
        {
            return Decimal.Mul(cost).Mul(Numerator).Div(Denominator).Add(ConstantValue);
        }
    }

    #endregion
}