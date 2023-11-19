namespace AElf.WebApp.Application.Chain.Dto;

public class EstimateTransactionFeeOutput
{
    public bool Success { get; set; }

    public long GasFee { get; set; }
}