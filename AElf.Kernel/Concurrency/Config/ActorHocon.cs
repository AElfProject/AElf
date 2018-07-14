namespace AElf.Kernel.Concurrency.Config
{
    public class ActorHocon
    {
        public const string ActorSingleHocon = @"
            akka {
                actor {
                    deployment {
                        /router {
                            router = tracked-group
                        }
                    }
                    router.type-mapping {
                        tracked-group = ""AElf.Kernel.Concurrency.Execution.TrackedGroup, AElf.Kernel""
                    }
                }
            }
        ";

        public const string ActorClusterHocon = @"
            petabridge.cmd {
                host = ""0.0.0.0""
                port = 9110
            }
            akka {
                actor {
                    provider = cluster
                    deployment {
                        /router {
                            router = round-robin-group
                            routees.paths = [""/user/worker""]
                            virtual-nodes-factor = 8
                            cluster {
                                enabled = on
                                max-nr-of-instances-per-node = 1
                                allow-local-routees = off
                                use-role = worker
                            }
                        }
                    }
                    router.type-mapping {
                        tracked-group = ""AElf.Kernel.Concurrency.Execution.TrackedGroup, AElf.Kernel""
                    }
                    serializers {
                        hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                    }
                    serialization-bindings {
                        ""System.Object"" = hyperion
                    }
                }
                remote {
                    maximum-payload-bytes = 30000000 bytes
                    dot-netty.tcp {
                        hostname = ""127.0.0.1""
                        port = 0
                        message-frame-size =  30000000b
                        send-buffer-size =  30000000b
                        receive-buffer-size =  30000000b
                        maximum-frame-size = 30000000b
                    }
                }
                cluster {
                    seed-nodes = [""akka.tcp://AElfSystem@127.0.0.1:32551""]
                    roles = [""manager""]
                    seed-node-timeout = 10s
                    gossip-interval = 3s
                    gossip-time-to-live = 5s
                    leader-actions-interval = 3s
                    unreachable-nodes-reaper-interval = 3s
                }
            }
        ";

        public const string ActorWorkerHocon = @"
            akka {
                actor {
                    provider = cluster
                    serializers {
                        hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                    }
                    serialization-bindings {
                        ""System.Object"" = hyperion
                    }
                }
                remote {
                    maximum-payload-bytes = 30000000 bytes
                    dot-netty.tcp {
                        hostname = ""127.0.0.1""
                        port = 32551
                        message-frame-size =  30000000b
                        send-buffer-size =  30000000b
                        receive-buffer-size =  30000000b
                        maximum-frame-size = 30000000b
                    }
                }
                cluster {
                    seed-nodes = [""akka.tcp://AElfSystem@127.0.0.1:32551"",""akka.tcp://AElfSystem@127.0.0.1:32552""]
                    roles = [""worker""]
                    seed-node-timeout = 10s
                    gossip-interval = 3s
                    gossip-time-to-live = 5s
                    leader-actions-interval = 3s
                    unreachable-nodes-reaper-interval = 3s
                }
            }
        ";
    }
}