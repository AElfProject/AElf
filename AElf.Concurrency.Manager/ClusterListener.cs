using System;
using Akka.Actor;
using Akka.Cluster;

namespace AElf.Concurrency.Manager
{
    public class ClusterListener : UntypedActor
    {
        protected Cluster Cluster = Cluster.Get(Context.System);
        
        /// <summary>
        /// Need to subscribe to cluster changes
        /// </summary>
        protected override void PreStart()
        {
            Cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents, new[] {typeof(ClusterEvent.IMemberEvent), typeof(ClusterEvent.ReachabilityEvent),typeof(ClusterEvent.IClusterDomainEvent)});
        }

        /// <summary>
        /// Re-subscribe on restart
        /// </summary>
        protected override void PostStop()
        {
            Cluster.Unsubscribe(Self);
        }

        protected override void OnReceive(object message)
        {
//            var up = message as ClusterEvent.MemberUp;
//            if (up != null)
//            {
//                var mem = up;
//                Console.WriteLine("Member is Up: {0}", mem.Member);
//            }
//            else if (message is ClusterEvent.UnreachableMember)
//            {
//                var unreachable = (ClusterEvent.UnreachableMember) message;
//                Console.WriteLine("Member detected as unreachable: {0}", unreachable.Member);
//            }
//            else if (message is ClusterEvent.MemberRemoved)
//            {
//                var removed = (ClusterEvent.MemberRemoved) message;
//                Console.WriteLine("Member is Removed: {0}", removed.Member);
//            }
//            else if (message is ClusterEvent.IMemberEvent)
//            {
//                Console.WriteLine("ClusterEvent.IMemberEvent++++++++++++++++");            
//            }
//            else if (message is ClusterEvent.CurrentClusterState)
//            {
//                var state = message as ClusterEvent.CurrentClusterState;
//                foreach (var member in state.Members)
//                {
//                    Console.WriteLine("CurrentClusterState==" + member.Address + "--" + member.Status);
//                }
//            }
//            else
//            {
//                Unhandled(message);
//            }
            switch (message)
            {
                case ClusterEvent.CurrentClusterState state:
                    foreach (var member in state.Members)
                    {
                        ClusterState.AddOrUpdate(member);
                    }
                    break;
                case ClusterEvent.MemberUp memberUp:
                    ClusterState.AddOrUpdate(memberUp.Member);
                    break;
                case ClusterEvent.MemberExited memberExited:
                    ClusterState.AddOrUpdate(memberExited.Member);
                    break;
                case ClusterEvent.UnreachableMember unreachableMember:
                    ClusterState.SetReachable(unreachableMember.Member, false);
                    break;
                case ClusterEvent.ReachableMember reachableMember:
                    ClusterState.SetReachable(reachableMember.Member, true);
                    break;
                case ClusterEvent.MemberRemoved memberRemoved:
                    ClusterState.Remove(memberRemoved.Member);
                    break;
                case ClusterEvent.LeaderChanged leaderChanged:
                    ClusterState.ChangeLeader(leaderChanged.Leader);
                    break;
                case ClusterEvent.RoleLeaderChanged roleLeaderChanged:
                    ClusterState.ChangeRoleLeader(roleLeaderChanged);
                    break;
            }
            
            ClusterState.Print();
        }
    }
}