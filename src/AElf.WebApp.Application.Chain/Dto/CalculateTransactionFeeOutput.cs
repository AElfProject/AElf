using System;
using System.Collections.Generic;
using AElf.Types;

namespace AElf.WebApp.Application.Chain.Dto;

public class CalculateTransactionFeeOutput
{
    public bool Success { get; set; }

    [Obsolete("This property is deprecated and will be removed in the next version. Use the TransactionFees instead.")]
    public Dictionary<string, long> TransactionFee { get; set; }
    
    [Obsolete("This property is deprecated and will be removed in the next version. Use the ResourceFees instead.")]
    public Dictionary<string, long> ResourceFee { get; set; }
    
    public FeeDto TransactionFees { get; set; }

    public FeeDto ResourceFees { get; set; }

    public string Error { get; set; }
}

public class FeeDto
{
    public string ChargingAddress { get; set; }
    public Dictionary<string, long> Fee { get; set; }
}