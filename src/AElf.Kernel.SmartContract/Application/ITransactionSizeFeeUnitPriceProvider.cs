using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Sdk.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    
    public interface ICalculateFeeService
    {
        long GetCpuTokenCost(int readCount);
        long GetStoTokenCost(int writeCount);
        long GetNetTokenCost(int neeCost);
        long GetTransactionFee(int txSize);
        ICalCostService GetCpuCalculator { get; }
        ICalCostService GetNetCalculator { get; }
        ICalCostService GetStoCalculator { get; }
        ICalCostService GetTxCalculator { get; }
    }
    public class CalculateFeeService : ICalculateFeeService
    {
        private static ICalCostService _cpuCal;
        private static ICalCostService _netCal;
        private static ICalCostService _stoCal;
        private static ICalCostService _txCal;

        static CalculateFeeService()
        { 
            _cpuCal = new CalCostService();
            _cpuCal.Add(10, x =>  10000 + 100000000L.Div(8).Mul(x));
            _cpuCal.Add(1000, x =>  100000000L.Div(4).Mul(x));
            _cpuCal.Add(-1, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 10,         // scale  x axis         
                Weight = 333,   // unit weight,  means  (10 cpu count = 333 weight) 
                WeightBase = 10,
                Decimal = 100000000L  // 1 token = 100000000
            }.GetCost(x));

            _stoCal = new CalCostService();
            _stoCal.Add(10, x => 10000 + 100000000L.Div(8).Mul(x));
            _stoCal.Add(1000, x => 100000000L.Div(4).Mul(x));
            _stoCal.Add(-1, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 5,
                Weight = 333,
                WeightBase = 5,
                Decimal = 100000000L
            }.GetCost(x));
              
            _netCal = new CalCostService();
            _netCal.Add(1000, x =>  10000 + 100000000L.Div(30).Mul(x));
            _netCal.Add(1000000, x =>  100000000L.Div(15).Mul(x));
            _netCal.Add(-1, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 333,
                WeightBase = 500,
                Decimal = 100000000L
            }.GetCost(x));  
                   
            _txCal = new CalCostService();
            _txCal.Add(1000, x => 10000 + 100000000L.Div(50).Div(15).Mul(x));
            _txCal.Add(1000000, x =>100000000L.Div(50).Div(15).Mul(x));
            _txCal.Add(-1, x => new PowCalService
            {
                Power = 2,
                ChangeSpanBase = 100,
                Weight = 1,
                WeightBase = 1,
                Decimal = 100000000L.Div(50)
            }.GetCost(x));

        }
        public ICalCostService GetCpuCalculator => _cpuCal;
        public ICalCostService GetNetCalculator => _netCal;
        public ICalCostService GetStoCalculator => _stoCal;
        public ICalCostService GetTxCalculator => _txCal;
        public long GetCpuTokenCost(int readCount)
        {
            return _cpuCal.CalCost(readCount);
        }

        public long GetStoTokenCost(int writeCount)
        {
            return _stoCal.CalCost(writeCount);
        }

        public long GetNetTokenCost(int netCost)
        {
            return _netCal.CalCost(netCost);
        }

        public long GetTransactionFee(int txSize)
        {
            return _txCal.CalCost(txSize);
        }
    }
    public interface ICalCostService
    {
        Dictionary<int, Func<int, long>> PieceWise {get;set;}
        long CalCost(int count);
        void Add(int limit, Func<int, long> func);
    }
    public class CalCostService : ICalCostService
    {
        public Dictionary<int, Func<int, long>> PieceWise {get;set;} = new Dictionary<int, Func<int, long>>();
        public void Add(int limit, Func<int,long> func){
                // to do
            PieceWise[limit] = func;
        }
        public long CalCost(int count)
        {
            long totalCost = 0;
            int prePieceKey = 0;
            int spare = count;
            foreach(var piece in PieceWise)
            {
                if (piece.Key < 0)
                {
                    totalCost += piece.Value.Invoke(spare);
                    break;
                }
                int costSpan = count - piece.Key;
                if(costSpan < 0)
                {
                    totalCost += piece.Value.Invoke(count - prePieceKey);
                    break;
                }
                totalCost += piece.Value.Invoke(piece.Key - prePieceKey);
                spare -= piece.Key - prePieceKey;
                prePieceKey = piece.Key;
                if(costSpan == 0)
                    break;
            }
            return totalCost >= 0? totalCost : long.MaxValue;
        }
    }
    
    #region cal imp     
    public interface ICalService
    {
        long GetCost(int initValue);
    } 
    public class LnCalService : ICalService
    {
        public int ChangeSpanBase { get; set; }
        public long Weight { get; set;}
        public int WeightBase { get; set;}
        public long Decimal { get; set; }
        public long GetCost(int cost)
        {
            int diff = cost + 1;
            double weightChange = (double)diff / ChangeSpanBase;
            double unitValue = (double)Weight/ WeightBase;
            if(weightChange <= 1)
                return 0;
            return (long) (weightChange * unitValue * Math.Log(weightChange, Math.E));
        }
    }
    public class PowCalService : ICalService
    {
        public double Power { get; set; }
        public int ChangeSpanBase { get; set; }
        public long Weight { get; set;}
        public int WeightBase { get; set;}
        public long Decimal { get; set; }
        public long GetCost(int cost)
        {
            return  ((long)Math.Pow((double) cost / ChangeSpanBase, Power) * Decimal).Mul(Weight).Div(WeightBase);
        }
    }
    public class BancorCalService: ICalService
    {
         public decimal ResourceWeight { get; set; }
         public decimal TokenWeight { get; set; }
         public long ResourceConnectorBalance { get; set; }
         public long TokenConnectorBalance { get; set; }
         public long GetCost(int cost)
         {
             throw new NotImplementedException();
         }
    }
    public class ConstCalService: ICalService
    {
        public long GetCost(int cost)
        {
            throw new NotImplementedException();
        }
    }
    public class LinerCalService: ICalService
    {
        public long GetCost(int cost)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}