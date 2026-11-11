using UnityEngine;

namespace ShiftHappens
{
    // LW's puck-side positioning and RW's weak-side positioning are approved, including both lane cuts.
    // C's puck-side circle support and inside-shoulder assignment during the handoff and contain are approved.
    // Other positioning is draft.
    // Work in rink coordinates so a mirrored shift makes the same reads.
    internal sealed class ZoneSkating
    {
        internal static readonly Vector2 BlueNet = new(105f, 397f);
        private const float InsideShoulderBuffer = 12f;
        private const float WeakSideSlotOffset = 48f;
        private const float WingSkateSpeed = 47f;
        private const float PassCutSampleTime = 0.1f;
        private const float PassCutSkatingAllowance = 0.35f;
        private static readonly Vector2 UpperCircleDot = new(242f, 245f);
        private static readonly Vector2 UpperCircleSupport = new(242f, 282f);

        private sealed class Skater
        {
            public Vector2 Position { get; private set; }
            private Vector2 velocity;

            public Skater(Vector2 start) => Position = start;

            public void Skate(Vector2 target, float speed, float dt)
            {
                Vector2 offset = target - Position;
                Vector2 desiredVelocity = Vector2.ClampMagnitude(offset * 2.2f, speed);
                velocity = Vector2.MoveTowards(velocity, desiredVelocity, 105f * dt);
                Position += velocity * dt;
            }
        }

        private readonly DraftShiftSettings settings;
        private readonly float rhythm;
        private readonly Skater center, upperWing, lowerWing, slot, support, upperPoint, lowerPoint, redGoalie;
        private Vector2? upperPassCut;
        private Vector2? lowerPassCut;

        public Vector2 Center => settings.Map(center.Position);
        public Vector2 UpperWing => settings.Map(upperWing.Position);
        public Vector2 LowerWing => settings.Map(lowerWing.Position);
        public Vector2 Slot => settings.Map(slot.Position);
        public Vector2 Support => settings.Map(support.Position);
        public Vector2 UpperPoint => settings.Map(upperPoint.Position);
        public Vector2 LowerPoint => settings.Map(lowerPoint.Position);
        public Vector2 RedGoalie => settings.Map(redGoalie.Position);

        public ZoneSkating(DraftShiftSettings settings, int seed)
        {
            this.settings = settings;
            uint mixed = unchecked((uint)seed * 2654435761u);
            rhythm = (mixed >> 8) / 16777216f * 2f * Mathf.PI;
            center = new(settings.BlueCenter);
            upperWing = new(settings.BlueLeftWing);
            lowerWing = new(settings.BlueRightWing);
            slot = new(settings.NetFrontStart);
            support = new(settings.RedSupport);
            upperPoint = new(settings.RedRightDefense);
            lowerPoint = new(settings.RedLeftDefense);
            redGoalie = new(new Vector2(1163, 397));
        }

        public void Step(float dt, float time, Vector2 puck, Vector2 ld, Vector2 rd,
            bool supportContain, bool tied, bool loose, Vector2? slotReception, Vector2? pointReception,
            bool toUpperPoint, bool behindNetExited = false)
        {
            puck = settings.Map(puck);
            ld = settings.Map(ld);
            rd = settings.Map(rd);
            float upperSide = Mathf.InverseLerp(495, 299, puck.y);
            if (settings.BehindNetCarry && !behindNetExited) upperSide = 1;
            float cut = Mathf.Sin(time * 1.03f + rhythm);
            float cycle = Mathf.Sin(time * 0.71f + rhythm + 1.4f);

            // Blue C supports low from inside. The near wing takes the point lane and
            // the weak-side wing folds toward the high slot as the puck crosses the zone.
            Vector2 centerTarget = new(
                Mathf.Clamp(puck.x + 55f, 220f, 278f),
                Mathf.Lerp(397f, Mathf.Clamp(puck.y, 215f, 579f), 0.42f));
            // Both current contain routes are on the upper side, including when mirrored.
            if (supportContain) centerTarget = UpperCircleSupport;
            if (settings.BehindNetCarry)
                centerTarget = behindNetExited ? BehindNetRotation.ExitSupport
                    : puck.x < 99 ? BehindNetRotation.SlotSupport : centerTarget;
            if (loose && Vector2.Distance(center.Position, puck) < 115f)
                centerTarget = Vector2.Lerp(centerTarget, puck, 0.48f);
            centerTarget += SpaceFrom(centerTarget, ld, 55f) + SpaceFrom(centerTarget, rd, 55f);
            if (supportContain)
                centerTarget = UpperCircleDot + Vector2.ClampMagnitude(centerTarget - UpperCircleDot, 70f);
            center.Skate(InsideShoulderTarget(centerTarget, support.Position, BlueNet), 44f, dt);
            Vector2? reception = pointReception.HasValue ? settings.Map(pointReception.Value) : null;
            upperWing.Skate(PointDefenseTarget(upperWing.Position, puck, upperPoint.Position, upperSide, -1,
                toUpperPoint ? reception : null, ref upperPassCut), WingSkateSpeed, dt);
            lowerWing.Skate(PointDefenseTarget(lowerWing.Position, puck, lowerPoint.Position, 1f - upperSide, 1,
                toUpperPoint ? null : reception, ref lowerPassCut), WingSkateSpeed, dt);

            // Red's net-front skater changes the passing angle without abandoning the crease threat.
            Vector2 slotAnchor = settings.NetFrontStart;
            Vector2 slotTarget = slotAnchor + new Vector2(7f + 6f * cycle,
                10f * cut + Mathf.Clamp((slotAnchor.y - ld.y) * 0.12f, -9f, 9f));
            slotTarget += SpaceFrom(slotTarget, rd, 50f) * 0.25f;
            if (slotReception.HasValue) slotTarget = settings.Map(slotReception.Value);
            slot.Skate(tied ? slot.Position : slotTarget, tied ? 0 : 38f, dt);

            // Support offers an outlet away from pressure. Both points slide to show a stick.
            float pressureSide = Mathf.InverseLerp(-90, 90, ld.y - puck.y);
            Vector2 supportTarget = new(Mathf.Lerp(260f, 319f, pressureSide) + 20f * cut,
                Mathf.Clamp(puck.y + Mathf.Lerp(188f, 74f, pressureSide) + 40f * cycle, 216f, 605f));
            supportTarget += SpaceFrom(supportTarget, center.Position, 68f) +
                SpaceFrom(supportTarget, ld, 64f);
            if (loose) supportTarget = Vector2.Lerp(supportTarget, puck, 0.55f);
            support.Skate(supportTarget, 51f, dt);
            upperPoint.Skate(pointReception.HasValue && toUpperPoint ? settings.Map(pointReception.Value) :
                PointTarget(settings.RedRightDefense, upperWing.Position, puck, cycle, 133f, 340f), 44f, dt);
            lowerPoint.Skate(pointReception.HasValue && !toUpperPoint ? settings.Map(pointReception.Value) :
                PointTarget(settings.RedLeftDefense, lowerWing.Position, puck, -cycle, 454f, 656f), 44f, dt);
            redGoalie.Skate(new Vector2(1163, Mathf.Lerp(387f, 407f, 1f - upperSide)), 19f, dt);
        }

