using UnityEngine;

namespace ShiftHappens
{
    internal static class BehindNetRotation
    {
        private const float FarPostY = 491f;
        internal static readonly Vector2 ExitPost = new(120, 435);
        internal static readonly Vector2 SlotSupport = new(229, 397);
        internal static readonly Vector2 ExitSupport = new(237, 466);
        private static readonly float[] Times = { 1.8f, 3.3f, 4.8f, 6.3f, 7.8f, 9.3f };
        private static readonly Vector2[] Route =
        {
            new(71, 246), new(57, 330), new(54, 397),
            new(57, 481), new(122, 530), new(188, 521)
        };

        internal static bool Exited(Vector2 carrier) => carrier.x >= 122 && carrier.y > 475;
        internal static bool BehindGoal(Vector2 carrier) => carrier.x < 99 && carrier.y > 315 && carrier.y < FarPostY;
        internal static bool PastFarPost(Vector2 carrier) => carrier.y >= FarPostY;

        internal static Vector2 CarrierAt(Vector2 start, float time, float wait)
        {
            const float arrival = 4.8f;
            if (time > arrival) time = Mathf.Max(arrival, time - Mathf.Max(0, wait));
            for (int i = 0; i < Route.Length; i++)
            {
                if (time > Times[i] && i + 1 < Route.Length) continue;
                float previousTime = i == 0 ? 0 : Times[i - 1];
                Vector2 previous = i == 0 ? start : Route[i - 1];
                return Vector2.Lerp(previous, Route[i], Mathf.InverseLerp(previousTime, Times[i], time));
            }
            return Route[Route.Length - 1];
        }

        internal static Vector2 PartnerTarget(Vector2 carrier, bool exited) => exited
            ? new Vector2(Mathf.Max(124, carrier.x - 12), Mathf.Max(444, carrier.y - 44))
            : ExitPost;
    }
}
