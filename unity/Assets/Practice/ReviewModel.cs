using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftHappens
{
    [Serializable] public sealed class ReviewEntity
    {
        public string id;
        public string label;
        public string team;
        public Vector2 position;
        public ReviewEntity(string id, string label, string team, Vector2 position)
        { this.id = id; this.label = label; this.team = team; this.position = position; }
    }
    [Serializable] public sealed class ReviewMark
    {
        public string kind;
        public string entityId;
        public Vector2 from;
        public Vector2 to;
    }
    [Serializable] public sealed class ReviewNote
    {
        public string id = Guid.NewGuid().ToString("N");
        public string createdUtc = DateTime.UtcNow.ToString("O");
        public string scope = "This moment";
        public string scene;
        public string buildVersion;
        public string unityVersion;
        public string mode;
        public int seed;
        public float gameTime;
        public string phase;
        public string possession;
        public string coachingReview;
        public string positioningReview;
        public string message;
        public bool gameWasPaused;
        public bool teachingWasPaused;
        public string settingsJson;
        public string orientation;
        public string coordinates = "Rink image coordinates: x increases right; y increases down. Image x=0..1260, y=70..720. Proposed changes only; not applied to gameplay.";
        public List<ReviewEntity> entities = new();
        public List<ReviewMark> marks = new();
        public string note = "";
        public string imageFile = "annotated.png";

        public Vector2 Proposed(string id)
        {
            var entity = entities.Find(item => item.id == id);
            Vector2 position = entity == null ? Vector2.zero : entity.position;
            foreach (var mark in marks) if (mark.kind == "Move" && mark.entityId == id) position = mark.to;
            return position;
        }
        public void Undo() { if (marks.Count > 0) marks.RemoveAt(marks.Count - 1); }
        public ReviewNote Copy() => JsonUtility.FromJson<ReviewNote>(JsonUtility.ToJson(this));
    }
    [Serializable] public sealed class ReviewBundle
    {
        public int schemaVersion = 1;
        public string purpose = "Coach-requested edits, not approved hockey rules. Ask about consequential ambiguity. Return interpreted changes, implemented changes, tests, and what to retest.";
        public List<ReviewNote> notes = new();
    }
}
