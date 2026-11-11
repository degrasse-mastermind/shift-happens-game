using System.Collections.Generic;
using UnityEngine;

namespace ShiftHappens.VisualProof
{
    // Proof geometry is measured in feet, using the practice view's existing landmarks.
    internal sealed class StylizedProRink
    {
        private readonly Transform root;
        private readonly bool retroPolygons;
        private readonly Material blueYoke, redYoke;
        private readonly Material ice, board, trim, gold, red, blue, navy, redPants, white, skin, steel, puck, cyan, glass;

        public StylizedProRink(Transform root, bool retroPolygons = false)
        {
            this.root = root;
            this.retroPolygons = retroPolygons;
            if (retroPolygons)
            {
                blueYoke = Paint("Retro cyan shoulder yoke", 0x23d6e7, .2f);
                redYoke = Paint("Retro red shoulder yoke", 0xf86774, .2f);
            }
            ice = Paint("Ice", 0xffffff, .58f);
            ice.SetTexture("_BaseMap", IceTexture());
            board = Paint("Boards", 0xf0f4fa, .3f);
            trim = Paint("Rail", 0x408ae2, .55f);
            gold = Paint("Kick plate", 0xeab443, .3f);
            red = Paint("Red", 0xc62945, .42f);
            blue = Paint("Royal blue", 0x195ccc, .44f);
            navy = Paint("Navy", 0x142444, .25f);
            redPants = Paint("Red pants", 0x581528, .25f);
            white = Paint("White equipment", 0xf4f8ff, .4f);
            skin = Paint("Face", 0xc88e70, .26f);
            steel = Paint("Steel", 0x9eb4cd, .65f);
            puck = Paint("Rubber", 0x141b29, .15f);
            cyan = Paint("Active marker", 0x1c9caa, .35f);
            glass = Paint("Tinted glass", 0x93c3ee, .72f, .18f);
        }

        public static Vector3 At(float depth, float across, float height = 0) =>
            new(across - 42.5f, height, 40f - depth);

        public Vector3[] Build()
        {
            List<Vector3> edge = new() { At(80, 0), At(28, 0) };
            for (int i = 1; i <= 16; i++)
            {
                float angle = i * Mathf.PI / 32;
                edge.Add(At(28 - 28 * Mathf.Sin(angle), 28 - 28 * Mathf.Cos(angle)));
            }
            edge.Add(At(0, 57));
            for (int i = 1; i <= 16; i++)
            {
                float angle = i * Mathf.PI / 32;
                edge.Add(At(28 - 28 * Mathf.Cos(angle), 57 + 28 * Mathf.Sin(angle)));
            }
            edge.Add(At(80, 85));
            var framing = new List<Vector3>(edge);
            Surface("Ice surface", ice, edge);
            for (int i = 1; i < edge.Count; i++)
            {
                Vector3 a = edge[i - 1], b = edge[i];
                Wall("Boards", board, a, b, .26f, 3.15f, .35f);
                Wall("Kick plate", gold, a, b, .05f, .31f, .4f);
                Wall("Blue rail", trim, a, b, 3.12f, 3.35f, .48f);
                // Leave the open end and the near lengths of the boards clear of glass.
                if (i == 1 || i == edge.Count - 1)
                {
                    framing.Add(a + Vector3.up * 3.35f);
                    framing.Add(b + Vector3.up * 3.35f);
                    continue;
                }
                framing.Add(a + Vector3.up * 7f);
                framing.Add(b + Vector3.up * 7f);
                Wall("Perimeter glass", glass, a, b, 3.35f, 7f, .10f);
                if (i % 3 == 0)
                    Rod("Glass stanchion", steel, root, a + Vector3.up * 3.35f, a + Vector3.up * 7f, .08f);
            }

            Stripe("Blue line", blue, At(75, 0, .018f), At(75, 85, .018f), 1);
            float goalLineInset = 28 - Mathf.Sqrt(28 * 28 - 17 * 17);
            Stripe("Goal line", red, At(11, goalLineInset, .018f), At(11, 85 - goalLineInset, .018f), .17f);
            foreach (float across in new[] { 20.5f, 64.5f })
            {
                Vector3 dot = At(31, across, .025f);
                Ring("Face-off circle", red, root, dot, 15, .16f);
                Part("Face-off dot", PrimitiveType.Cylinder, red, root, dot, new(2, .012f, 2));
                foreach (float side in new[] { -1f, 1f })
                {
                    foreach (float depth in new[] { 29f, 33f })
                        Stripe("Hash mark", red, At(depth, across + side * 14.86f, .03f),
                            At(depth, across + side * 16.86f, .03f), .16f);
                    foreach (float longSide in new[] { -1f, 1f })
                    {
                        float inside = 31 + longSide * 2;
                        float y = across + side * .75f;
                        Stripe("Face-off mark", red, At(31 + longSide * 6, y, .03f), At(inside, y, .03f), .16f);
                        Stripe("Face-off mark", red, At(inside, y, .03f), At(inside, y + side * 3, .03f), .16f);
                    }
                }
            }
            var crease = new List<Vector3> { At(11, 38.5f, .032f) };
            for (int i = 0; i <= 24; i++)
            {
                float angle = Mathf.Lerp(-.7297f, .7297f, i / 24f);
                crease.Add(At(11 + 6 * Mathf.Cos(angle), 42.5f + 6 * Mathf.Sin(angle), .032f));
            }
            crease.Add(At(11, 46.5f, .032f));
            Surface("Crease", Paint("Crease blue", 0xa1d4ee, .48f), crease);
            for (int i = 1; i < crease.Count; i++)
                Stripe("Crease border", red, crease[i - 1] + Vector3.up * .012f, crease[i] + Vector3.up * .012f, .12f);
            Goal();
            return framing.ToArray();
        }

