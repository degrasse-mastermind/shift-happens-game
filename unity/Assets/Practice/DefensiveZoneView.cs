using UnityEngine;

namespace ShiftHappens
{
    // Keep the simulation's goal line, face-off dots and blue line aligned with the
    // dimensioned rink. Simulation and saved notes keep their original coordinates.
    internal static class DefensiveZoneView
    {
        internal const float UnitsPerFoot = 7.2f;
        internal const float Width = 83f * UnitsPerFoot;
        internal const float Height = 90f * UnitsPerFoot;

        internal static Rect TextureRegion(bool mirrored) =>
            mirrored ? new Rect(1, 0, -1, 1) : new Rect(0, 0, 1, 1);

        internal static Vector2 Project(Vector2 ice, bool mirrored)
        {
            float x = mirrored ? 1262f - ice.x : ice.x;
            float feetX = x <= 99f ? Map(x, 17, 99, 0, 11)
                : x <= 242f ? Map(x, 99, 242, 11, 31)
                : Map(x, 242, 423, 31, 75);
            float feetY = SideDistance(ice.y - 397f, 153f, 308f, 22f, 42.5f);
            float viewX = (feetX + 3f) * UnitsPerFoot;
            return new Vector2(mirrored ? Width - viewX : viewX, (feetY + 45f) * UnitsPerFoot);
        }

        internal static Vector2 Unproject(Vector2 view, bool mirrored)
        {
            float feetX = (mirrored ? Width - view.x : view.x) / UnitsPerFoot - 3f;
            float x = feetX <= 11f ? Map(feetX, 0, 11, 17, 99)
                : feetX <= 31f ? Map(feetX, 11, 31, 99, 242)
                : Map(feetX, 31, 75, 242, 423);
            float y = SideDistance(view.y / UnitsPerFoot - 45f, 22f, 42.5f, 153f, 308f) + 397f;
            return new Vector2(mirrored ? 1262f - x : x, y);
        }

        private static float SideDistance(float value, float dot, float boards, float mappedDot, float mappedBoards)
        {
            float distance = Mathf.Abs(value);
            return Mathf.Sign(value) * (distance <= dot ? Map(distance, 0, dot, 0, mappedDot)
                : Map(distance, dot, boards, mappedDot, mappedBoards));
        }

        private static float Map(float value, float start, float end, float mappedStart, float mappedEnd) =>
            Mathf.LerpUnclamped(mappedStart, mappedEnd, (value - start) / (end - start));
    }
}
