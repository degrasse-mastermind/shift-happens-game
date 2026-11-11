using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace ShiftHappens
{
    public static class ReviewStorage
    {
        public static string DefaultRoot => Path.Combine(Application.persistentDataPath, "Reviews");
        public static void Save(ReviewNote note, byte[] png, string root)
        {
            Directory.CreateDirectory(root);
            string folder = Path.Combine(root, note.id);
            Directory.CreateDirectory(folder);
            // Write the image first; the JSON is the commit marker used by Load.
            File.WriteAllBytes(Path.Combine(folder, "annotated.png"), png);
            File.WriteAllText(Path.Combine(folder, "review.json"), JsonUtility.ToJson(note, true));
        }
        public static ReviewBundle Load(string root)
        {
            var bundle = new ReviewBundle();
            if (!Directory.Exists(root)) return bundle;
            foreach (string folder in Directory.GetDirectories(root))
            {
                string file = Path.Combine(folder, "review.json");
                if (!File.Exists(file)) continue;
                try
                {
                    var note = JsonUtility.FromJson<ReviewNote>(File.ReadAllText(file));
                    if (note != null && note.id == Path.GetFileName(folder) && File.Exists(Path.Combine(folder, "annotated.png")))
                        bundle.notes.Add(note);
                }
                catch (Exception exception) { Debug.LogWarning("Review note could not be read: " + exception.Message); }
            }
            bundle.notes.Sort((a, b) => string.CompareOrdinal(a.createdUtc, b.createdUtc));
            return bundle;
        }
        public static string Export(ReviewBundle bundle, string root)
        {
            if (bundle.notes.Count == 0) throw new InvalidOperationException("Save a note before exporting.");
            string folder = Path.Combine(root, "Exports");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "Shift-Happens-Review-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".zip");
            string temporary = path + ".partial";
            using (var stream = new FileStream(temporary, FileMode.CreateNew))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var portable = JsonUtility.FromJson<ReviewBundle>(JsonUtility.ToJson(bundle));
                foreach (var item in portable.notes) item.imageFile = item.id + "/annotated.png";
                Write(archive, "review.json", JsonUtility.ToJson(portable, true));
                Write(archive, "review.md", Markdown(bundle));
                foreach (var note in bundle.notes)
                {
                    string image = Path.Combine(root, note.id, "annotated.png");
                    using (var entry = archive.CreateEntry(note.id + "/annotated.png").Open())
                    using (var source = File.OpenRead(image)) source.CopyTo(entry);
                }
            }
            File.Move(temporary, path);
            return path;
        }
        private static void Write(ZipArchive archive, string name, string value)
        { using var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.UTF8); writer.Write(value); }
        public static string Markdown(ReviewBundle bundle)
        {
            var text = new StringBuilder("# Shift Happens! Review\n\nThese annotations propose edits. Player-facing coaching cues and Hold and Contain scoring are final in both presets. Approved positioning covers Blue LW on the puck side and Blue RW on the weak side, including their lane cuts on release, Blue C's inside support during the handoff and contain, and Blue RD's revised return to the inside shoulder between the net-front threat and Blue's net. Other positioning remains draft.\n\nRead each image and its scope. Interpret the request, identify consequential ambiguity, implement authorized changes, and return: changed / tested / needs David's check.\n");
            foreach (var note in bundle.notes)
            {
                text.AppendLine($"\n## {note.id}\n\n**Scope: {note.scope}** | {note.mode}, seed {note.seed}, captured at {note.gameTime:0.00}s ({note.phase})\n\nScene: {note.scene}; build: {note.buildVersion}; possession: {note.possession}\n\n{note.orientation}\n\n{note.coordinates}");
                if (!string.IsNullOrEmpty(note.positioningReview))
                    text.AppendLine($"\n{note.positioningReview}");
                text.AppendLine($"\n![Annotated review]({note.id}/annotated.png)\n\n### David's note\n{note.note}\n\n### Proposed annotations");
                foreach (var mark in note.marks) text.AppendLine($"- {mark.kind}: {mark.entityId ?? "rink / UI"}, ({mark.from.x:0.0}, {mark.from.y:0.0}) -> ({mark.to.x:0.0}, {mark.to.y:0.0})");
                text.AppendLine("\nOriginal player positions, preset settings, and exact annotation coordinates are in review.json. Position changes are proposals; arrows are requested movement, not observed trajectories.\n");
            }
            return text.ToString();
        }
    }
}
