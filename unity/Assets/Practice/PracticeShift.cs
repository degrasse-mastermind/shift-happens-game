using UnityEngine;

namespace ShiftHappens
{
    public sealed class PracticeShift
    {
        public const float WarningDuration = 1.5f;
        public const float StickReach = 75f;
        public static readonly Vector2 StartPosition = new(185, 360);
        public static readonly Vector2 Center = new(204, 402);
        public static readonly Vector2 Point = new(390, 150);

        private const float DecisionTime = 14f;
        private const float SkateSpeed = 53f;
        internal const float PassSpeed = 150f;
        private const float TieDuration = 2.5f;
        private static readonly Vector2[] StandardCarrierWaypoints = {
            new(205, 255), new(180, 215),
            new(175, 190), new(210, 220), new(190, 235)
        };
        private static readonly Vector2[] CornerCarrierWaypoints = {
            new(132, 190), new(177, 215), new(183, 205), new(203, 221), new(190, 235)
        };
        private static readonly float[] RouteTimes = {0f, 3f, 6f, 9f, 12f, DecisionTime};

        public enum Phase { Ready, Carry, Warning, Pass, LoosePuck, Shot, Finished }
        public enum ReadResult { Pending, Met, Missed, Unobserved }
        public enum Possession { RedCarrier, InFlight, Loose, Blue, Goalie, None }
        public Phase Stage { get; private set; } = Phase.Ready;
        public int Seed { get; }
        internal bool Mirrored => settings.Mirror;
        public float Time { get; private set; }
        public Vector2 Player { get; private set; }
        public Vector2 Carrier { get; private set; }
        public Vector2 Puck { get; private set; }
        public Vector2 Partner { get; private set; }
        public Vector2 PartnerFacing { get; private set; } = Vector2.up;
        public Vector2 NetFrontAttacker => skating.Slot;
        public Vector2 BlueCenter => skating.Center;
        public Vector2 BlueLeftWing => skating.UpperWing;
        public Vector2 BlueRightWing => skating.LowerWing;
        public Vector2 RedSupport => skating.Support;
        public Vector2 RedRightDefense => skating.UpperPoint;
        public Vector2 RedLeftDefense => skating.LowerPoint;
        public Vector2 RedGoalie => skating.RedGoalie;
        public bool PartnerHasCoverage { get; private set; }
        public bool PartnerArrivedWrongSide => Vector2.Distance(Partner, PartnerCoverageTarget) <= 5f &&
            settings.Map(Partner).x >= settings.Map(NetFrontAttacker).x;
        public float? HandoffAt { get; private set; }
        public float? BehindNetExitAt { get; private set; }
        public bool BehindNetScenario => settings.BehindNetCarry;
        public bool BehindNetCrossing => BehindNetScenario && BehindNetRotation.BehindGoal(settings.Map(Carrier));
        public bool ContainmentAvailable => PartnerHasCoverage && IsCarrierWindow && Carrier.y < 397f;
        public bool Containing { get; private set; }
        public ReadResult HoldRead { get; private set; }
        public ReadResult ContainRead { get; private set; }
        public Possession PuckPossession { get; private set; } = Possession.RedCarrier;
        public bool Guided { get; }
        public bool TeachingPaused { get; private set; }
        public string Message { get; private set; } = "RD returns from the bottom corner. Protect the middle.";
        public bool Tied => Time < tiedUntil;
        public bool SlotFeed => slotFeed;
        public bool Paused { get; private set; }
        public bool Reviewing { get; private set; }
        public void SetReviewing(bool active) => Reviewing = active;
        public bool CanSkate => !Reviewing && Stage != Phase.Ready && Stage != Phase.Finished && !Paused && !TeachingPaused;
        public Vector2 Destination => Stage == Phase.Pass ? passTarget :
            SlotFeed ? NetFrontAttacker : PointReceiver;
        public string PassReceiver => SlotFeed ? settings.NetFrontLabel : toUpperPoint ? "RD" : "LD";
        public bool SlotLaneCovered => NetFrontLaneCovered();
        public Vector2 Goalie { get; private set; }
        public string ScoreSummary => BehindNetScenario
            ? $"DRAFT SCORE  Hold inside: {HoldRead}  |  Cover Red C: {ContainRead}"
            : $"FINAL SCORE  Hold: {HoldRead}{(openedSlotBeforeHandoff ? " (left net front and opened slot)" : "")}  |  Contain: {ContainRead}";
        public string PositioningSummary
        {
            get
            {
                if (Stage != Phase.Ready && !IsCarrierWindow)
                    return "Inside-shoulder positioning is reviewed while Red carries.";
                if (BehindNetScenario)
                    return $"Draft net-front read: LD {(PlayerCoversNetFront() ? "covers Red C" : "leaves Red C open")}.";
                Vector2 net = settings.Map(ZoneSkating.BlueNet);
                string Shoulder(Vector2 defender, Vector2 threat) =>
                    ZoneSkating.InsideShoulderDepth(defender, threat, net) > 0f ? "inside" : "outside";
                return $"Inside shoulder   LD (draft) {Shoulder(Player, ContainmentAvailable ? Carrier : NetFrontAttacker)}   " +
                    $"RD (final) {Shoulder(Partner, NetFrontAttacker)}   C {Shoulder(BlueCenter, RedSupport)}   " +
                    $"LW {Shoulder(BlueLeftWing, RedRightDefense)}   RW {Shoulder(BlueRightWing, RedLeftDefense)}";
            }
        }

