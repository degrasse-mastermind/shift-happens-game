using UnityEngine;

namespace ShiftHappens.VisualProof
{
    // The proof poses visual parts from replay displacement. The simulation owns the root position.
    internal sealed class StylizedProPlayer
    {
        internal sealed class Leg
        {
            public Transform Foot, Thigh, Shin, Knee, Detail;
            public Vector3 RestFoot, RestKnee;
            public int Side;
        }

        internal sealed class CarryStick
        {
            internal readonly struct Arm
            {
                public readonly Vector3 Elbow;
                public readonly Transform Forearm, Glove;

                public Arm(Vector3 elbow, Transform forearm, Transform glove)
                {
                    Elbow = elbow;
                    Forearm = forearm;
                    Glove = glove;
                }

                public void Grip(Vector3 position)
                {
                    SetRod(Forearm, Elbow, position, .50f);
                    Glove.localPosition = position;
                }
            }

            private readonly Transform shaft, blade, toe;
            private readonly Arm[] arms;
            private readonly Vector3 topGrip;

            public CarryStick(Transform shaft, Transform blade, Transform toe, Arm[] arms, Vector3 topGrip)
            {
                this.shaft = shaft;
                this.blade = blade;
                this.toe = toe;
                this.arms = arms;
                this.topGrip = topGrip;
            }

            public void Pose(Vector3 puck, float radius)
            {
                Vector3 towardPuck = new Vector3(puck.x, 0, puck.z).normalized;
                if (towardPuck == Vector3.zero) towardPuck = Vector3.forward;
                Vector3 across = Vector3.Cross(Vector3.up, towardPuck);
                puck.y = .13f;
                // Place the face of the blade against the puck's edge, with the blade level.
                Vector3 contact = puck - towardPuck * (radius + .065f);
                Vector3 heel = contact - across * .43f;
                Vector3 bend = contact + across * .33f;
                Vector3 tip = bend + across * .31f + towardPuck * .09f;
                float reach = Mathf.Max(0, Vector3.Dot(heel - topGrip, towardPuck) - 2);
                Vector3 upper = topGrip + towardPuck * (reach * .55f);
                Vector3 lower = Vector3.Lerp(upper, heel, .28f);
                arms[0].Grip(upper);
                arms[1].Grip(lower);
                SetRod(shaft, upper + (upper - heel).normalized * .22f, heel, .12f);
                SetBladeSection(blade, heel, bend);
                SetBladeSection(toe, bend, tip);
            }

            private static void SetBladeSection(Transform section, Vector3 from, Vector3 to)
            {
                section.localPosition = (from + to) / 2;
                section.localRotation = Quaternion.LookRotation(to - from);
                Vector3 size = section.localScale;
                size.z = Vector3.Distance(from, to) + .06f;
                section.localScale = size;
            }
        }

        public Transform Root { get; }
        public bool SkateWithGlide { get; set; }
        private readonly Transform body;
        private readonly Leg[] legs;
        private readonly bool goalie;
        private readonly CarryStick carryStick;
        private Vector3 direction;
        private float distance;
        private float effort;

        public StylizedProPlayer(Transform root, Transform body, Leg[] legs, bool goalie, CarryStick carryStick)
        {
            Root = root;
            this.body = body;
            this.legs = legs;
            this.goalie = goalie;
            this.carryStick = carryStick;
        }

        public void HoldPuckAtBlade(Vector3 worldPuck, float worldRadius) =>
            carryStick?.Pose(Root.InverseTransformPoint(worldPuck), worldRadius / Root.lossyScale.x);

        public void Reset()
        {
            direction = Vector3.forward;
            distance = 0;
            effort = 0;
        }

        public void Pose(Vector3 displacement, float dt, float time)
        {
            float travel = displacement.magnitude;
            if (travel > .0001f)
                direction = Root.InverseTransformDirection(displacement / travel);
            distance += travel;
            float speed = dt > 0 ? travel / dt : 0;
            effort = Mathf.MoveTowards(effort, Mathf.Clamp01(speed / (goalie ? 2.3f : 6.2f)), dt * 7);
            float stride = SkateWithGlide ? (goalie ? 4.8f : 10.5f) : (goalie ? 3.9f : 6.5f);
            float cycle = distance * 2 * Mathf.PI / stride;
            float lean = direction.x * effort;
            float readyShift = goalie ? Mathf.Sin(time * 3.4f) * .055f : 0;
            float flex = Mathf.Cos(cycle * 2) * (SkateWithGlide ? .012f : .035f) * effort;
            body.localPosition = new(lean * .15f + readyShift, (goalie ? -.19f : -.07f) - effort * .10f + flex, 0);
            body.localRotation = Quaternion.Euler(0, 0, -lean * 3.5f);
            foreach (var leg in legs)
                PoseLeg(leg, cycle);
        }

        private void PoseLeg(Leg leg, float cycle)
        {
            int side = leg.Side;
            float phase = cycle + (side < 0 ? 0 : Mathf.PI);
            float extension = Mathf.Cos(phase);
            float recover = Mathf.Max(0, Mathf.Sin(phase));
            Vector3 foot = leg.RestFoot;
            if (SkateWithGlide)
            {
                // Extend one blade outward and back while the other stays low for the glide.
                float push = Mathf.SmoothStep(0, 1, Mathf.Max(0, extension)) * effort;
                foot.x += side * ((goalie ? .14f : .09f) + push * (goalie ? .52f : .87f));
                if (!goalie) foot -= direction * (.62f * push);
                foot.y += recover * effort * (goalie ? .015f : .045f);
            }
            else if (goalie)
            {
                bool leading = side * direction.x > 0;
                float push = .48f + .52f * extension;
                foot.x += side * (.14f + effort * push * (leading ? .57f : .41f));
                foot.y += leading ? 0 : effort * recover * .14f;
            }
            else
            {
                foot -= direction * (extension * .83f * effort);
                foot.x += side * Mathf.Max(0, extension) * .48f * effort;
                foot.y += recover * .30f * effort;
            }

            Vector3 ankle = foot + Vector3.up * .25f;
            Vector3 knee = leg.RestKnee + (foot - leg.RestFoot) * .46f;
            knee.y += body.localPosition.y * .45f;
            Vector3 hip = new(side * .43f, 2.22f, .04f);
            hip += body.localPosition;
            leg.Foot.localPosition = foot;
            float edge = effort * Mathf.Max(0, extension);
            leg.Foot.localRotation = Quaternion.Euler(0, side * (goalie ? 12 : 6 + 17 * edge),
                SkateWithGlide ? -side * edge * 9 : 0);
            leg.Knee.localPosition = knee + Vector3.up * .14f;
            SetRod(leg.Thigh, hip, knee, goalie ? .61f : .55f);
            SetRod(leg.Shin, ankle, knee, .49f);
            if (goalie)
            {
                leg.Detail.localPosition = knee + new Vector3(0, -.24f, .34f);
                leg.Detail.localRotation = Quaternion.Euler(-10, side * 14, side * -8 - (foot.x - leg.RestFoot.x) * 14);
            }
            else
                leg.Detail.localPosition = Vector3.Lerp(ankle, knee, .42f);
        }

        internal static void SetRod(Transform rod, Vector3 from, Vector3 to, float diameter)
        {
            Vector3 axis = to - from;
            rod.localPosition = (from + to) / 2;
            rod.localScale = new(diameter, axis.magnitude / 2, diameter);
            rod.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
        }
    }
}
