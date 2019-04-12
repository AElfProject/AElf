using Akka.Actor;
using Akka.Cluster;

namespace AElf.Monitor
{
    public class AkkaClusterListener : UntypedActor
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
            switch (message)
            {
                case ClusterEvent.CurrentClusterState state:
                    foreach (var member in state.Members)
                    {
                        AkkaClusterState.AddOrUpdate(member);
                    }
                    break;
                case ClusterEvent.MemberJoined memberJoined:
                    AkkaClusterState.AddOrUpdate(memberJoined.Member);
                    break;
                case ClusterEvent.MemberUp memberUp:
                    AkkaClusterState.AddOrUpdate(memberUp.Member);
                    break;
                case ClusterEvent.MemberExited memberExited:
                    AkkaClusterState.AddOrUpdate(memberExited.Member);
                    break;
                case ClusterEvent.UnreachableMember unreachableMember:
                    AkkaClusterState.SetReachable(unreachableMember.Member, false);
                    break;
                case ClusterEvent.ReachableMember reachableMember:
                    AkkaClusterState.SetReachable(reachableMember.Member, true);
                    break;
                case ClusterEvent.MemberRemoved memberRemoved:
                    AkkaClusterState.Remove(memberRemoved.Member);
                    break;
                case ClusterEvent.LeaderChanged leaderChanged:
                    AkkaClusterState.ChangeLeader(leaderChanged.Leader);
                    break;
                case ClusterEvent.RoleLeaderChanged roleLeaderChanged:
                    AkkaClusterState.ChangeRoleLeader(roleLeaderChanged);
                    break;
            }
            
            AkkaClusterState.Print();
        }
    }
}