        private Vector2 target = StartPosition;
        private float passAt;
        private float tiedUntil;
        private float tieReadyAt;
        private Vector2 shotTarget;
        private Vector2 passTarget;
        private bool toUpperPoint = true;
        private readonly DraftShiftSettings settings;
        private readonly ZoneSkating skating;
        private float coverageStableTime;
        private float holdEligibleTime;
        private float holdCoveredTime;
        private float containEligibleTime;
        private float exitCoveredTime;
        private float containStreakTime;
        private bool completedContainStreak;
        private float openLaneTime;
        private float looseAt;
        private float pickupTime;
        private bool teachingShown;
        private bool slotFeed;
        private bool openedSlotBeforeHandoff;
        private Vector2 PartnerCoverageTarget
        {
            get
            {
                Vector2 threat = NetFrontAttacker;
                Vector2 localTarget = settings.Map(threat) + settings.PartnerCoverageOffset;
                if (settings.PartnerCoverageOffset.x < 0f) localTarget.x = Mathf.Max(150f, localTarget.x);
                Vector2 target = settings.Map(localTarget);
                // A rink-side offset intentionally lets the wrong-side drill test a missed assignment.
                return settings.PartnerCoverageOffset.x < 0f
                    ? ZoneSkating.InsideShoulderTarget(target, threat, settings.Map(ZoneSkating.BlueNet))
                    : target;
            }
        }
        private Vector2 PointReceiver => toUpperPoint ? RedRightDefense : RedLeftDefense;
        private bool IsCarrierWindow => PuckPossession == Possession.RedCarrier &&
            (Stage == Phase.Carry || Stage == Phase.Warning);

        public PracticeShift(int seed, DraftShiftSettings scenario = null, bool guided = true)
        {
            Seed = seed;
            Guided = guided;
            settings = (scenario ?? new DraftShiftSettings()).Copy();
            skating = new ZoneSkating(settings, seed);
            Player = settings.Map(settings.PlayerStart);
            target = Player;
            Carrier = settings.Map(settings.CarrierStart);
            Partner = settings.Map(settings.PartnerStart);
            Puck = settings.Map(settings.PuckStart);
            if (settings.CornerCarry)
                Message = $"Red {settings.CarrierLabel} has the puck in the upper corner. Protect the lane to Red {settings.NetFrontLabel}.";
            if (BehindNetScenario)
                Message = "Red starts on your side. Hold inside and watch Red C.";
            Goalie = settings.Map(ZoneSkating.BlueNet);
            slotFeed = (seed & 1) == 1;
        }

