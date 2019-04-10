using AElf.Kernel.Account;
using AElf.Common;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;

namespace AElf.Contracts.Proposal
{
    #region Field Names

    public static class FieldNames
    {
        public static readonly string MultiSig = "__MultiSig__";
    }

    #endregion Field Names
    
    public class Authorization : CSharpSmartContract
    {
        private readonly Map<Address, Auth> _multiSig = new Map<Address, Auth>(FieldNames.MultiSig);

        #region Actions

        public Address CreateMultiSigAccount(Auth authorization)
        {
            
        }
        
        public Auth GetAuthorization(Address address)
        {
            // case 1
            // get authorization of system account
            
            // case 2 
            // get authorization of normal multi sig account
        }

        public Hash Propose(Kernel.Account.Proposal proposal)
        {
            
        }
        
            

        #endregion


        private bool GetAuthorization(Address address, out Auth authorization)
        {
            // case 1
            // get authorization of system account
            
            // case 2 
            // get authorization of normal multi sig account
        }
    }
}