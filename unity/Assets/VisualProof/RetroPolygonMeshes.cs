using System.Collections.Generic;
using UnityEngine;

namespace ShiftHappens.VisualProof
{
    internal static class RetroPolygonMeshes
    {
        private const int Sides = 8;

        public static void Apply(IEnumerable<StylizedProPlayer> players)
        {
            var rounded = BuildRoundedMesh();
            foreach (var player in players)
            foreach (var filter in player.Root.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh.name == "Sphere")
                    filter.sharedMesh = rounded;
        }

        private static Mesh BuildRoundedMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int band = 0; band < 4; band++)
            for (int side = 0; side < Sides; side++)
            {
                Vector3 a = Point(band, side);
                Vector3 b = Point(band + 1, side);
                Vector3 c = Point(band + 1, side + 1);
                Vector3 d = Point(band, side + 1);
                if (band != 3) Face(a, b, c);
                if (band != 0) Face(a, c, d);
            }

            var mesh = new Mesh { name = "Retro eight-sided equipment" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0)
                    (b, c) = (c, b);
                int index = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
            }
        }

        private static Vector3 Point(int band, int side)
        {
            float latitude = Mathf.PI * band / 4;
            float longitude = 2 * Mathf.PI * side / Sides;
            float radius = .5f * Mathf.Sin(latitude);
            return new Vector3(radius * Mathf.Cos(longitude), .5f * Mathf.Cos(latitude),
                radius * Mathf.Sin(longitude));
        }
    }
}