        public void TogglePlay()
        {
            if (Reviewing) return;
            if (Stage == Phase.Ready) Stage = Phase.Carry;
            else if (TeachingPaused)
            {
                TeachingPaused = false;
                Message = BehindNetScenario ? "Keep Red C in sight. RD has the other post."
                    : "Hold the middle until RD has coverage.";
            }
            else if (Stage != Phase.Finished) Paused = !Paused;
        }

        public void SkateTo(Vector2 icePoint)
        {
            if (Reviewing) return;
            target = new Vector2(Mathf.Clamp(icePoint.x, 55, 1205), Mathf.Clamp(icePoint.y, 120, 675));
        }

        public void TieUp()
        {
            if (!CanSkate || Tied || Time < tieReadyAt) return;
            if (Stage == Phase.Shot)
            {
                Message = "The shot has already left the stick.";
                return;
            }
            if (Vector2.Distance(Player, NetFrontAttacker) > StickReach)
            {
                Message = $"Skate closer to Red {settings.NetFrontLabel} to tie up the receiver.";
                return;
            }
            tiedUntil = Time + TieDuration;
            tieReadyAt = Time + 3.5f;
            Message = $"Red {settings.NetFrontLabel} tied up. Hold your position.";
        }

        public void Step(float dt)
        {
            if (!CanSkate || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            // Bounded substeps keep coverage windows and puck interception consistent across frame rates.
            dt = Mathf.Min(dt, 0.25f);
            while (dt > 0 && CanSkate)
            {
                float step = Mathf.Min(dt, 0.02f);
                Advance(step);
                dt -= step;
            }
        }

        private void Advance(float dt)
        {
            Time += dt;
            Player = Vector2.MoveTowards(Player, target, SkateSpeed * dt);
            if (Tied && Vector2.Distance(Player, NetFrontAttacker) > 90f) tiedUntil = Time;
            if (Stage == Phase.Carry) SkateCarrier();
            skating.Step(dt, Time, Puck, Player, Partner, CenterSupportsContain(), Tied, Stage == Phase.LoosePuck,
                Stage == Phase.Pass && SlotFeed ? passTarget : null,
                Stage == Phase.Pass && !SlotFeed ? passTarget : null, toUpperPoint, BehindNetExitAt.HasValue);
            Goalie = new Vector2(Goalie.x, Mathf.MoveTowards(Goalie.y,
                Mathf.Clamp(Stage == Phase.Shot ? shotTarget.y : Puck.y, 365f, 429f),
                (Stage == Phase.Shot ? 12f : 20f) * dt));
            MovePartner(dt);
            ObserveDecisions(dt);

            if (Guided && !teachingShown && !HandoffAt.HasValue && Stage == Phase.Carry &&
                (BehindNetScenario ? BehindNetCrossing : Time >= 0.8f))
            {
                teachingShown = true;
                TeachingPaused = true;
                Message = BehindNetScenario ? "Hold inside. RD seals the other post and C protects the slot."
                    : "RD is still returning. Protect the middle, then resume and read the handoff.";
                return;
            }

            switch (Stage)
            {
                case Phase.Carry:
                    if (BehindNetScenario)
                    {
                        ChooseBehindNetPass();
                        break;
                    }
                    if (openLaneTime >= 0.5f && Time >= 1.5f)
                    {
                        if (!HandoffAt.HasValue) openedSlotBeforeHandoff = true;
                        slotFeed = true;
                        WarnForPass("The middle opened. Red looks for the net-front pass.");
                        break;
                    }
                    if (Containing && completedContainStreak)
                    {
                        slotFeed = false;
                        ReleasePass(contained: true);
                        break;
                    }
                    if (Time < DecisionTime) break;
                    if (SlotFeed && !NetFrontLaneCovered()) WarnForPass("Red looks for a pass to the slot.");
                    else ReleasePass();
                    break;
                case Phase.Warning:
                    if (Time >= passAt) ReleasePass();
                    break;
                case Phase.Pass:
                    Vector2 previous = Puck;
                    Puck = Vector2.MoveTowards(Puck, Destination, PassSpeed * dt);
                    if (DistanceToLane(Player, previous, Puck) < 18f ||
                        DistanceToLane(Partner, previous, Puck) < 18f ||
                        DistanceToLane(BlueCenter, previous, Puck) < 16f ||
                        DistanceToLane(BlueLeftWing, previous, Puck) < 16f ||
                        DistanceToLane(BlueRightWing, previous, Puck) < 16f)
                    {
                        DeflectPass("Blue's stick breaks up the passing lane. Recover the loose puck.");
                        break;
                    }
                    if (Puck != Destination) break;
                    if (SlotFeed && (Tied || NetFrontLaneCovered()))
                    {
                        DeflectPass(Tied ? "Your tie-up denies the reception. Recover the loose puck."
                            : "Net-front coverage denies the reception. Recover the loose puck.");
                        break;
                    }
                    Vector2 receiver = SlotFeed ? NetFrontAttacker : PointReceiver;
                    if (Vector2.Distance(receiver, Puck) > 25f)
                    {
                        DeflectPass("The pass misses the receiver's stick. Recover the loose puck.");
                        break;
                    }
                    shotTarget = settings.Map(new Vector2(99, SlotFeed ? 373 : 397));
                    Stage = Phase.Shot;
                    Message = SlotFeed ? $"Red {PassReceiver} shoots from the slot." : $"Red {PassReceiver} shoots from the point.";
                    break;
                case Phase.LoosePuck:
                    pickupTime = Vector2.Distance(Player, Puck) < 30f ? pickupTime + dt : 0;
                    if (pickupTime >= 0.3f)
                    {
                        PuckPossession = Possession.Blue;
                        Puck = Player;
                        Finish("Blue establishes control of the loose puck.");
                    }
                    else if (Time - looseAt >= 3f) Finish("Loose puck remains contested. Review and retry.");
                    break;
                case Phase.Shot:
                    Puck = Vector2.MoveTowards(Puck, shotTarget, 300f * dt);
                    if (Puck != shotTarget) break;
                    bool saved = Mathf.Abs(Goalie.y - shotTarget.y) < 32f;
                    PuckPossession = saved ? Possession.Goalie : Possession.None;
                    Finish(saved ? "Save. Blue's goalie covers the puck." : "Goal. Red beats Blue's goalie.");
                    break;
            }
            if (Time >= settings.ShiftDuration && Stage != Phase.Finished) Finish("Shift time reached. Review and retry.");
        }

        private void SkateCarrier()
        {
            if (BehindNetScenario)
            {
                Vector2 local = BehindNetRotation.CarrierAt(settings.CarrierStart, Time, settings.BehindNetWait);
                Carrier = settings.Map(local);
                Puck = settings.Map(local + settings.PuckStart - settings.CarrierStart);
                if (!BehindNetExitAt.HasValue && BehindNetRotation.Exited(local))
                {
                    BehindNetExitAt = Time;
                    HoldRead = EvaluateHold();
                    openLaneTime = 0;
                    Message = Guided ? "RD takes the carrier. Stay goal-side of Red C."
                        : "Red comes out on the other side.";
                }
                return;
            }
            Vector2[] waypoints = settings.CornerCarry ? CornerCarrierWaypoints : StandardCarrierWaypoints;
            for (int i = 1; i < RouteTimes.Length; i++)
            {
                if (Time > RouteTimes[i] && i < RouteTimes.Length - 1) continue;
                float progress = Mathf.InverseLerp(RouteTimes[i - 1], RouteTimes[i], Time);
                Vector2 start = i == 1 ? settings.CarrierStart : waypoints[i - 2];
                Vector2 carrierPosition = Vector2.Lerp(start, waypoints[i - 1], progress);
                Vector2 puckOffset = Vector2.Lerp(settings.PuckStart - settings.CarrierStart,
                    new Vector2(-17, 9), Mathf.Clamp01(Time / RouteTimes[1]));
                Carrier = settings.Map(carrierPosition);
                Puck = settings.Map(carrierPosition + puckOffset);
                break;
            }
        }

        private void ChooseBehindNetPass()
        {
            if (!BehindNetExitAt.HasValue) return;
            if (openLaneTime >= .5f)
            {
                slotFeed = true;
                WarnForPass("The slot opened. Red looks for Red C.");
                return;
            }
            if (Time - BehindNetExitAt.Value < 1.7f) return;
            slotFeed = false;
            ReleasePass();
        }

        private void ReleasePass(bool contained = false)
        {
            // A covered slot changes the attacking choice, including the seed-1 comparison branch.
            if (SlotFeed && NetFrontLaneCovered()) slotFeed = false;
            if (!SlotFeed)
                toUpperPoint = OutletScore(RedRightDefense) >= OutletScore(RedLeftDefense);
            passTarget = SlotFeed ? NetFrontAttacker : PointReceiver;
            Stage = Phase.Pass;
            PuckPossession = Possession.InFlight;
            Containing = false;
            FinalizeReads();
            Message = (contained ? "LD closes from inside. " : "") +
                (SlotFeed ? $"Red passes to Red {PassReceiver}." : $"Red cycles to Red {PassReceiver} at the point.");
        }

        private float OutletScore(Vector2 outlet)
        {
            float defense = Mathf.Min(
                Mathf.Min(DistanceToLane(Player, Puck, outlet), DistanceToLane(Partner, Puck, outlet)),
                Mathf.Min(DistanceToLane(BlueCenter, Puck, outlet),
                    Mathf.Min(DistanceToLane(BlueLeftWing, Puck, outlet), DistanceToLane(BlueRightWing, Puck, outlet))));
            return defense - Vector2.Distance(Puck, outlet) * 0.07f;
        }

        private void MovePartner(float dt)
        {
            if (BehindNetScenario)
            {
                Vector2 target = BehindNetRotation.PartnerTarget(settings.Map(Carrier), BehindNetExitAt.HasValue);
                Partner = Vector2.MoveTowards(Partner, settings.Map(target), settings.PartnerReturnSpeed * dt);
                Vector2 look = (Puck - Partner).normalized;
                if (look.sqrMagnitude > 0) PartnerFacing = look;
                return;
            }
            if (Time >= settings.PartnerReturnDelay)
                Partner = Vector2.MoveTowards(Partner, PartnerCoverageTarget, settings.PartnerReturnSpeed * dt);
            Vector2 lookTarget = (Puck + NetFrontAttacker) * 0.5f;
            Vector2 direction = (lookTarget - Partner).normalized;
            float facing = Mathf.Atan2(PartnerFacing.y, PartnerFacing.x) * Mathf.Rad2Deg;
            float bearing = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float turned = Mathf.MoveTowardsAngle(facing, bearing, 3f * Mathf.Rad2Deg * dt) * Mathf.Deg2Rad;
            PartnerFacing = new Vector2(Mathf.Cos(turned), Mathf.Sin(turned));
            bool positioned = CoversNetFront(Partner) && Vector2.Dot(PartnerFacing, direction) > 0.5f;
            coverageStableTime = positioned ? coverageStableTime + dt : 0;
            bool established = positioned && coverageStableTime >= settings.CoverageSettleTime;
            bool wasEstablished = PartnerHasCoverage;
            PartnerHasCoverage = established;
            if (established && IsCarrierWindow && !HandoffAt.HasValue)
            {
                HandoffAt = Time;
                HoldRead = EvaluateHold();
            }
            if (Guided && Stage == Phase.Carry && established != wasEstablished)
                Message = established ? "RD has net front. Close from inside to contain."
                    : "RD lost coverage. Recover inside and protect the middle.";
        }

        private void ObserveDecisions(float dt)
        {
            Containing = ContainmentAvailable && InsideContainment();
            containStreakTime = Containing ? containStreakTime + dt : 0;
            if (Containing && (containStreakTime >= settings.ContainHoldTime ||
                Mathf.Approximately(containStreakTime, settings.ContainHoldTime)))
                completedContainStreak = true;
            if (!IsCarrierWindow) return;
            bool covered = NetFrontLaneCovered();
            openLaneTime = covered ? 0 : openLaneTime + dt;
            if (BehindNetScenario && BehindNetExitAt.HasValue)
            {
                containEligibleTime += dt;
                if (covered) exitCoveredTime += dt;
                return;
            }
            if (!HandoffAt.HasValue)
            {
                holdEligibleTime += dt;
                if (PlayerCoversNetFront()) holdCoveredTime += dt;
            }
            if (ContainmentAvailable) containEligibleTime += dt;
        }

        private bool CoversNetFront(Vector2 defender)
        {
            Vector2 local = settings.Map(defender);
            return local.x >= 150f && local.x <= settings.Map(NetFrontAttacker).x - settings.GoalSideMargin &&
                Vector2.Distance(defender, NetFrontAttacker) <= settings.CoverageReach &&
                DistanceToLane(defender, Puck, NetFrontAttacker) <= settings.LaneReach;
        }

        private bool PlayerCoversNetFront()
        {
            if (BehindNetScenario)
                return ZoneSkating.InsideShoulderDepth(Player, NetFrontAttacker, settings.Map(ZoneSkating.BlueNet)) >= settings.GoalSideMargin &&
                    Vector2.Distance(Player, NetFrontAttacker) <= settings.CoverageReach;
            // In the marked setup LD starts up the lane from LW and guards the feed from inside.
            if (!settings.HoldFromPassingLane) return CoversNetFront(Player);
            return settings.Map(Player).x <= settings.Map(NetFrontAttacker).x - settings.GoalSideMargin &&
                DistanceToLane(Player, Puck, NetFrontAttacker) <= settings.LaneReach;
        }

        private bool CenterSupportsContain()
        {
            if (!ContainmentAvailable || !OnInsideLane()) return false;
            float range = Vector2.Distance(Player, Carrier);
            float holdRange = Vector2.Distance(settings.Map(settings.PlayerStart), Carrier);
            return range <= settings.ContainDistance || range + 10f < holdRange;
        }

        private bool InsideContainment() => OnInsideLane() &&
            Vector2.Distance(Player, Carrier) <= settings.ContainDistance;

        private bool OnInsideLane()
        {
            Vector2 localPlayer = settings.Map(Player);
            Vector2 localCarrier = settings.Map(Carrier);
            return localPlayer.x <= localCarrier.x + 10f &&
                DistanceToLane(Player, Carrier, NetFrontAttacker) <= settings.LaneReach;
        }

        private bool NetFrontLaneCovered() => PlayerCoversNetFront() || PartnerHasCoverage;

        private static float DistanceToLane(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 lane = end - start;
            float fraction = lane.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - start, lane) / lane.sqrMagnitude) : 0;
            return Vector2.Distance(point, start + lane * fraction);
        }

