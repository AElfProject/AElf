using System;
using System.Collections.Generic;
using AElf.Types;

namespace AElf.WebApp.Application.Chain.Dto;

public class CalculateTransactionFeeOutput
{
    public bool Success { get; set; }

    [Obsolete("ThisField is deprecated, please use NewField instead.")]
    public Dictionary<string, long> TransactionFee { get; set; }
    
    [Obsolete("ThisField is deprecated, please use NewField instead.")]
    public Dictionary<string, long> ResourceFee { get; set; }
    
    public List<FeeDto> TransactionFeeList { get; set; }

    public List<FeeDto> ResourceFeeList { get; set; }

    public string Error { get; set; }
}

public class FeeDto
{
    public string ChargingAddress { get; set; }
    public Dictionary<string, long> Fee { get; set; }
}