        private static Vector2 PointDefenseTarget(Vector2 wing, Vector2 puck, Vector2 opponent,
            float nearSide, int side, Vector2? reception, ref Vector2? passCut)
        {
            if (!reception.HasValue)
            {
                passCut = null;
                return WingTarget(puck, opponent, nearSide, side);
            }
            passCut ??= FindPassCut(wing, puck, reception.Value, opponent);
            return passCut.HasValue ? InsideShoulderTarget(passCut.Value, opponent, BlueNet)
                : WingTarget(puck, opponent, 1f, side);
        }

        private static Vector2? FindPassCut(Vector2 wing, Vector2 puck, Vector2 reception, Vector2 opponent)
        {
            Vector2 pass = reception - puck;
            float flight = pass.magnitude / PracticeShift.PassSpeed;
            Vector2 puckVelocity = pass.normalized * PracticeShift.PassSpeed;
            // Allow for acceleration and slowing as the wing approaches the lane.
            for (float time = PassCutSampleTime; time < flight; time += PassCutSampleTime)
            {
                Vector2 intercept = puck + puckVelocity * time;
                float skatingRange = WingSkateSpeed * Mathf.Max(0f, time - PassCutSkatingAllowance);
                if (InsideShoulderDepth(intercept, opponent, BlueNet) >= InsideShoulderBuffer &&
                    (wing - intercept).sqrMagnitude <= skatingRange * skatingRange)
                    return intercept;
            }
            return null;
        }

        private static Vector2 WingTarget(Vector2 puck, Vector2 point, float nearSide, int side)
        {
            Vector2 highSlot = new(306f, BlueNet.y + side * WeakSideSlotOffset);
            Vector2 pointLane = Vector2.Lerp(point, puck, 0.32f);
            pointLane.x = Mathf.Clamp(pointLane.x, 295f, 367f);
            pointLane.y = side < 0 ? Mathf.Clamp(pointLane.y, 167f, 327f) : Mathf.Clamp(pointLane.y, 467f, 627f);
            return InsideShoulderTarget(Vector2.Lerp(highSlot, pointLane, nearSide), point, BlueNet);
        }

        internal static float InsideShoulderDepth(Vector2 skater, Vector2 opponent, Vector2 net) =>
            Vector2.Dot(skater - opponent, (net - opponent).normalized);

        internal static Vector2 InsideShoulderTarget(Vector2 target, Vector2 opponent, Vector2 net)
        {
            float depth = InsideShoulderDepth(target, opponent, net);
            return target + (net - opponent).normalized * Mathf.Max(0f, InsideShoulderBuffer - depth);
        }

        private static Vector2 PointTarget(Vector2 anchor, Vector2 wing, Vector2 puck, float drift, float minY, float maxY)
        {
            float showStick = Mathf.Clamp((anchor.y - wing.y) * 0.46f, -52f, 52f);
            return new Vector2(Mathf.Clamp(anchor.x + (puck.x - 180f) * 0.12f + drift * 12f, 370f, 431f),
                Mathf.Clamp(anchor.y + showStick + drift * 43f, minY, maxY));
        }

        private static Vector2 SpaceFrom(Vector2 target, Vector2 skater, float radius)
        {
            Vector2 away = target - skater;
            return away.normalized * Mathf.Max(0, radius - away.magnitude);
        }
    }
}