        private void WarnForPass(string message)
        {
            Stage = Phase.Warning;
            passAt = Time + WarningDuration;
            Message = message;
        }

        private void DeflectPass(string message)
        {
            Stage = Phase.LoosePuck;
            PuckPossession = Possession.Loose;
            looseAt = Time;
            Message = message;
        }

        private void FinalizeReads()
        {
            if (HoldRead == ReadResult.Pending)
                HoldRead = EvaluateHold();
            if (BehindNetScenario)
            {
                if (ContainRead == ReadResult.Pending)
                    ContainRead = containEligibleTime < .5f ? ReadResult.Unobserved
                        : exitCoveredTime / containEligibleTime >= settings.RequiredHoldCoverage ? ReadResult.Met : ReadResult.Missed;
                return;
            }
            if (ContainRead == ReadResult.Pending)
                ContainRead = completedContainStreak ? ReadResult.Met
                    : containEligibleTime < settings.ContainHoldTime + 0.5f ? ReadResult.Unobserved : ReadResult.Missed;
        }

        private ReadResult EvaluateHold() => openedSlotBeforeHandoff ? ReadResult.Missed
            : holdEligibleTime < 0.5f ? ReadResult.Unobserved
            : holdCoveredTime / holdEligibleTime >= settings.RequiredHoldCoverage ? ReadResult.Met : ReadResult.Missed;

        private void Finish(string message)
        {
            FinalizeReads();
            Stage = Phase.Finished;
            Message = message;
        }
    }
}
