using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.AirDrop
{
    public class AirDropContract : AirDropContractContainer.AirDropContractBase
    {
        public override Empty Deposit(DepositInput input)
        {
            
            return new Empty();
        }

        public override Empty AirDrop(AirDropInput input)
        {
            
            return new Empty();
        }
    }
}