﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Concurrency.Execution.Messages;
using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Routing;
using Akka.Util;
using ReaderWriterLock = AElf.Common.Synchronisation.ReaderWriterLock;

namespace AElf.Kernel.Concurrency.Execution
{
    class IgnoreMessageRoutee : Routee
    {
        public override void Send(object message, IActorRef sender)
        {
        }
    }

    class NoAvailableRoutee : Routee
    {
        public override void Send(object message, IActorRef sender)
        {
            if (message is JobExecutionRequest request)
            {
                sender.Tell(new JobExecutionStatus(request.RequestId,
                    JobExecutionStatus.RequestStatus.FailedDueToNoAvailableWorker));
            }
        }
    }

    public class TrackedRoutingLogic : RoutingLogic
    {
        private IgnoreMessageRoutee IgnoreMessageRoutee { get; } = new IgnoreMessageRoutee();
        private NoAvailableRoutee NoAvailableRoutee { get; } = new NoAvailableRoutee();
        private ReaderWriterLock _lock = new ReaderWriterLock();
        private HashSet<int> _runningRouteeIndexes = new HashSet<int>();
        private HashSet<int> _idleRouteeIndexes = new HashSet<int>();
        private Dictionary<long, int> _requestIdToRouteeIndex = new Dictionary<long, int>();

        public override Routee Select(object message, Routee[] routees)
        {
            var task = _lock.WriteLock(() =>
                {
                    if (_runningRouteeIndexes.Count == 0 && _idleRouteeIndexes.Count == 0)
                    {
                        for (int i = 0; i < routees.Length; i++)
                        {
                            _idleRouteeIndexes.Add(i);
                        }
                    }

                    if (message is JobExecutionStatus status)
                    {
                        if (status.Status == JobExecutionStatus.RequestStatus.Completed)
                        {
                            if (_requestIdToRouteeIndex.TryGetValue(status.RequestId, out var ind))
                            {
                                _runningRouteeIndexes.Remove(ind);
                                _idleRouteeIndexes.Add(ind);
                                _requestIdToRouteeIndex.Remove(status.RequestId);
                            }

                            return IgnoreMessageRoutee;
                        }
                    }
                    else if (message is JobExecutionRequest req)
                    {
                        if (_idleRouteeIndexes.Count == 0)
                        {
                            return NoAvailableRoutee;
                        }

                        var ind = _idleRouteeIndexes.First();

                        _idleRouteeIndexes.Remove(ind);
                        _runningRouteeIndexes.Add(ind);
                        _requestIdToRouteeIndex.Add(req.RequestId, ind);
                        return routees[ind];
                    }

                    return Routee.NoRoutee;
                }
            );

            task.Wait();
            return task.Result;
        }
    }

    public sealed class TrackedGroup : Group
    {
        public TrackedGroup(Akka.Configuration.Config config)
            : this(
                config.GetStringList("routees.paths"),
                Dispatchers.DefaultDispatcherId)
        {
        }

        public TrackedGroup(params string[] paths) : this(paths, Dispatchers.DefaultDispatcherId)
        {
        }

        public TrackedGroup(IEnumerable<string> paths) : this(paths, Dispatchers.DefaultDispatcherId)
        {
        }

        public TrackedGroup(IEnumerable<string> paths, string routerDispatcher)
            : base(paths, routerDispatcher)
        {
        }

        public override IEnumerable<string> GetPaths(ActorSystem system)
        {
            return InternalPaths;
        }

        public override Router CreateRouter(ActorSystem system)
        {
            return new Router(new TrackedRoutingLogic());
        }

        public Group WithDispatcher(string dispatcherId)
        {
            return new TrackedGroup(InternalPaths, dispatcherId);
        }

        public override ISurrogate ToSurrogate(ActorSystem system)
        {
            return new TrackedGroupSurrogate
            {
                Paths = InternalPaths,
                RouterDispatcher = RouterDispatcher
            };
        }

        public class TrackedGroupSurrogate : ISurrogate
        {
            /// <summary>
            /// Creates a <see cref="TrackedGroupSurrogate"/> encapsulated by this surrogate.
            /// </summary>
            /// <param name="system">The actor system that owns this router.</param>
            /// <returns>The <see cref="TrackedGroupSurrogate"/> encapsulated by this surrogate.</returns>
            public ISurrogated FromSurrogate(ActorSystem system)
            {
                return new TrackedGroup(Paths, RouterDispatcher);
            }

            /// <summary>
            /// The actor paths used by this router during routee selection.
            /// </summary>
            public IEnumerable<string> Paths { get; set; }

            /// <summary>
            /// The dispatcher to use when passing messages to the routees.
            /// </summary>
            public string RouterDispatcher { get; set; }
        }
    }
}