        public StylizedProPlayer MakePlayer(string name, bool goalie = false, bool redTeam = false, bool tracksPuck = false)
        {
            var player = new GameObject(name).transform;
            player.SetParent(root, false);
            var body = new GameObject("Body").transform;
            body.SetParent(player, false);
            var legs = new StylizedProPlayer.Leg[2];
            var arms = tracksPuck ? new StylizedProPlayer.CarryStick.Arm[2] : null;
            Material jersey = redTeam ? red : blue;
            Material pants = redTeam ? redPants : navy;
            Material yoke = retroPolygons ? (redTeam ? redYoke : blueYoke) : jersey;
            float shoulder = goalie ? 1.20f : .98f;
            Vector3 grip = goalie ? new(1.46f, 2.62f, .86f) : new(-.69f, 3.11f, .79f);
            Vector3 heel = goalie ? new(.50f, .13f, 1.84f) : new(1.15f, .13f, 2.76f);
            Ball("Hockey pants", pants, body, new(0, 2.35f, 0), new(1.52f, 1.15f, .98f));
            Ball("Jersey", jersey, body, new(0, 3.34f, .20f), new(2 * shoulder, 1.76f, 1.06f));
            if (retroPolygons)
                Ball("Shoulder yoke", yoke, body, new(0, 3.78f, .20f), new(2.04f * shoulder, .65f, 1.14f));
            Ball("Jersey hem", white, body, new(0, 2.72f, .11f), new(1.55f, .24f, 1.04f));
            Ball("Collar", white, body, new(0, 4.04f, .33f), new(.70f, .25f, .63f));
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (goalie ? .85f : .60f);
                Vector3 skate = new(x, .23f, side < 0 ? .04f : -.20f);
                Vector3 knee = new(x * .94f, 1.30f, .36f);
                var foot = new GameObject(side < 0 ? "Left skate" : "Right skate").transform;
                foot.SetParent(player, false);
                var leg = new StylizedProPlayer.Leg
                {
                    Side = side,
                    RestFoot = skate,
                    RestKnee = knee,
                    Foot = foot,
                    Thigh = Part("Thigh", PrimitiveType.Cylinder, pants, player, Vector3.zero, Vector3.one),
                    Shin = Part("Shin", PrimitiveType.Cylinder, jersey, player, Vector3.zero, Vector3.one),
                    Knee = Part("Padded knee", PrimitiveType.Sphere, pants, player, knee, new(.67f, .85f, .67f)),
                };
                legs[side < 0 ? 0 : 1] = leg;
                if (goalie)
                {
                    var pad = Part("Leg pad", PrimitiveType.Cube, white, player, knee + new Vector3(0, -.24f, .34f), retroPolygons ? new(1.02f, 1.68f, .48f) : new(.86f, 1.57f, .44f));
                    Part("Pad stripe", PrimitiveType.Cube, jersey, pad, new(side * .22f, 0, .505f), new(.18f, .91f, .05f));
                    leg.Detail = pad;
                }
                else
                    leg.Detail = Part("Sock stripe", PrimitiveType.Sphere, white, player, knee, new(.52f, .24f, .54f));
                Ball("Skate boot", navy, foot, Vector3.zero, new(.53f, .45f, 1.00f));
                Part("Skate blade", PrimitiveType.Cube, steel, foot, new(0, -.19f, .06f), new(.10f, .08f, .87f));
                Vector3 arm = new(side * shoulder * .8f, 3.70f, .26f);
                Vector3 elbow = new(side * (goalie ? 1.32f : 1.03f), 3.02f, .50f);
                Vector3 hand = goalie
                    ? (side > 0 ? grip : new Vector3(-1.52f, 2.72f, .91f))
                    : (side < 0 ? grip : Vector3.Lerp(grip, heel, .28f));
                Ball("Shoulder pad", yoke, player, arm, new(.85f, .82f, .92f));
                Rod("Jersey sleeve", jersey, player, arm, elbow, .63f);
                Rod("Sleeve stripe", retroPolygons ? yoke : white, player, Vector3.Lerp(arm, elbow, .58f), Vector3.Lerp(arm, elbow, .80f), .66f);
                Transform forearm = Rod("Forearm", jersey, player, elbow, hand, .50f);
                Transform glove = Ball(goalie ? "Goalie glove" : "Glove", goalie ? white : pants, player, hand,
                    goalie ? new(1.02f, .88f, .53f) : new(.52f, .56f, .57f));
                if (arms != null) arms[side < 0 ? 0 : 1] = new(elbow, forearm, glove);
                if (goalie && side > 0)
                    Part("Blocker", PrimitiveType.Cube, white, player, hand + Vector3.forward * .22f, new(.84f, .91f, .24f));
            }

