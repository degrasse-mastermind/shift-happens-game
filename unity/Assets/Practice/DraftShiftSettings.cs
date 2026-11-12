using System;
using UnityEngine;

namespace ShiftHappens
{
    // Presets combine approved Hold and Contain scoring with positioning under review.
    [Serializable]
    public sealed class DraftShiftSettings
    {
        public enum RedForward { C, LW, RW }

        public bool CornerCarry;
        public bool BehindNetCarry;
        [Min(0)] public float BehindNetWait;
        public bool HoldFromPassingLane;
        public bool Mirror;
        public Vector2 PartnerStart = new(130, 600);
        // Approved offset from David's marked RD position relative to the net-front threat.
        public Vector2 PartnerCoverageOffset = new(-47.21f, -13.25f);
        [Min(0)] public float PartnerReturnSpeed = 48f;
        [Min(0)] public float PartnerReturnDelay;
        [Min(0)] public float CoverageSettleTime = 0.6f;
        [Min(1)] public float CoverageReach = 65f;
        [Min(1)] public float GoalSideMargin = 15f;
        [Min(1)] public float LaneReach = 50f;
        [Min(1)] public float ContainDistance = 110f;
        [Min(0)] public float ContainHoldTime = 1f;
        [Range(0, 1)] public float RequiredHoldCoverage = 0.7f;
        [Min(1)] public float ShiftDuration = 30f;

        [Header("Starting positions and Red roles")]
        public Vector2 PlayerStart = PracticeShift.StartPosition;
        public Vector2 BlueCenter = new(225, 465);
        public Vector2 BlueLeftWing = new(330, 245);
        public Vector2 BlueRightWing = new(330, 550);
        public Vector2 NetFrontStart = PracticeShift.Center;
        public Vector2 CarrierStart = new(230, 315);
        public Vector2 PuckStart = new(213, 324);
        public Vector2 RedSupport = new(260, 500);
        public Vector2 RedLeftDefense = new(390, 635);
        public Vector2 RedRightDefense = PracticeShift.Point;
        public RedForward CarrierRole = RedForward.RW;
        public RedForward NetFrontRole = RedForward.C;
        public RedForward SupportRole = RedForward.LW;

        // Final positions from David's Starting setup review, in unmirrored rink coordinates.
        public static readonly Vector2 MarkedPartnerStart = new(55.21369552612305f, 532.6640625f);
        public static DraftShiftSettings MarkedPreset() => new()
        {
            CornerCarry = true,
            HoldFromPassingLane = true,
            PartnerStart = MarkedPartnerStart,
            PartnerReturnSpeed = 36f,
            ContainDistance = 80f,
            PlayerStart = new(143.7271270751953f, 306.30206298828125f),
            BlueCenter = new(245.2998809814453f, 362.89251708984375f),
            BlueLeftWing = new(335.2642822265625f, 252.61361694335938f),
            BlueRightWing = new(310.5966796875f, 435.4444580078125f),
            NetFrontStart = new(194.51348876953125f, 435.4444580078125f),
            CarrierStart = new(63.91996765136719f, 188.76792907714844f),
            PuckStart = new(95.8427734375f, 155.39402770996094f),
            RedSupport = new(209.02391052246094f, 245.35842895507812f),
            RedLeftDefense = new(391.8548583984375f, 429.6403503417969f),
            RedRightDefense = new(402.0121765136719f, 168.45335388183594f),
            CarrierRole = RedForward.C,
            NetFrontRole = RedForward.LW,
            SupportRole = RedForward.RW,
        };
        public static DraftShiftSettings BehindNetPreset(float wait = 0)
        {
            var settings = new DraftShiftSettings
            {
                BehindNetCarry = true,
                BehindNetWait = wait,
                PlayerStart = new(151, 373),
                CoverageReach = 82,
                PartnerStart = new(132, 516),
                BlueCenter = new(243, 329),
                NetFrontStart = new(208, 397),
                CarrierStart = new(171, 232),
                PuckStart = new(154, 241),
            };
            if (wait > 0f) return settings;

            // David's November 12 Behind net review marks, in unmirrored rink coordinates.
            settings.PlayerStart = new(161.62095642089844f, 355.35455322265625f);
            settings.BlueRightWing = new(330.43463134765625f, 426.8605041503906f);
            settings.NetFrontStart = new(293.7038879394531f, 387.34405517578125f);
            settings.RedLeftDefense = new(382.74810791015625f, 430.6239929199219f);
            settings.RedSupport = new(255.8600616455078f, 488.95782470703125f);
            return settings;
        }

        public string NetFrontLabel => NetFrontRole.ToString();
        public string CarrierLabel => CarrierRole.ToString();
        public string SupportLabel => SupportRole.ToString();

        public DraftShiftSettings Copy() => (DraftShiftSettings)MemberwiseClone();

        // Reflection around the supplied rink's center line preserves both end markings.
        public Vector2 Map(Vector2 point) => Mirror ? new Vector2(1262 - point.x, point.y) : point;
    }

    [Serializable]
    public sealed class DraftPracticePreset
    {
        public string Name = "Marked upper corner";
        public DraftShiftSettings Settings = DraftShiftSettings.MarkedPreset();
    }
}
