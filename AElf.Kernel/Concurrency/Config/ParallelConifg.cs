﻿using AElf.Configuration;

namespace AElf.Kernel.Concurrency.Config
{
    [ConfigFile(FileName = "parallelconfig.json")]
    public class ParallelConfig : ConfigBase<ParallelConfig>
    {
        public bool IsParallelEnable { get; set; }

        public ParallelConfig()
        {
            IsParallelEnable = false;
        }
    }
}