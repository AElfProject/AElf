using System.Collections.Generic;
using AElf.Types;

namespace AElf.WebApp.Application.Chain.Dto;

public class CalculateTransactionFeeOutput
{
    public bool Success { get; set; }

    public Dictionary<string, long> TransactionFee { get; set; }

    public Dictionary<string, long> ResourceFee { get; set; }

    public string Error { get; set; }
    
    public string TransactionFeeChargingAddress { get; set; }
}