using System;
using UnityEngine;

namespace ShiftHappens
{
    // Every tactical distance, timing and accepted response here awaits David's review.
    [Serializable]
    public sealed class DraftShiftSettings
    {
        public bool Mirror;
        public Vector2 PartnerStart = new(130, 600);
        public Vector2 PartnerCoverageOffset = new(-40, 25);
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

        public DraftShiftSettings Copy() => (DraftShiftSettings)MemberwiseClone();

        // Reflection around the supplied rink's center line preserves both end markings.
        public Vector2 Map(Vector2 point) => Mirror ? new Vector2(1262 - point.x, point.y) : point;
    }
}
