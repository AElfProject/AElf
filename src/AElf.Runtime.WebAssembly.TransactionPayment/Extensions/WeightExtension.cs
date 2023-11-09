namespace AElf.Runtime.WebAssembly.TransactionPayment.Extensions;

public static class WeightExtension
{
    public static Weight Sub(this Weight weight, Weight other)
    {
        return new Weight
        {
            RefTime = weight.RefTime - other.RefTime,
            ProofSize = weight.ProofSize - other.ProofSize
        };
    }

    public static Weight Add(this Weight weight, Weight other)
    {
        return new Weight
        {
            RefTime = weight.RefTime + other.RefTime,
            ProofSize = weight.ProofSize + other.ProofSize
        };
    }

    public static Weight Mul(this Weight weight, int multiplier)
    {
        return new Weight
        {
            RefTime = weight.RefTime * multiplier,
            ProofSize = weight.ProofSize * multiplier
        };
    }

    public static bool Insufficient(this Weight weight)
    {
        return weight.RefTime < 0 || weight.ProofSize < 0;
    }
}