            float head = goalie ? 4.63f : 4.78f;
            Ball("Helmet shell", goalie ? white : jersey, body, new(0, head, .27f), new(1.22f, 1.18f, 1.25f));
            Ball("Face opening", navy, body, new(0, head - .14f, .69f), new(.94f, .68f, .55f));
            Ball("Face", skin, body, new(0, head - .13f, .78f), new(.72f, .52f, .38f));
            for (int bar = -2; bar <= 2; bar++)
                Rod("Cage wire", retroPolygons ? white : steel, body, new(bar * .16f, head - .50f, 1.02f), new(bar * .16f, head + .14f, 1.02f), .046f);
            for (int bar = -2; bar <= 1; bar++)
                Rod("Cage wire", retroPolygons ? white : steel, body, new(-.43f, head + bar * .17f, 1.02f), new(.43f, head + bar * .17f, 1.02f), .046f);
            var stick = Stick(player, grip, heel, goalie, arms);
            return new StylizedProPlayer(player, body, legs, goalie, stick) { SkateWithGlide = retroPolygons };
        }

        private StylizedProPlayer.CarryStick Stick(Transform player, Vector3 grip, Vector3 heel, bool goalie,
            StylizedProPlayer.CarryStick.Arm[] arms)
        {
            Vector3 knob = grip + (grip - heel).normalized * .22f;
            Transform shaft = Rod("Stick shaft", navy, player, knob, heel, goalie ? .14f : .12f);
            if (goalie)
                StickSection("Goalie paddle", steel, player, heel, Vector3.Lerp(heel, grip, .64f), .30f, .13f);

            // Both blade sections are level at the ice. The goalie blade reaches across the pads.
            Vector3 bend = heel + (goalie ? new Vector3(-.88f, 0, .12f) : new Vector3(.51f, 0, .61f));
            Vector3 toe = bend + (goalie ? new Vector3(-.31f, 0, .13f) : new Vector3(.08f, 0, .28f));
            Transform blade = StickSection("Stick blade", puck, player, heel, bend, .22f, .13f);
            Transform tip = StickSection("Stick toe", puck, player, bend, toe, .20f, .13f);
            return arms == null ? null : new StylizedProPlayer.CarryStick(shaft, blade, tip, arms, grip);
        }

        private static Transform StickSection(string name, Material material, Transform player,
            Vector3 from, Vector3 to, float height, float thickness)
        {
            var section = Part(name, PrimitiveType.Cube, material, player, (from + to) / 2,
                new(thickness, height, Vector3.Distance(from, to) + .06f));
            section.localRotation = Quaternion.LookRotation(to - from);
            return section;
        }

        public List<Mesh> ReplaceDefensemanVisual(Transform player, GameObject modelAsset)
        {
            var previewMeshes = new List<Mesh>();
            var model = Object.Instantiate(modelAsset, player, false);
            model.name = "Meshy blue defenseman";
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                Object.Destroy(model);
                return previewMeshes;
            }

