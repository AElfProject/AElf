namespace AElf.Kernel
{
    public class ProtobufSerialable: ISerializable
    {

        private IAccountManager _accountManager;
        private readonly int _someInt;

        public ProtobufSerialable(IAccountManager accountManager,int someInt)
        {
            _accountManager = accountManager;
            _someInt = someInt;
        }


        public byte[] Serialize()
        {
            throw new System.NotImplementedException();
        }
    }
    
}