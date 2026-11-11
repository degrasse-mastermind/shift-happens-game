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
        private const float PassSpeed = 150f;
        private const float TieDuration = 2.5f;
        private static readonly Vector2[] CarrierRoute = {
            new(230, 315), new(205, 255), new(180, 215),
            new(175, 190), new(210, 220), new(190, 235)
        };
        private static readonly float[] RouteTimes = {0f, 3f, 6f, 9f, 12f, DecisionTime};

        public enum Phase { Ready, Carry, Warning, Pass, LoosePuck, Shot, Finished }
        public enum ReadResult { Pending, Met, Missed, Unobserved }
        public enum Possession { RedCarrier, InFlight, Loose, Blue, Goalie, None }
        public Phase Stage { get; private set; } = Phase.Ready;
        public int Seed { get; }
        public float Time { get; private set; }
        public Vector2 Player { get; private set; }
        public Vector2 Carrier { get; private set; }
        public Vector2 Puck { get; private set; }
        public Vector2 Partner { get; private set; }
        public Vector2 PartnerFacing { get; private set; } = Vector2.up;
        public Vector2 NetFrontAttacker => settings.Map(Center);
        public bool PartnerHasCoverage { get; private set; }
        public float? HandoffAt { get; private set; }
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
        public bool CanSkate => Stage != Phase.Ready && Stage != Phase.Finished && !Paused && !TeachingPaused;
        public Vector2 Destination => settings.Map(SlotFeed ? Center : Point);
        public Vector2 Goalie { get; private set; }
        public string DraftReview => $"DRAFT  Hold: {HoldRead}  |  Contain: {ContainRead}";

        private Vector2 target = StartPosition;
        private float passAt;
        private float tiedUntil;
        private float tieReadyAt;
        private Vector2 shotTarget;
        private readonly DraftShiftSettings settings;
        private float coverageStableTime;
        private float holdEligibleTime;
        private float holdCoveredTime;
        private float containEligibleTime;
        private float containedTime;
        private float openLaneTime;
        private float looseAt;
        private float pickupTime;
        private bool teachingShown;
        private bool slotFeed;
        private bool IsCarrierWindow => PuckPossession == Possession.RedCarrier &&
            (Stage == Phase.Carry || Stage == Phase.Warning);

        public PracticeShift(int seed, DraftShiftSettings scenario = null, bool guided = true)
        {
            Seed = seed;
            Guided = guided;
            settings = (scenario ?? new DraftShiftSettings()).Copy();
            Player = settings.Map(StartPosition);
            target = Player;
            Carrier = settings.Map(CarrierRoute[0]);
            Partner = settings.Map(settings.PartnerStart);
            Puck = Carrier + new Vector2(settings.Mirror ? 17 : -17, 9);
            Goalie = settings.Map(new Vector2(105, 397));
            slotFeed = (seed & 1) == 1;
        }

        public void TogglePlay()
        {
            if (Stage == Phase.Ready) Stage = Phase.Carry;
            else if (TeachingPaused)
            {
                TeachingPaused = false;
                Message = "Hold the middle until RD has coverage.";
            }
            else if (Stage != Phase.Finished) Paused = !Paused;
        }

        public void SkateTo(Vector2 icePoint)
        {
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
                Message = "Skate closer to Red C to tie him up.";
                return;
            }
            tiedUntil = Time + TieDuration;
            tieReadyAt = Time + 3.5f;
            Message = "Red C tied up. Hold your position.";
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
            Goalie = new Vector2(Goalie.x, Mathf.MoveTowards(Goalie.y,
                Mathf.Clamp(Stage == Phase.Shot ? shotTarget.y : Puck.y, 365f, 429f),
                (Stage == Phase.Shot ? 12f : 20f) * dt));
            MovePartner(dt);
            ObserveDecisions(dt);

            if (Guided && !teachingShown && !HandoffAt.HasValue && Stage == Phase.Carry && Time >= 0.8f)
            {
                teachingShown = true;
                TeachingPaused = true;
                Message = "RD is still returning. Protect the middle, then resume and read the handoff.";
                return;
            }

            switch (Stage)
            {
                case Phase.Carry:
                    SkateCarrier();
                    if (openLaneTime >= 0.5f && Time >= 1.5f)
                    {
                        slotFeed = true;
                        WarnForPass("The middle opened. Red looks for the net-front pass.");
                        break;
                    }
                    if (Containing && containedTime >= settings.ContainHoldTime)
                    {
                        slotFeed = false;
                        ReleasePass("LD closes from inside. Red cycles to the point.");
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
                        DistanceToLane(Partner, previous, Puck) < 18f)
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
                    shotTarget = settings.Map(new Vector2(99, SlotFeed ? 373 : 397));
                    Stage = Phase.Shot;
                    Message = SlotFeed ? "Red C shoots from the slot." : "Red shoots from the point.";
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
            for (int i = 1; i < RouteTimes.Length; i++)
            {
                if (Time > RouteTimes[i] && i < RouteTimes.Length - 1) continue;
                float progress = Mathf.InverseLerp(RouteTimes[i - 1], RouteTimes[i], Time);
                Carrier = settings.Map(Vector2.Lerp(CarrierRoute[i - 1], CarrierRoute[i], progress));
                Puck = Carrier + new Vector2(settings.Mirror ? 17 : -17, 9);
                break;
            }
        }

        private void ReleasePass(string message = null)
        {
            // A covered slot changes the attacking choice, including the seed-1 comparison branch.
            if (SlotFeed && NetFrontLaneCovered()) slotFeed = false;
            Stage = Phase.Pass;
            PuckPossession = Possession.InFlight;
            Containing = false;
            FinalizeReads();
            Message = message ?? (SlotFeed ? "Red passes to Red C." : "Red cycles the puck to the point.");
        }

        private void MovePartner(float dt)
        {
            Vector2 coverageTarget = settings.Map(Center + settings.PartnerCoverageOffset);
            if (Time >= settings.PartnerReturnDelay)
                Partner = Vector2.MoveTowards(Partner, coverageTarget, settings.PartnerReturnSpeed * dt);
            Vector2 lookTarget = (Puck + NetFrontAttacker) * 0.5f;
            Vector2 direction = (lookTarget - Partner).normalized;
            PartnerFacing = Vector3.RotateTowards(PartnerFacing, direction, 3f * dt, 0f);
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
            if (!IsCarrierWindow) return;
            bool covered = NetFrontLaneCovered();
            openLaneTime = covered ? 0 : openLaneTime + dt;
            if (!HandoffAt.HasValue)
            {
                holdEligibleTime += dt;
                if (CoversNetFront(Player)) holdCoveredTime += dt;
            }
            if (ContainmentAvailable)
            {
                containEligibleTime += dt;
                if (Containing) containedTime += dt;
            }
        }

        private bool CoversNetFront(Vector2 defender)
        {
            Vector2 local = settings.Map(defender);
            return local.x >= 150f && local.x <= Center.x - settings.GoalSideMargin &&
                Vector2.Distance(defender, NetFrontAttacker) <= settings.CoverageReach &&
                DistanceToLane(defender, Puck, NetFrontAttacker) <= settings.LaneReach;
        }

        private bool InsideContainment()
        {
            Vector2 localPlayer = settings.Map(Player);
            Vector2 localCarrier = settings.Map(Carrier);
            return localPlayer.x <= localCarrier.x + 10f &&
                Vector2.Distance(Player, Carrier) <= settings.ContainDistance &&
                DistanceToLane(Player, Carrier, NetFrontAttacker) <= settings.LaneReach;
        }

        private bool NetFrontLaneCovered() => CoversNetFront(Player) || PartnerHasCoverage;

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
            if (ContainRead == ReadResult.Pending)
                ContainRead = containedTime > 0 && containedTime >= settings.ContainHoldTime ? ReadResult.Met
                    : containEligibleTime < settings.ContainHoldTime + 0.5f ? ReadResult.Unobserved : ReadResult.Missed;
        }

        private ReadResult EvaluateHold() => holdEligibleTime < 0.5f ? ReadResult.Unobserved
            : holdCoveredTime / holdEligibleTime >= settings.RequiredHoldCoverage ? ReadResult.Met : ReadResult.Missed;

        private void Finish(string message)
        {
            FinalizeReads();
            Stage = Phase.Finished;
            Message = message;
        }
    }
}
