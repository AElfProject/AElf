using System;
using Volo.Abp;

namespace AElf.ExecutionWorker
{
    class Program
    {
        public static void Main(string[] args)
        {
            using (var application = AbpApplicationFactory.Create<ExecutionWorkerModule>(options =>
            {
                options.UseAutofac();
            }))
            {
                application.Initialize();
                Console.WriteLine("Press ENTER to stop application...");
                Console.ReadLine();
            }
        }
    }
}