namespace AElf.Kernel.Types.Common
{
    public class TerminationSignal
    {
        public TerminatedModuleEnum Module { get;}
        
        public TerminationSignal(TerminatedModuleEnum module)
        {
            Module = module;
        }
    }

    public class TerminatedModule
    {
        public TerminatedModuleEnum Module { get;}

        public TerminatedModule(TerminatedModuleEnum module)
        {
            Module = module;
        }
    }

    public enum TerminatedModuleEnum
    {
        Rpc,
        TxPool,
        Mining,
        BlockSynchronizer
    }
}