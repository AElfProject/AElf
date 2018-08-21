namespace AElf.SmartContract
{
    public class StateCache
    {
        private byte[] _currentValue;
        public Kernel.Storages.Types Type { get; set; }

        public StateCache(byte[] initialValue)
        {
            InitialValue = initialValue;
            _currentValue = initialValue;
        }

        public bool Dirty { get; private set; }
        
        public byte[] InitialValue { get; }

        public byte[] CurrentValue
        {
            get => _currentValue;
            set
            {
                Dirty = true;
                _currentValue = value;
            }
        }

        public void SetValue(byte[] value)
        {
            Dirty = true;
            CurrentValue = value;
        }
    }
}