namespace AElf.Configuration
{
    [ConfigFile(FileName = "parallel.json")]
    public class ParallelConfig : ConfigBase<ParallelConfig>
    {
        public bool IsParallelEnable { get; set; }

        public ParallelConfig()
        {
            IsParallelEnable = false;
        }
    }
}