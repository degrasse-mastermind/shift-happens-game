using UnityEngine;

namespace ShiftHappens
{
    public sealed class PracticeShift
    {
        public const float WarningDuration = 1.5f;
        public const float StickReach = 75f;
        public static readonly Vector2 StartPosition = new(185, 360);
        public static readonly Vector2 Center = new(204, 402);
        public static readonly Vector2 Point = new(348, 525);

        private const float DecisionTime = 14f;
        private const float SkateSpeed = 53f;
        private const float PassSpeed = 150f;
        private const float TieDuration = 2.5f;
        private static readonly Vector2[] CarrierRoute = {
            new(230, 315), new(145, 290), new(70, 335),
            new(70, 450), new(153, 490), new(205, 475)
        };
        private static readonly float[] RouteTimes = {0f, 3f, 6f, 9f, 12f, DecisionTime};

        public enum Phase { Ready, Carry, Warning, Pass, Shot, Finished }
        public Phase Stage { get; private set; } = Phase.Ready;
        public int Seed { get; }
        public float Time { get; private set; }
        public Vector2 Player { get; private set; } = StartPosition;
        public Vector2 Carrier { get; private set; } = CarrierRoute[0];
        public Vector2 Puck { get; private set; } = CarrierRoute[0] + new Vector2(-17, 9);
        public string Message { get; private set; } = "Protect the inside while Red carries behind the net.";
        public bool Tied => Time < tiedUntil;
        public bool SlotFeed => (Seed & 1) == 1;
        public bool Paused { get; private set; }
        public bool CanSkate => Stage != Phase.Ready && Stage != Phase.Finished && !Paused;
        public Vector2 Destination => SlotFeed ? Center : Point;
        public Vector2 Goalie { get; private set; } = new(105, 397);

        private Vector2 target = StartPosition;
        private float passAt;
        private float tiedUntil;
        private float tieReadyAt;
        private Vector2 shotTarget;

        public PracticeShift(int seed) => Seed = seed;

        public void TogglePlay()
        {
            if (Stage == Phase.Ready) Stage = Phase.Carry;
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
            if (Vector2.Distance(Player, Center) > StickReach)
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
            if (!CanSkate) return;
            Time += dt;
            Player = Vector2.MoveTowards(Player, target, SkateSpeed * dt);
            if (Tied && Vector2.Distance(Player, Center) > 90f) tiedUntil = Time;
            Goalie = new Vector2(105, Mathf.MoveTowards(Goalie.y,
                Mathf.Clamp(Stage == Phase.Shot ? shotTarget.y : Puck.y, 365f, 429f),
                (Stage == Phase.Shot ? 12f : 20f) * dt));

            switch (Stage)
            {
                case Phase.Carry:
                    SkateCarrier();
                    if (Time < DecisionTime) break;
                    if (SlotFeed)
                    {
                        Stage = Phase.Warning;
                        passAt = Time + WarningDuration;
                        Message = "Red looks for a pass to the slot.";
                    }
                    else ReleasePass();
                    break;
                case Phase.Warning:
                    if (Time >= passAt) ReleasePass();
                    break;
                case Phase.Pass:
                    Puck = Vector2.MoveTowards(Puck, Destination, PassSpeed * dt);
                    if (Puck != Destination) break;
                    if (SlotFeed && Tied)
                    {
                        Stage = Phase.Finished;
                        Message = "Pass denied. Your tie-up kept Red C from receiving it.";
                        break;
                    }
                    shotTarget = new Vector2(99, SlotFeed ? 373 : 397);
                    Stage = Phase.Shot;
                    Message = SlotFeed ? "Red C shoots from the slot." : "Red shoots from the point.";
                    break;
                case Phase.Shot:
                    Puck = Vector2.MoveTowards(Puck, shotTarget, 300f * dt);
                    if (Puck != shotTarget) break;
                    Stage = Phase.Finished;
                    Message = Mathf.Abs(Goalie.y - shotTarget.y) < 32f
                        ? "Save. Blue's goalie covers the puck." : "Goal. Red beats Blue's goalie.";
                    break;
            }
        }

        private void SkateCarrier()
        {
            for (int i = 1; i < RouteTimes.Length; i++)
            {
                if (Time > RouteTimes[i] && i < RouteTimes.Length - 1) continue;
                float progress = Mathf.InverseLerp(RouteTimes[i - 1], RouteTimes[i], Time);
                Carrier = Vector2.Lerp(CarrierRoute[i - 1], CarrierRoute[i], progress);
                Puck = Carrier + new Vector2(-17, 9);
                break;
            }
        }

        private void ReleasePass()
        {
            Stage = Phase.Pass;
            Message = SlotFeed ? "Red passes to Red C." : "Red cycles the puck to the point.";
        }
    }
}
