﻿using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Volo.Abp;

namespace AElf.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var application = AbpApplicationFactory.Create<BenchmarkAElfModule>(options =>
            {
                options.UseAutofac();
            }))
            {
                application.Initialize();
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());          
            }
        }
    }
}