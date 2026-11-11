using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShiftHappens
{
    // Review only owns copies and annotations. It never edits simulation positions or scene assets.
    public sealed class ReviewMode : MonoBehaviour
    {
        public bool Active { get; private set; }
        private RinkPractice host;
        private ReviewNote current;
        private ReviewBundle saved;
        private Texture2D ice, youthHockey;
        private readonly Dictionary<string, Texture2D> thumbnails = new();
        private int tool;
        private int scope;
        private string selected = "blue.ld";
        private string message = "";
        private bool dragging;
        private bool saving;
        private Vector2 dragFrom;
        private Vector2 dragTo;
        private Vector2 queueScroll;
        private const float BoardHeight = 464f;
        private const float BoardScale = BoardHeight / DefensiveZoneView.Height;
        private const float BoardWidth = DefensiveZoneView.Width * BoardScale;
        private readonly Rect board = new(16 + (900 - BoardWidth) / 2, 145, BoardWidth, BoardHeight);
        private bool mirrored;
        private readonly string[] tools = { "Move", "Arrow", "Circle" };

        public void Initialize(RinkPractice owner)
        {
            host = owner;
            ice = Resources.Load<Texture2D>("PracticeZoneIce");
            youthHockey = Resources.Load<Texture2D>("PracticeYouthHockey");
            saved = ReviewStorage.Load(ReviewStorage.DefaultRoot);
        }
        public void Begin()
        {
            if (Active) return;
            Active = true;
            host.SetReviewGate(true);
            scope = 0;
            current = host.CaptureReview(false);
            mirrored = host.IsMirrored;
            message = "Choose a tool, then mark the rink. Notes are optional. Nothing changes in the game.";
            dragging = false;
        }
        private void Resume()
        {
            Active = false;
            dragging = false;
            host.SetReviewGate(false);
        }
        private Vector2 ToBoard(Vector2 p) => DefensiveZoneView.Project(p, mirrored) * BoardScale;

        private Vector2 ToIce(Vector2 p)
        {
            Vector2 local = p - board.position;
            local.x = Mathf.Clamp(local.x, 0, board.width);
            local.y = Mathf.Clamp(local.y, 0, board.height);
            return DefensiveZoneView.Unproject(local / BoardScale, mirrored);
        }
        private void OnGUI()
        {
            if (!Active || current == null) return;
            GUI.depth = -100;
            Matrix4x4 oldMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 860f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 860 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            var title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            var label = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            var noteStyle = new GUIStyle(GUI.skin.textArea) { fontSize = 19, wordWrap = true };
            Fill(new Rect(0, 0, 1280, 860), new Color(.04f, .09f, .15f));
            GUI.Label(new Rect(16, 10, 1200, 40), "REVIEW MODE  /  Freeze - Mark - Say - Save", title);
            GUI.Label(new Rect(655, 18, 607, 26), "LW puck-side, RW weak-side, C support, RD return final · Other positions draft", label);
            GUI.Label(new Rect(16, 53, 1235, 28), $"{current.scene}  |  {current.mode}  |  Seed {current.seed}  |  Captured at {current.gameTime:0.00}s  |  GAME FROZEN", label);
            GUI.enabled = !saving;
            int nextScope = GUI.Toolbar(new Rect(16, 92, 400, 40), scope, new[] { "This moment", "Starting setup" }, button);
            if (nextScope != scope)
            {
                if (current.marks.Count > 0) message = "Save or discard this note before changing scope. Your marks are preserved.";
                else
                {
                    string text = current.note;
                    scope = nextScope;
                    current = host.CaptureReview(scope == 1);
                    current.note = text;
                    dragging = false;
                }
            }
            int nextTool = GUI.Toolbar(new Rect(435, 92, 330, 40), tool, tools, button);
            if (nextTool != tool) { tool = nextTool; dragging = false; }
            if (GUI.Button(new Rect(780, 92, 136, 40), "Undo mark", button)) { current.Undo(); dragging = false; }
            DrawBoard();
            GUI.Label(new Rect(16, 616, 900, 24), "Faint = original. Bright = proposal. Drag a player, draw an arrow, or circle an area.", label);
            GUI.Label(new Rect(16, 642, 900, 22), current.positioningReview, label);
            GUI.Label(new Rect(16, 665, 900, 28), "Optional note - describe timing, behavior, controls or anything else", label);
            GUI.SetNextControlName("ReviewNote");
            current.note = GUI.TextArea(new Rect(16, 699, 900, 92), current.note, 4000, noteStyle);
            GUI.Label(new Rect(16, 797, 900, 47), "Windows dictation: click the note, then Win+H if available. No microphone recording or external transcription is built into this release.", label);
            GUI.Label(new Rect(940, 92, 320, 35), "Review queue  /  " + saved.notes.Count + " saved", title);
            queueScroll = GUI.BeginScrollView(new Rect(936, 140, 328, 327), queueScroll, new Rect(0, 0, 302, Mathf.Max(320, saved.notes.Count * 94)));
            for (int i = 0; i < saved.notes.Count; i++)
            {
                var item = saved.notes[i];
                Texture2D thumbnail = Thumbnail(item);
                if (thumbnail != null) GUI.DrawTexture(new Rect(0, i * 94, 106, 72), thumbnail, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(112, i * 94, 187, 85), $"{i + 1}. {item.scope}\n{item.gameTime:0.0}s / {item.marks.Count} marks\n{(item.note.Length > 32 ? item.note.Substring(0, 32) + "..." : item.note)}", label);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(940, 478, 315, 86), message, label);
            GUI.enabled = !saving && (current.marks.Count > 0 || !string.IsNullOrWhiteSpace(current.note));
            if (GUI.Button(new Rect(940, 572, 320, 48), saving ? "Saving..." : "Save note & resume", button)) StartCoroutine(Save());
            GUI.enabled = !saving;
            if (GUI.Button(new Rect(940, 630, 320, 44), "Discard draft & resume", button)) Resume();
            GUI.enabled = !saving && saved.notes.Count > 0;
            if (GUI.Button(new Rect(940, 684, 320, 44), "Export saved reviews", button))
            {
                try
                {
                    string path = ReviewStorage.Export(saved, ReviewStorage.DefaultRoot);
                    GUIUtility.systemCopyBuffer = path;
                    message = "Exported. ZIP path copied. Open folder and attach the ZIP to your Work chat.";
                }
                catch (Exception exception) { message = "Export failed; saved notes remain safe. " + exception.Message; }
            }
            GUI.enabled = !saving;
            if (GUI.Button(new Rect(940, 738, 320, 44), "Open reviews folder", button))
            {
                try { Directory.CreateDirectory(ReviewStorage.DefaultRoot); Application.OpenURL(new Uri(ReviewStorage.DefaultRoot).AbsoluteUri); }
                catch (Exception exception) { message = "Could not open folder: " + exception.Message; }
            }
            GUI.Label(new Rect(940, 793, 320, 52), "Saved locally. Export includes saved notes only. Unsubmitted draft is separate.", label);
            GUI.enabled = true;
            GUI.matrix = oldMatrix;
        }
        private void DrawBoard()
        {
            Vector2 pointer = Event.current.mousePosition;
            GUI.BeginGroup(board);
            if (ice != null) GUI.DrawTextureWithTexCoords(new Rect(Vector2.zero, board.size), ice,
                DefensiveZoneView.TextureRegion(mirrored));
            foreach (var entity in current.entities)
            {
                Vector2 original = ToBoard(entity.position);
                Vector2 proposal = ToBoard(current.Proposed(entity.id));
                if (original != proposal) Token(entity, original, .35f, false);
                Token(entity, proposal, 1f, selected == entity.id);
            }
            foreach (var mark in current.marks)
            {
                if (mark.kind == "Arrow") Arrow(ToBoard(mark.from), ToBoard(mark.to), new Color(.9f, .45f, 0));
                if (mark.kind == "Circle") Circle(ToBoard(mark.from), ToBoard(mark.to), new Color(.9f, .45f, 0));
            }
            if (dragging && tool == 1) Arrow(ToBoard(dragFrom), ToBoard(dragTo), Color.yellow);
            if (dragging && tool == 2) Circle(ToBoard(dragFrom), ToBoard(dragTo), Color.yellow);
            if (dragging && tool == 0)
                Token(current.entities.Find(e => e.id == selected), ToBoard(dragTo), .85f, true);
            GUI.EndGroup();
            if (saving) return;
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && board.Contains(pointer))
            {
                GUI.FocusControl(null);
                Vector2 hit = ToIce(pointer);
                ReviewEntity closest = null;
                float distance = 40;
                foreach (var entity in current.entities)
                {
                    float value = Vector2.Distance(hit, current.Proposed(entity.id));
                    if (value < distance) { closest = entity; distance = value; }
                }
                selected = closest?.id;
                if (tool == 0 && closest == null) { message = "Drag a player or the puck to propose a position."; e.Use(); return; }
                dragFrom = tool == 0 ? current.Proposed(selected) : hit;
                dragTo = hit;
                dragging = true;
                e.Use();
            }
            if (e.type == EventType.MouseDrag && dragging) { dragTo = ToIce(pointer); e.Use(); }
            if (e.type == EventType.MouseUp && dragging)
            {
                dragTo = ToIce(pointer);
                if (Vector2.Distance(dragFrom, dragTo) > 2)
                    current.marks.Add(new ReviewMark { kind = tools[tool], entityId = selected, from = dragFrom, to = dragTo });
                dragging = false;
                e.Use();
            }
        }
        private IEnumerator Save()
        {
            saving = true;
            dragging = false;
            // Capture only the game view with its review overlay, not the desktop.
            yield return new WaitForEndOfFrame();
            Texture2D image = null;
            try
            {
                image = ScreenCapture.CaptureScreenshotAsTexture();
                ReviewStorage.Save(current.Copy(), image.EncodeToPNG(), ReviewStorage.DefaultRoot);
                saved = ReviewStorage.Load(ReviewStorage.DefaultRoot);
                message = "Saved locally. Export your saved notes after testing.";
                Resume();
            }
            catch (Exception exception) { message = "Save failed; your draft is still open. " + exception.Message; }
            finally { if (image != null) Destroy(image); saving = false; }
        }
        private Texture2D Thumbnail(ReviewNote note)
        {
            if (thumbnails.TryGetValue(note.id, out var texture)) return texture;
            try
            {
                texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(Path.Combine(ReviewStorage.DefaultRoot, note.id, "annotated.png")));
                thumbnails.Add(note.id, texture);
                return texture;
            }
            catch { if (texture != null) Destroy(texture); return null; }
        }
        private static string OnIceLabel(ReviewEntity entity) =>
            entity?.team == "blue" ? (entity.id == "blue.ld" ? "LD" : entity.label) : "";

        private void Token(ReviewEntity entity, Vector2 at, float alpha, bool selected)
        {
            if (entity == null) return;
            bool isPuck = entity.id == "puck";
            float size = (isPuck ? RinkPractice.PuckSize : RinkPractice.PlayerSize) * BoardScale;
            var image = new Rect(at - Vector2.one * size / 2f, Vector2.one * size);
            if (selected)
                Circle(at, at + Vector2.right * (size / 2f + 2), new Color(1, .75f, .15f));
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            if (isPuck) GUI.DrawTexture(image, host.PuckTexture);
            else
                GUI.DrawTextureWithTexCoords(image, youthHockey,
                    RinkPractice.PlayerTextureRegion(entity.team == "red", entity.label == "G"));
            GUI.color = oldColor;
            string name = OnIceLabel(entity);
            if (name.Length == 0) return;
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;
            var plate = new Rect(at.x - 13, at.y + size / 2f, 26, 17);
            Fill(plate, new Color(.08f, .21f, .32f, .94f * alpha));
            GUI.Label(plate, name, style);
        }
        private static void Fill(Rect rect, Color color)
        { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        private static void Line(Vector2 a, Vector2 b, Color color, float width = 3)
        {
            Matrix4x4 matrix = GUI.matrix;
            GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(a.x, a.y, 0), Quaternion.Euler(0, 0, Mathf.Atan2(b.y-a.y, b.x-a.x)*Mathf.Rad2Deg), Vector3.one);
            Fill(new Rect(0, -width/2, Vector2.Distance(a,b), width), color);
            GUI.matrix = matrix;
        }
        private static void Arrow(Vector2 a, Vector2 b, Color color)
        {
            Line(a,b,color);
            Vector2 direction = (b-a).normalized;
            Vector2 side = new(-direction.y, direction.x);
            Line(b, b-direction*15+side*8,color); Line(b,b-direction*15-side*8,color);
        }
        private static void Circle(Vector2 a, Vector2 b, Color color)
        {
            Vector2 center = (a+b)*.5f, radius = new(Mathf.Max(4,Mathf.Abs(a.x-b.x)*.5f), Mathf.Max(4,Mathf.Abs(a.y-b.y)*.5f));
            for (int i=0;i<48;i++)
            {
                float t=i*Mathf.PI*2/48, n=(i+1)*Mathf.PI*2/48;
                Line(center+new Vector2(Mathf.Cos(t)*radius.x,Mathf.Sin(t)*radius.y), center+new Vector2(Mathf.Cos(n)*radius.x,Mathf.Sin(n)*radius.y),color);
            }
        }
        private void OnDestroy() { foreach (var item in thumbnails.Values) if (item != null) Destroy(item); }
    }
}
