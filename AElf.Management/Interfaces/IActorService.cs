using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface IActorService
    {
        List<MemberInfo> GetState(string chainId);
    }
}