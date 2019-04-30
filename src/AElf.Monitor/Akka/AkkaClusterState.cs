using System;
using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Cluster;

namespace AElf.Monitor
{
    public class AkkaClusterState
    {
        public static ConcurrentDictionary<string, MemberInfo> MemberInfos = new ConcurrentDictionary<string, MemberInfo>();

        public static void AddOrUpdate(Member member)
        {
            var address = member.Address.ToString();
            if (MemberInfos.ContainsKey(address))
            {
                MemberInfos[address].Roles = string.Join(",", member.Roles);
                MemberInfos[address].Status = member.Status.ToString();
            }
            else
            {
                MemberInfos.TryAdd(
                    address,
                    new MemberInfo
                    {
                        Address = address,
                        Roles = string.Join(",", member.Roles),
                        Status = member.Status.ToString(),
                        Reachable = true
                    }
                );
            }
        }

        public static void SetReachable(Member member,bool reachable)
        {
            var address = member.Address.ToString();
            if (MemberInfos.ContainsKey(address))
            {
                var memberInfo = MemberInfos[address];
                memberInfo.Roles = string.Join(",", member.Roles);
                memberInfo.Status = member.Status.ToString();
                memberInfo.Reachable = reachable;
            }
            else
            {
                MemberInfos.TryAdd(
                    address,
                    new MemberInfo
                    {
                        Address = address,
                        Roles = string.Join(",", member.Roles),
                        Status = member.Status.ToString(),
                        Reachable = reachable
                    }
                );
            }
        }

        public static void Remove(Member member)
        {
            var address = member.Address.ToString();
            if (MemberInfos.ContainsKey(address))
            {
                MemberInfo info;
                MemberInfos.TryRemove(address, out info);
            }
        }

        public static void ChangeLeader(Akka.Actor.Address address)
        {
            foreach (var member in MemberInfos.Values)
            {
                member.ClusterLeader = false;
            }

            var addressStr = address.ToString();
            if (MemberInfos.ContainsKey(addressStr))
            {
                MemberInfos[addressStr].ClusterLeader = true;
            }
        }
        
        public static void ChangeRoleLeader(ClusterEvent.RoleLeaderChanged roleLeader)
        {
            foreach (var member in MemberInfos.Values)
            {
                if (member.Roles == roleLeader.Role)
                {
                    member.RoleLeader = false;
                }
            }

            var addressStr = roleLeader.Leader.ToString();
            if (MemberInfos.ContainsKey(addressStr))
            {
                MemberInfos[addressStr].RoleLeader = true;
            }
        }

        public static void Print()
        {
            Console.WriteLine("==========================================================================================================================================");
            foreach (var memberInfo in MemberInfos.Values)
            {
                Console.WriteLine("Roles:"+memberInfo.Roles + "\tAddress:" + memberInfo.Address + "\tStatus:" + memberInfo.Status + "\tReachable:" + memberInfo.Reachable + "\tClusterLeader:" + memberInfo.ClusterLeader + "\tRoleLeader:" + memberInfo.RoleLeader);
            }

            Console.WriteLine();
        }
    }

    public class MemberInfo
    {
        public string Roles { get; set; }
        
        public string Address { get; set; }

        public string Status { get; set; }

        public bool Reachable { get; set; }

        public bool ClusterLeader { get; set; }

        public bool RoleLeader { get; set; }
    }
}