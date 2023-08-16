namespace AElf.Runtime.WebAssembly;

public class WriteOutcome
{
    public WriteOutcomeType WriteOutcomeType { get; set; }
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// TODO: SENTINEL is not a `0`
    /// </summary>
    /// <returns></returns>
    public int OldLenWithSentinel()
    {
        if (WriteOutcomeType == WriteOutcomeType.New)
        {
            return 0;
        }

        if (WriteOutcomeType == WriteOutcomeType.Overwritten)
        {
            if (int.TryParse(Value, out var len))
            {
                return len;
            }
        }

        return Value.Length;
    }
}

public enum WriteOutcomeType
{
    // No value existed at the specified key.
    New,

    // A value of the returned length was overwritten.
    Overwritten,

    // This is only returned when specifically requested because it causes additional work
    // depending on the size of the pre-existing value. When not requested [`Self::Overwritten`]
    // is returned instead.
    Taken
}