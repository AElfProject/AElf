using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ICodeCheckActivationService
    {
        void Enable();
        void Disable();
        bool IsActive();
    }

    public class CodeCheckActivationService : ICodeCheckActivationService, ISingletonDependency
    {
        private volatile bool _active = false;

        public void Enable()
        {
            _active = true;
        }

        public void Disable()
        {
            _active = false;
        }

        public bool IsActive()
        {
            return _active;
        }
    }
}