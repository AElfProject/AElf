using System.Collections.Generic;

namespace AElf.WebApp.Application.Chain.Dto;

public class CalculateTransactionFeeOutput
{
    public bool Success { get; set; }

    public Dictionary<string, long> TransactionFee { get; set; }

    public Dictionary<string, long> ResourceFee { get; set; }
}