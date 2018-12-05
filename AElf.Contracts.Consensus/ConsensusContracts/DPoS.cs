using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.Enums;
using AElf.Contracts.Consensus.ConsensusContracts.FieldMapCollections;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Types;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus.ConsensusContracts
{
    // ReSharper disable UnusedMember.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable MemberCanBeMadeStatic.Local
    public class DPoS
    {
        public ConsensusType Type => ConsensusType.AElfDPoS;

        public ulong CurrentRoundNumber => _currentRoundNumberField.GetAsync().Result;

        private Address TokenContractAddress ;

        public int Interval
        {
            get
            {
                var interval = _miningIntervalField.GetAsync().Result;
                return interval == 0 ? 4000 : interval;
            }
        }

        public int LogLevel { get; set; }

        public Hash Nonce { get; set; } = Hash.Default;

        #region Protobuf fields and maps

        private readonly UInt64Field _currentRoundNumberField;

        private readonly PbField<OngoingMiners> _ongoingMinersField;

        private readonly Map<UInt64Value, Round> _dPoSInfoMap;

        private readonly Map<UInt64Value, StringValue> _eBPMap;

        private readonly PbField<Timestamp> _timeForProducingExtraBlockField;

        private readonly Map<UInt64Value, StringValue> _firstPlaceMap;

        private readonly Int32Field _miningIntervalField;

        private readonly Map<BytesValue, Tickets> _balanceMap;

        private readonly PbField<Candidates> _candidatesField;

        private readonly Map<UInt64Value, ElectionSnapshot> _snapshotMap;

        #endregion

        public DPoS(AElfDPoSFieldMapCollection collection)
        {
            _currentRoundNumberField = collection.CurrentRoundNumberField;
            _ongoingMinersField = collection.OngoingMinersField;
            _dPoSInfoMap = collection.DPoSInfoMap;
            _eBPMap = collection.EBPMap;
            _timeForProducingExtraBlockField = collection.TimeForProducingExtraBlockField;
            _firstPlaceMap = collection.FirstPlaceMap;
            _miningIntervalField = collection.MiningIntervalField;
            _balanceMap = collection.BalanceMap;
            _candidatesField = collection.CandidatesField;
            _snapshotMap = collection.SnapshotField;
        }

        /// <inheritdoc />
        /// <summary>
        /// 1. Set block producers / miners;
        /// 2. Set current round number to 1;
        /// 3. Set mining interval;
        /// 4. Set first place of round 1 and 2 using AElfDPoSInformation;
        /// 5. Set DPoS information of first round to map;
        /// 6. Set EBP of round 1 and 2;
        /// 7. Set Extra Block mining time slot of current round (actually round 1).
        /// </summary>
        /// <param name="args">
        /// 3 args:
        /// [0] Miners
        /// [1] AElfDPoSInformation
        /// [2] SInt32Value
        /// </param>
        /// <returns></returns>
        public async Task Initialize(List<byte[]> args)
        {
            if (args.Count != 4)
            {
                return;
            }

            var round1 = new UInt64Value {Value = 1};
            var round2 = new UInt64Value {Value = 2};
            Miners miners;
            AElfDPoSInformation dPoSInfo;
            SInt32Value miningInterval;
            try
            {
                miners = Miners.Parser.ParseFrom(args[0]);
                dPoSInfo = AElfDPoSInformation.Parser.ParseFrom(args[1]);
                miningInterval = SInt32Value.Parser.ParseFrom(args[2]);
                LogLevel = Int32Value.Parser.ParseFrom(args[3]).Value;
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to parse from byte array.", e);
                return;
            }

            // 1. Set block producers;
            try
            {
                await InitializeBlockProducer(miners);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set block producers.", e);
            }


            // 2. Set current round number to 1;
            try
            {
                await UpdateCurrentRoundNumber(1);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to update current round number.", e);
            }

            // 3. Set mining interval;
            try
            {
                await SetMiningInterval(miningInterval);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set mining interval.", e);
            }

            // 4. Set first place of round 1 and 2 using DPoSInfo;
            try
            {
                await SetFirstPlaceOfSpecificRound(round1, dPoSInfo);
                await SetFirstPlaceOfSpecificRound(round2, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set first place.", e);
            }

            // 5. Set DPoS information of first round to map;
            try
            {
                await SetDPoSInfoToMap(round1, dPoSInfo.Rounds[0]);
                await SetDPoSInfoToMap(round2, dPoSInfo.Rounds[1]);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set DPoS information of first round to map.", e);
            }

            // 6. Set EBP of round 1 and 2;
            try
            {
                await SetExtraBlockProducerOfSpecificRound(round1, dPoSInfo);
                await SetExtraBlockProducerOfSpecificRound(round2, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set Extra Block Producer.", e);
            }

            // 7. Set Extra Block mining time slot of current round (actually round 1);
            try
            {
                await SetExtraBlockMiningTimeSlotOfSpecificRound(round1, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set Extra Block mining time slot.", e);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// 1. Supply DPoS information of current round (in case of some block producers failed to
        ///     publish their out value, signature or in value);
        /// 2. Set DPoS information of next round.
        /// </summary>
        /// <param name="args">
        /// 3 args:
        /// [0] Round
        /// [1] Round
        /// [2] StringValue
        /// [3] Int64Value
        /// </param>
        /// <returns></returns>
        public async Task Update(List<byte[]> args)
        {
            if (args.Count != 4)
            {
                return;
            }

            Round currentRoundInfo;
            Round nextRoundInfo;
            StringValue nextExtraBlockProducer;
            Int64Value roundId;

            try
            {
                currentRoundInfo = Round.Parser.ParseFrom(args[0]);
                nextRoundInfo = Round.Parser.ParseFrom(args[1]);
                nextExtraBlockProducer = StringValue.Parser.ParseFrom(args[2]);
                roundId = Int64Value.Parser.ParseFrom(args[3]);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Update), "Failed to parse from byte array.", e);
                return;
            }

            if (!await CheckRoundId(roundId))
            {
                return;
            }

            await SupplyDPoSInformationOfCurrentRound(currentRoundInfo);
            await SetDPoSInformationOfNextRound(nextRoundInfo, nextExtraBlockProducer);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="args">
        /// (I) Publish out value and signature
        /// 5 args:
        /// [0] UInt64Value
        /// [1] Hash
        /// [2] Hash
        /// [3] Int64Value
        /// 
        /// (II) Publish in value
        /// 4 args:
        /// [0] UInt64Value
        /// [1] Hash
        /// [2] Int64Value
        /// </param>
        /// <returns></returns>
        public async Task Publish(List<byte[]> args)
        {
            var fromAddressToHex = new StringValue {Value = Api.GetFromAddress().GetFormatted()};

            UInt64Value roundNumber;
            try
            {
                roundNumber = UInt64Value.Parser.ParseFrom(args[0]);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Publish), "Failed to parse from byte array.", e);
                return;
            }

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (args.Count == 4)
            {
                Hash outValue;
                Hash signature;
                Int64Value roundId;

                try
                {
                    outValue = Hash.Parser.ParseFrom(args[1]);
                    signature = Hash.Parser.ParseFrom(args[2]);
                    roundId = Int64Value.Parser.ParseFrom(args[3]);
                }
                catch (Exception e)
                {
                    ConsoleWriteLine(nameof(Publish), "Failed to parse from byte array.", e);
                    return;
                }

                if (!await CheckRoundId(roundId))
                {
                    return;
                }

                await PublishOutValueAndSignature(roundNumber, fromAddressToHex, outValue, signature);
            }

            if (args.Count == 3)
            {
                Hash inValue;
                Int64Value roundId;

                try
                {
                    inValue = Hash.Parser.ParseFrom(args[1]);
                    roundId = Int64Value.Parser.ParseFrom(args[2]);
                }
                catch (Exception e)
                {
                    ConsoleWriteLine(nameof(Publish), "Failed to parse from byte array.", e);
                    return;
                }

                if (!await CheckRoundId(roundId))
                {
                    return;
                }

                await PublishInValue(roundNumber, fromAddressToHex, inValue);
            }
        }

        public async Task Election(List<byte[]> args)
        {
            // QuitElection
            if (args.Count == 0)
            {
                var minerWannaQuitElection = Api.RecoverPublicKey();
                var candidates = await _candidatesField.GetAsync();
                if (candidates == null || !candidates.PubKeys.Contains(ByteString.CopyFrom(minerWannaQuitElection)))
                {
                    return;
                }

                var parameter =
                    ByteString.CopyFrom(ParamsPacker.Pack(minerWannaQuitElection));
                Api.Call(TokenContractAddress, "CancelElection", parameter.ToByteArray());
                candidates.PubKeys.Remove(ByteString.CopyFrom(minerWannaQuitElection));
                await _candidatesField.SetAsync(candidates);
            }

            // Replace
            if (args.Count == 1)
            {
                byte[] pubKey;

                try
                {
                    pubKey = args[0];
                }
                catch (Exception e)
                {
                    ConsoleWriteLine(nameof(Election), "Failed to parse from byte array.", e);
                    return;
                }

                if (pubKey.Any())
                {
                    await UpdateOngoingMiners(pubKey);
                }
                else
                {
                    // General election
                    await UpdateOngoingMiners(await GetVictories());
                }
            }

            // Vote
            if (args.Count == 3)
            {
                byte[] pubKey;
                UInt64Value amount;
                BoolValue voteOrNot;

                try
                {
                    pubKey = args[0];
                    amount = UInt64Value.Parser.ParseFrom(args[1]);
                    voteOrNot = BoolValue.Parser.ParseFrom(args[2]);

                }
                catch (Exception e)
                {
                    ConsoleWriteLine(nameof(Election), "Failed to parse from byte array.", e);
                    return;
                }

                if (voteOrNot.Value)
                {
                    await Vote(pubKey, amount);
                }
                else
                {
                    //await Regret(candidateAddress, amount);
                }
            }
        }

        public Miners GetCurrentMiners()
        {
            var ongoingMiners = _ongoingMinersField.GetValue();
            if (ongoingMiners == null || !ongoingMiners.Miners.Any())
            {
                ongoingMiners = new OngoingMiners();
            }

            return ongoingMiners.GetCurrentMiners(CurrentRoundNumber);
        }

        public async Task HandleTickets(byte[] pubKey, ulong amount, bool withdraw = false)
        {
            var bv = new BytesValue
            {
                Value = ByteString.CopyFrom(pubKey)
            };
            
            if (!withdraw)
            {
                
                if (_balanceMap.TryGet(bv, out var tickets))
                {
                    tickets.RemainingTickets += amount;
                    await _balanceMap.SetValueAsync(bv, tickets);
                }
                else
                {
                    tickets = new Tickets {RemainingTickets = amount};
                    await _balanceMap.SetValueAsync(bv, tickets);
                }
            }
            else
            {
                if (_balanceMap.TryGet(bv, out var tickets))
                {
                    Api.Assert(tickets.RemainingTickets >= amount,
                        $"{Api.GetFromAddress().GetFormatted()} can't withdraw tickets.");

                    tickets.RemainingTickets -= amount;
                    Api.Call(TokenContractAddress, "Transfer",
                        ByteString.CopyFrom(ParamsPacker.Pack(amount)).ToByteArray());
                    await _balanceMap.SetValueAsync(bv, tickets);
                }
            }

            Console.WriteLine($"{pubKey.ToHex()}'s tickets changed: {amount}");
        }

        public async Task AnnounceElection(byte[] candidatePubKey)
        {
            var candidates = await _candidatesField.GetAsync();
            if (candidates == null || !candidates.PubKeys.Any())
            {
                candidates = new Candidates();
            }

            candidates.PubKeys.Add(ByteString.CopyFrom(candidatePubKey));
            await _candidatesField.SetAsync(candidates);
        }

        /// <inheritdoc />
        /// <summary>
        /// Checking steps:
        /// 1. Contained by BlockProducer.Nodes;
        /// 2. Timestamp sitting in correct time slot of current round, or later than extra block time slot
        ///     if Extra Block Producer failed to produce extra block.
        /// 3. Should be different from current round if this block is about to update information of next
        ///     round.
        /// </summary>
        /// <param name="args">
        /// 2 args:
        /// [0] StringValue
        /// [1] Timestamp
        /// [2] Int64Value
        /// </param>
        /// <returns>
        /// 0: Success
        /// 1: NotBP
        /// 2: InvalidTimeSlot
        /// 3: SameWithCurrentRound
        /// 11: ParseProblem
        /// </returns>
        public async Task<int> Validation(List<byte[]> args)
        {
            byte[] pubKey;
            Timestamp timestamp;
            Int64Value roundId;
            try
            {
                pubKey = args[0];
                timestamp = Timestamp.Parser.ParseFrom(args[1]);
                roundId = Int64Value.Parser.ParseFrom(args[2]);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Validation), "Failed to parse from byte array.", e);
                return 11;
            }

            // 1. Contained by BlockProducer.Nodes;
            if (!IsBlockProducer(pubKey))
            {
                return 1;
            }

            // 2. Timestamp sitting in correct time slot of current round;
            var timeSlotOfBlockProducer = (await GetBPInfoOfCurrentRound(pubKey)).TimeSlot;
            var endOfTimeSlotOfBlockProducer = GetTimestampWithOffset(timeSlotOfBlockProducer, Interval);
            var timeSlotOfEBP = await _timeForProducingExtraBlockField.GetAsync();
            var validTimeSlot = CompareTimestamp(timestamp, timeSlotOfBlockProducer) &&
                                CompareTimestamp(endOfTimeSlotOfBlockProducer, timestamp) ||
                                CompareTimestamp(timestamp, timeSlotOfEBP);
            if (!validTimeSlot)
            {
                return 2;
            }

            var currentRound = await GetCurrentRoundInfo();
            if (roundId.Value != 1)
            {
                // 3. Is same with current round.
                if (currentRound.RoundId == roundId.Value)
                {
                    return 3;
                }
            }

            return 0;
        }

        #region Private Methods

        #region Important Privite Methods

        private async Task InitializeBlockProducer(Miners miners)
        {
            var candidates = new Candidates();
            foreach (var pubKey in miners.Producers)
            {
                ConsoleWriteLine(nameof(Initialize), $"Set miner {pubKey.ToByteArray().ToHex()} to state store.");

                candidates.PubKeys.Add(pubKey);

                // This should only happen on main chain. 
                var bv = new BytesValue
                {
                    Value = pubKey
                };
                if (_balanceMap.TryGet(bv, out var tickets))
                {
                    ConsoleWriteLine(nameof(InitializeBlockProducer),
                        $"Remaining tickets of {bv.Value.ToByteArray().ToHex()}: {tickets.RemainingTickets}");
                }
                 else
                {
                    // Miners in the white list
                    tickets = new Tickets {RemainingTickets = GlobalConfig.LockTokenForElection};
                    await _balanceMap.SetValueAsync(bv, tickets);
                    ConsoleWriteLine(nameof(InitializeBlockProducer),
                        $"Remaining tickets of {bv.Value.ToByteArray().ToHex()}: {tickets.RemainingTickets}");
                }
            }

            await _candidatesField.SetAsync(candidates);
            await UpdateOngoingMiners(miners);
        }

        private async Task UpdateOngoingMiners(Miners miners)
        {
            var ongoingMiners = await _ongoingMinersField.GetAsync();
            if (ongoingMiners == null || !ongoingMiners.Miners.Any())
            {
                ongoingMiners = new OngoingMiners();
            }

            miners.TakeEffectRoundNumber = CurrentRoundNumber.Add(1);
            ongoingMiners.UpdateMiners(miners);
            await _ongoingMinersField.SetAsync(ongoingMiners);

            var snapshot = new ElectionSnapshot();
            snapshot.EndRoundNumber = CurrentRoundNumber.Add(1);

            foreach (var candidate in _candidatesField.GetValue().PubKeys)
            {
                snapshot.TicketsMap.Add(new TicketsMap
                    {Candidate = candidate, TicketsCount = await GetTicketCount(candidate.ToByteArray())});
            }

            await _snapshotMap.SetValueAsync(new UInt64Value {Value = CurrentRoundNumber.Add(1)}, snapshot);
        }

        private async Task UpdateOngoingMiners(byte[] pubKey)
        {
            var ongoingMiners = await _ongoingMinersField.GetAsync();
            if (ongoingMiners == null || !ongoingMiners.Miners.Any())
            {
                return;
            }

            // Use the snapshot to do the replacement.
            var currentMiners = ongoingMiners.GetCurrentMiners(CurrentRoundNumber);
            if (_snapshotMap.TryGet(
                new UInt64Value {Value = currentMiners.TakeEffectRoundNumber},
                out var snapshot))
            {
                var nextMiner = snapshot.GetNextCandidate(currentMiners);
                currentMiners.Producers.Remove(ByteString.CopyFrom(pubKey));
                currentMiners.Producers.Add(ByteString.CopyFrom(nextMiner));
                currentMiners.TakeEffectRoundNumber = CurrentRoundNumber.Add(1);
                ongoingMiners.Miners.Add(currentMiners);
                await _ongoingMinersField.SetAsync(ongoingMiners);
            }
        }

        private async Task UpdateCurrentRoundNumber(ulong currentRoundNumber)
        {
            Console.WriteLine("current round number: " + currentRoundNumber);
            await _currentRoundNumberField.SetAsync(currentRoundNumber);
        }

        private async Task SetMiningInterval(SInt32Value interval)
        {
            await _miningIntervalField.SetAsync(interval.Value);
        }

        private async Task SetFirstPlaceOfSpecificRound(UInt64Value roundNumber, AElfDPoSInformation info)
        {
            await _firstPlaceMap.SetValueAsync(roundNumber,
                new StringValue {Value = info.GetRoundInfo(roundNumber.Value).BlockProducers.First().Key});
        }

        private async Task SetFirstPlaceOfSpecificRound(UInt64Value roundNumber, StringValue accountAddress)
        {
            await _firstPlaceMap.SetValueAsync(roundNumber, accountAddress);
        }

        private async Task SetDPoSInfoToMap(UInt64Value roundNumber, Round roundInfo)
        {
            await _dPoSInfoMap.SetValueAsync(roundNumber, roundInfo);
        }

        private async Task SetExtraBlockProducerOfSpecificRound(UInt64Value roundNumber, AElfDPoSInformation info)
        {
            await _eBPMap.SetValueAsync(roundNumber,
                info.GetExtraBlockProducerOfSpecificRound(roundNumber.Value));
        }

        private async Task SetExtraBlockProducerOfSpecificRound(UInt64Value roundNumber, StringValue extraBlockProducer)
        {
            await _eBPMap.SetValueAsync(roundNumber, extraBlockProducer);
        }

        private async Task SetExtraBlockMiningTimeSlotOfSpecificRound(UInt64Value roundNumber, AElfDPoSInformation info)
        {
            var lastMinerTimeSlot = info.GetLastBlockProducerTimeSlotOfSpecificRound(roundNumber.Value);
            var timeSlot = GetTimestampWithOffset(lastMinerTimeSlot, Interval);
            await _timeForProducingExtraBlockField.SetAsync(timeSlot);
        }

        private async Task SetExtraBlockMiningTimeSlotOfSpecificRound(Timestamp timestamp)
        {
            await _timeForProducingExtraBlockField.SetAsync(timestamp);
        }

        private async Task SupplyDPoSInformationOfCurrentRound(Round currentRoundInfo)
        {
            var currentRoundInfoFromDPoSMap = new Round();

            try
            {
                currentRoundInfoFromDPoSMap = await GetCurrentRoundInfo();
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Update), "Failed to get current RoundInfo.", e);
            }

            try
            {
                foreach (var infoPair in currentRoundInfoFromDPoSMap.BlockProducers)
                {
                    //If one Block Producer failed to publish his in value (with a tx),
                    //it means maybe something wrong happened to him.
                    if (infoPair.Value.InValue != null && infoPair.Value.OutValue != null)
                        continue;

                    //So the Extra Block Producer of this round will help him to supply all the needed information
                    //which contains in value, out value, signature.
                    var supplyValue = currentRoundInfo.BlockProducers.First(info => info.Key == infoPair.Key)
                        .Value;
                    infoPair.Value.InValue = supplyValue.InValue;
                    infoPair.Value.OutValue = supplyValue.OutValue;
                    infoPair.Value.Signature = supplyValue.Signature;
                }
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Update), "Failed to supply information of current round.", e);

                ConsoleWriteLine(nameof(Update), "Current RoundInfo:");

                foreach (var key in currentRoundInfo.BlockProducers.Keys)
                {
                    ConsoleWriteLine(nameof(Update), key);
                }
            }

            await SetCurrentRoundInfo(currentRoundInfoFromDPoSMap);
        }

        private async Task SetDPoSInformationOfNextRound(Round nextRoundInfo, StringValue nextExtraBlockProducer)
        {
            //Update Current Round Number.
            await UpdateCurrentRoundNumber();

            var newRoundNumber = new UInt64Value {Value = CurrentRoundNumber};

            //Update ExtraBlockProducer.
            await SetExtraBlockProducerOfSpecificRound(newRoundNumber, nextExtraBlockProducer);

            //Update RoundInfo.
            nextRoundInfo.BlockProducers.First(info => info.Key == nextExtraBlockProducer.Value).Value.IsEBP = true;

            //Update DPoSInfo.
            await SetDPoSInfoToMap(newRoundNumber, nextRoundInfo);

            //Update First Place.
            await SetFirstPlaceOfSpecificRound(newRoundNumber,
                new StringValue {Value = nextRoundInfo.BlockProducers.First().Key});

            //Update Extra Block Time Slot.
            await SetExtraBlockMiningTimeSlotOfSpecificRound(GetTimestampWithOffset(
                nextRoundInfo.BlockProducers.Last().Value.TimeSlot, Interval));

            ConsoleWriteLine(nameof(Update), $"Sync dpos info of round {CurrentRoundNumber} succeed.");
        }

        private async Task<Round> GetCurrentRoundInfo()
        {
            return await _dPoSInfoMap.GetValueAsync(new UInt64Value {Value = CurrentRoundNumber});
        }

        private async Task SetCurrentRoundInfo(Round currentRoundInfo)
        {
            await _dPoSInfoMap.SetValueAsync(new UInt64Value {Value = CurrentRoundNumber}, currentRoundInfo);
        }

        private async Task UpdateCurrentRoundNumber()
        {
            await _currentRoundNumberField.SetAsync(CurrentRoundNumber + 1);
        }

        private async Task PublishOutValueAndSignature(UInt64Value roundNumber, StringValue accountAddress,
            Hash outValue, Hash signature)
        {
            var info = await GetBPInfoOfSpecificRound(accountAddress, roundNumber);
            info.OutValue = outValue;
            if (roundNumber.Value > 1)
                info.Signature = signature;
            var roundInfo = await _dPoSInfoMap.GetValueAsync(roundNumber);
            roundInfo.BlockProducers[accountAddress.Value] = info;

            await _dPoSInfoMap.SetValueAsync(roundNumber, roundInfo);
        }

        private async Task PublishInValue(UInt64Value roundNumber, StringValue accountAddress, Hash inValue)
        {
            var info = await GetBPInfoOfSpecificRound(accountAddress, roundNumber);
            info.InValue = inValue;

            var roundInfo = await _dPoSInfoMap.GetValueAsync(roundNumber);
            roundInfo.BlockProducers[accountAddress.Value] = info;

            await _dPoSInfoMap.SetValueAsync(roundNumber, roundInfo);
        }

        private async Task<BlockProducer> GetBPInfoOfSpecificRound(StringValue accountAddress, UInt64Value roundNumber)
        {
            return (await _dPoSInfoMap.GetValueAsync(roundNumber)).BlockProducers[accountAddress.Value];
        }

        private async Task<BlockProducer> GetBPInfoOfCurrentRound(byte[] pubKey)
        {
            return (await _dPoSInfoMap.GetValueAsync(new UInt64Value {Value = CurrentRoundNumber})).BlockProducers[
                pubKey.ToHex()];
        }

        private bool IsBlockProducer(byte[] pubKey)
        {
            var miners = _ongoingMinersField.GetValue().GetCurrentMiners(CurrentRoundNumber);
            return miners.Producers.Contains(ByteString.CopyFrom(pubKey));
        }

        private async Task<Miners> GetVictories()
        {
            var candidates = await _candidatesField.GetAsync();
            var nodes = new List<Producer>();
            foreach (var candidate in candidates.PubKeys)
            {
                var ticketCount = await GetTicketCount(candidate.ToByteArray());
                nodes.Add(new Producer
                {
                    PubKey = candidate.ToByteArray(),
                    TicketCount = ticketCount
                });
            }

            return new Miners
            {
                Producers =
                {
                    nodes.OrderByDescending(n => n.TicketCount).Take(GlobalConfig.BlockProducerNumber)
                        .Select(n => ByteString.CopyFrom(n.PubKey))
                }
            };
        }

        private async Task Vote(byte[] pubKey, UInt64Value amount)
        {
            Api.Assert(CheckTickets(amount), $"Tickets of {Api.GetFromAddress().GetFormatted()} is not enough.");

            Api.Assert(await IsCandidate(pubKey), $"{pubKey.ToHex()} is not a candidate.");

            var bv = new BytesValue
            {
                Value = ByteString.CopyFrom(pubKey)
            };
            if (_balanceMap.TryGet(bv, out var tickets))
            {
                tickets.RemainingTickets.Add(amount.Value);
                var record = tickets.VotingRecord.FirstOrDefault();
                if (record != null)
                {
                    tickets.VotingRecord.Remove(record);
                    record.TicketsCount += amount.Value;
                    tickets.VotingRecord.Add(record);
                }
                else
                {
                    tickets.VotingRecord.Add(new VotingRecord
                    {
                        From = Api.GetFromAddress(),
                        TicketsCount = amount.Value
                    });
                }
            }

            await _balanceMap.SetValueAsync(bv, tickets);
        }

        private async Task<ulong> GetTicketCount(byte[] pubKey)
        {
            var bv = new BytesValue
            {
                Value = ByteString.CopyFrom(pubKey)
            };
            var balance = (await _balanceMap.GetValueAsync(bv)).RemainingTickets;
            return balance >= GlobalConfig.LockTokenForElection ? balance - GlobalConfig.LockTokenForElection : 0;
        }

        private async Task<bool> CheckRoundId(Int64Value roundId)
        {
            var currentRoundInfo = await GetCurrentRoundInfo();
            return currentRoundInfo.RoundId == roundId.Value;
        }

        private bool CheckTickets(UInt64Value amount)
        {
            var bv = new BytesValue
            {
                Value = Api.GetPublicKey()
            };
            if (_balanceMap.TryGet(bv, out var tickets))
            {
                return tickets.RemainingTickets >= amount.Value;
            }

            return false;
        }

        private async Task<bool> IsCandidate(byte[] pubKey)
        {
            var candidates = await _candidatesField.GetAsync();
            return candidates != null && candidates.PubKeys.Contains(ByteString.CopyFrom(pubKey));
        }

        #endregion

        #region Utilities

        private DateTime GetLocalTime()
        {
            return DateTime.UtcNow.ToLocalTime();
        }

        private Timestamp GetTimestampWithOffset(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
        }

        private void ConsoleWriteLine(string prefix, string log, Exception ex = null)
        {
            // Debug level: 6=Off, 5=Fatal 4=Error, 3=Warn, 2=Info, 1=Debug, 0=Trace
            // TODO logging by LogLevel
            if (LogLevel == 6)
                return;

            Console.WriteLine($"[{GetLocalTime():yyyy-MM-dd HH:mm:ss.fff} - AElfDPoS]{prefix} - {log}.");
            if (ex != null)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Return true if ts1 >= ts2
        /// </summary>
        /// <param name="ts1"></param>
        /// <param name="ts2"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private bool CompareTimestamp(Timestamp ts1, Timestamp ts2)
        {
            return ts1.ToDateTime() >= ts2.ToDateTime();
        }

        #endregion

        #endregion
    }
}