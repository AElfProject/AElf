namespace AElf.Kernel
{
    public class EvilTriggerOptions
    {
        public int EvilTriggerNumber { get; set; } = 32;
        public bool DoubleSpendAttack { get; set; }
        public bool RepeatTransactionInOneBlockAttack { get; set; }
    }
}