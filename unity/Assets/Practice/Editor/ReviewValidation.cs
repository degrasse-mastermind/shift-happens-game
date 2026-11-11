using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace ShiftHappens.Editor
{
    public static class ReviewValidation
    {
        [MenuItem("Shift Happens/Validate Review Mode")]
        public static void Run()
        {
            PracticeValidation.Run();
            foreach (bool guided in new[] { false, true })
            {
                var shift = new PracticeShift(1, new DraftShiftSettings(), guided);
                shift.TogglePlay();
                for (int i=0;i<70;i++) shift.Step(.02f);
                string frozen = JsonUtility.ToJson(new Snapshot(shift));
                shift.SetReviewing(true);
                for (int i=0;i<500;i++) { shift.TogglePlay(); shift.TieUp(); shift.SkateTo(Vector2.one*900); shift.Step(.25f); }
                Require(frozen == JsonUtility.ToJson(new Snapshot(shift)), "Review must preserve all exposed state despite inputs and steps.");
                shift.SetReviewing(false);
                Require(frozen == JsonUtility.ToJson(new Snapshot(shift)), "Leaving review cannot toggle teaching or manual pause.");
                if (shift.TeachingPaused) shift.TogglePlay();
                float time = shift.Time; shift.Step(.02f);
                Require(shift.Time > time, "Review gate must release without leaving play stuck.");
            }
            var note = new ReviewNote();
            note.entities.Add(new ReviewEntity("blue.ld", "Y", "blue", new Vector2(185,360)));
            note.marks.Add(new ReviewMark { kind="Move", entityId="blue.ld", from=new Vector2(185,360), to=new Vector2(200,320) });
            Require(note.Proposed("blue.ld") == new Vector2(200,320), "Proposed position missing.");
            var copy = note.Copy(); copy.Undo();
            Require(note.marks.Count==1 && copy.marks.Count==0 && copy.Proposed("blue.ld")==new Vector2(185,360), "Copy and undo must not modify source.");
            string root=Path.Combine(Path.GetTempPath(),"ShiftHappensReviewTest-"+Guid.NewGuid().ToString("N"));
            var texture=new Texture2D(16,16); texture.SetPixel(0,0,Color.red); texture.Apply();
            ReviewStorage.Save(note,texture.EncodeToPNG(),root);
            UnityEngine.Object.DestroyImmediate(texture);
            var bundle=ReviewStorage.Load(root);
            Require(bundle.notes.Count==1 && bundle.notes[0].marks.Count==1,"Reload must recover saved annotations.");
            string zip=ReviewStorage.Export(bundle,root);
            using(var archive=ZipFile.OpenRead(zip))
            {
                Require(archive.GetEntry("review.json")!=null && archive.GetEntry("review.md")!=null && archive.GetEntry(note.id+"/annotated.png")!=null,"Export must be self-contained.");
                using var reader=new StreamReader(archive.GetEntry("review.json").Open());
                Require(JsonUtility.FromJson<ReviewBundle>(reader.ReadToEnd()).notes[0].entities[0].id=="blue.ld","Export entity identity must survive JSON roundtrip.");
            }
            Debug.Log("REVIEW_CHECKS_PASSED: guided/freer freeze, resume, copy/undo, durable save/load, ZIP image/Markdown/JSON. Test output: "+root);
        }
        private static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Snapshot
        {
            public float time;
            public Vector2 player,partner,puck,carrier,goalie;
            public bool paused,teaching,tied,coverage;
            public string stage,hold,contain,possession,message;
            public Snapshot(PracticeShift s)
            {
                time=s.Time;player=s.Player;partner=s.Partner;puck=s.Puck;carrier=s.Carrier;goalie=s.Goalie;
                paused=s.Paused;teaching=s.TeachingPaused;tied=s.Tied;coverage=s.PartnerHasCoverage;
                stage=s.Stage.ToString();hold=s.HoldRead.ToString();contain=s.ContainRead.ToString();possession=s.PuckPossession.ToString();message=s.Message;
            }
        }
    }
}
