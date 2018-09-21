namespace AElf.Kernel
{
    public class TxStatus
    {
        public static readonly TxStatus Received = new TxStatus("Received") { };
        public static readonly TxStatus Validating = new TxStatus("Validating") { };
        public static readonly TxStatus Validated = new TxStatus("Validated") { };
//        public static readonly TxStatus Grouping = new TxStatus("Grouping") { };
//        public static readonly TxStatus Grouped = new TxStatus("Grouped") { };
        public static readonly TxStatus Executing = new TxStatus("Executing") { };
        public static readonly TxStatus Executed = new TxStatus("Executed") { };
        public static readonly TxStatus Expired = new TxStatus("Expired") { };
        public static readonly TxStatus Invalid = new TxStatus("Invalid"){ };

        private readonly string _name;

        private TxStatus(string name)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }
    }
}