            // The replay owns the player's root. Fit only the imported visual to the proof's feet scale.
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y <= Mathf.Epsilon)
            {
                Object.Destroy(model);
                return previewMeshes;
            }

            foreach (var renderer in renderers)
                ColorDefenseman(renderer, bounds, previewMeshes);

            const float skaterHeight = 5.35f;
            Vector3 iceAnchor = new(bounds.center.x, bounds.min.y, bounds.center.z);
            float fit = skaterHeight / (bounds.size.y / player.lossyScale.y);
            model.transform.localScale *= fit;
            model.transform.localPosition -= player.InverseTransformPoint(iceAnchor) * fit;

            foreach (var renderer in player.GetComponentsInChildren<Renderer>())
                renderer.enabled = renderer.transform.IsChildOf(model.transform);
            return previewMeshes;
        }

        private void ColorDefenseman(MeshRenderer renderer, Bounds bounds, List<Mesh> previewMeshes)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            var source = filter.sharedMesh;
            var vertices = source.vertices;
            // Use the imported pose's proportions and find its handedness from the blade toe.
            var points = new Vector3[vertices.Length];
            Vector3 toe = Vector3.zero;
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = (filter.transform.TransformPoint(vertices[i]) - bounds.center) * (1.898f / bounds.size.y);
                if (points[i].y < -.70f && Mathf.Abs(points[i].x) > Mathf.Abs(toe.x))
                    toe = points[i];
            }
            float across = Mathf.Sign(toe.x);
            float forward = Mathf.Sign(toe.z);
            var equipment = new List<int>();
            var uniform = new List<int>();
            var stick = new List<int>();
            var triangles = source.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 center = (points[triangles[i]] + points[triangles[i + 1]] + points[triangles[i + 2]]) / 3;
                float x = across * center.x, y = center.y, depth = forward * center.z;
                float shaftX = -.50f + (.16f - y) * .91f;
                float shaftDepth = .08f + (.16f - y) * .32f;
                bool onShaft = y < .18f && Mathf.Abs(x - shaftX) < .055f &&
                    Mathf.Abs(depth - shaftDepth) < .065f;
                bool onBlade = y < -.76f && x > .37f && depth > .32f;
                bool onGlove = (x < -.42f && y < .22f && y > .06f) ||
                    (x > -.20f && x < .04f && y < -.21f && y > -.36f && depth > .12f);
                var region = onBlade || onShaft ? stick : y > -.39f && y < .55f && !onGlove ? uniform : equipment;
                region.Add(triangles[i]);
                region.Add(triangles[i + 1]);
                region.Add(triangles[i + 2]);
            }

            var colored = Object.Instantiate(source);
            colored.name = "Meshy defenseman preview materials";
            colored.subMeshCount = 3;
            colored.SetTriangles(equipment, 0, false);
            colored.SetTriangles(uniform, 1, false);
            colored.SetTriangles(stick, 2, false);
            filter.sharedMesh = colored;
            renderer.sharedMaterials = new[] { white, navy, puck };
            previewMeshes.Add(colored);
        }

        public Transform MakePuck() =>
            Part("Puck", PrimitiveType.Cylinder, puck, root, Vector3.zero, new(1.25f, .085f, 1.25f));

        public Transform PuckOutline() => Ring("Puck outline", white, root, Vector3.zero, .82f, .32f, 32);

        public Transform LdRing() => Ring("Active LD ring", cyan, root, Vector3.zero, 3.30f, .37f, 48);

        private void Goal()
        {
            Vector3 left = At(11, 39.5f), right = At(11, 45.5f);
            Vector3 backLeft = At(7.5f, 39.75f), backRight = At(7.5f, 45.25f);
            Vector3 top = Vector3.up * 4, rearTop = Vector3.up * 3.1f;
            Rod("Left post", red, root, left, left + top, .25f);
            Rod("Right post", red, root, right, right + top, .25f);
            Rod("Crossbar", red, root, left + top, right + top, .25f);
            Rod("Goal frame", red, root, backLeft, backRight, .20f);
            foreach (int i in new[] { 0, 1 })
            {
                Vector3 front = i == 0 ? left : right, back = i == 0 ? backLeft : backRight;
                Rod("Goal frame", red, root, front + top, back + rearTop, .17f);
                Rod("Goal frame", red, root, front, back, .17f);
                Rod("Goal frame", red, root, back, back + rearTop, .17f);
                for (int j = 1; j < 8; j++)
                {
                    float t = j / 8f;
                    Rod("Side mesh", white, root, Vector3.Lerp(front, back, t),
                        Vector3.Lerp(front + top, back + rearTop, t), .05f);
                    Rod("Side mesh", white, root, front + top * t, back + rearTop * t, .05f);
                }
            }
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f;
                Vector3 back = Vector3.Lerp(backLeft, backRight, t);
                Rod("Back mesh", white, root, back, back + rearTop, .055f);
                Rod("Roof mesh", white, root, back + rearTop, Vector3.Lerp(left, right, t) + top, .055f);
            }
            for (int i = 1; i < 8; i++)
            {
                float t = i / 8f;
                Rod("Back mesh", white, root, backLeft + rearTop * t, backRight + rearTop * t, .055f);
                Rod("Roof mesh", white, root, Vector3.Lerp(left + top, backLeft + rearTop, t),
                    Vector3.Lerp(right + top, backRight + rearTop, t), .055f);
            }
        }

        private void Wall(string name, Material material, Vector3 a, Vector3 b, float bottom, float top, float thickness)
        {
            Vector3 center = (a + b) / 2 + Vector3.up * ((top + bottom) / 2);
            var wall = Part(name, PrimitiveType.Cube, material, root, center, new(thickness, top - bottom, Vector3.Distance(a, b) + .06f));
            wall.localRotation = Quaternion.LookRotation(b - a);
        }

        private void Stripe(string name, Material material, Vector3 a, Vector3 b, float width) =>
            Wall(name, material, a, b, -.015f, .015f, width);

        private void Surface(string name, Material material, List<Vector3> points)
        {
            var indices = new List<int>();
            for (int i = 1; i < points.Count - 1; i++) AddTriangle(indices, points, 0, i, i + 1);
            var surface = MeshObject(name, material, root, Vector3.zero, points, indices);
            if (material != ice) return;
            var uv = new Vector2[points.Count];
            for (int i = 0; i < points.Count; i++)
                uv[i] = new Vector2((points[i].x + 42.5f) / 85, (points[i].z + 40) / 80);
            surface.GetComponent<MeshFilter>().sharedMesh.uv = uv;
        }

        private static Texture2D IceTexture()
        {
            const int size = 256;
            var pixels = new Color[size * size];
            var random = new System.Random(17);
            Color center = new(.79f, .87f, .96f);
            Color edge = new(.60f, .75f, .88f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2((x - 128) / 128f, (y - 128) / 128f).sqrMagnitude;
                float grain = ((float)random.NextDouble() - .5f) * .04f;
                pixels[y * size + x] = Color.Lerp(center, edge, radius * .54f) + new Color(grain, grain, grain, 0);
            }
            for (int i = 0; i < 420; i++)
            {
                Vector2 start = new(random.Next(size), random.Next(size));
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                int length = random.Next(3, 17);
                for (int j = 0; j < length; j++)
                {
                    Vector2 point = start + direction * j;
                    int x = Mathf.RoundToInt(point.x), y = Mathf.RoundToInt(point.y);
                    if (x < 0 || y < 0 || x >= size || y >= size) continue;
                    int index = y * size + x;
                    pixels[index] = Color.Lerp(pixels[index], Color.white, Mathf.Sin(j * Mathf.PI / length) * .12f);
                }
            }
            var texture = new Texture2D(size, size) { name = "Stylized Pro ice", wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Transform Ring(string name, Material material, Transform parent, Vector3 center, float radius,
            float width, int segments = 96)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * 2 * Mathf.PI / segments;
                Vector3 direction = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices.Add(direction * (radius - width / 2));
                vertices.Add(direction * (radius + width / 2));
                if (i == 0) continue;
                int k = i * 2;
                AddTriangle(indices, vertices, k - 2, k - 1, k);
                AddTriangle(indices, vertices, k, k - 1, k + 1);
            }
            return MeshObject(name, material, parent, center, vertices, indices);
        }

        private static void AddTriangle(List<int> indices, List<Vector3> vertices, int a, int b, int c)
        {
            bool up = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).y > 0;
            indices.Add(a);
            indices.Add(up ? b : c);
            indices.Add(up ? c : b);
        }

        private static Transform MeshObject(string name, Material material, Transform parent, Vector3 position,
            List<Vector3> vertices, List<int> indices)
        {
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            return obj.transform;
        }

        private static Transform Ball(string name, Material material, Transform parent, Vector3 position, Vector3 size) =>
            Part(name, PrimitiveType.Sphere, material, parent, position, size);

        private static Transform Rod(string name, Material material, Transform parent, Vector3 a, Vector3 b, float diameter)
        {
            var rod = Part(name, PrimitiveType.Cylinder, material, parent, (a + b) / 2,
                new(diameter, Vector3.Distance(a, b) / 2, diameter));
            rod.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            return rod;
        }

        private static Transform Part(string name, PrimitiveType shape, Material material, Transform parent,
            Vector3 position, Vector3 size)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }

        private static Material Paint(string name, int rgb, float smoothness, float alpha = 1)
        {
            var color = new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            material.SetFloat("_Smoothness", smoothness);
            if (alpha >= 1) return material;
            material.SetFloat("_Surface", 1);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }
    }
}
