using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ShiftHappens.Editor
{
    public static class PracticeValidation
    {
        public static void Run()
        {
            try
            {
                CheckHandoff(false);
                CheckHandoff(true);
                CheckEarlyDeparture();
                CheckWrongSide();
                CheckPauseAndRetry();
                CheckLoosePuck();
                CheckMissedContainment();
                Debug.Log("PRACTICE_CHECKS_PASSED (7 scenarios)");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        public static void BuildWindows()
        {
            Run();
            string workspace = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string buildPath = Path.Combine(workspace, "artifacts", "windows", "Shift Happens.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(buildPath));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled), scene => scene.path),
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.StrictMode,
            });
            Require(report.summary.result == BuildResult.Succeeded, $"Windows build result: {report.summary.result}");
            Debug.Log($"PRACTICE_WINDOWS_BUILD_PASSED {report.summary.totalSize} bytes");
        }

        private static void CheckHandoff(bool mirrored)
        {
            var settings = new DraftShiftSettings { Mirror = mirrored };
            var shift = Start(1, settings);
            Vector2 start = settings.Map(shift.Partner);
            Require(start.x < 180 && start.y > 550, "RD must start in the bottom corner.");
            Require(!shift.PartnerHasCoverage, "Corner position must not count as coverage.");
            shift.Step(0.1f);
            Require(shift.Partner.y < start.y, "RD must skate toward net-front coverage.");
            AdvanceUntil(shift, () => shift.ContainmentAvailable, 10f);
            Require(shift.HoldRead == PracticeShift.ReadResult.Met, "Holding until RD is set should meet the first read.");
            Require(settings.Map(shift.Partner).x < PracticeShift.Center.x, "RD must be goal-side at the handoff.");
            Require(!shift.Containing, "The handoff alone must not count as containing.");
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Pass, 14f, () =>
            {
                Vector2 carrier = settings.Map(shift.Carrier);
                shift.SkateTo(settings.Map(carrier + new Vector2(-12, 60)));
            });
            Require(shift.ContainRead == PracticeShift.ReadResult.Met, "Closing from inside after coverage should meet the second read.");
            Require(!shift.SlotFeed, "A covered slot should redirect a slot look to the point.");
            Require(shift.Time < 14f && shift.Message.StartsWith("LD closes"), "Containment must pressure Red into an earlier point cycle.");
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Finished, 30f);
            Debug.Log($"Handoff verified. Mirror={mirrored}, handoff={shift.HandoffAt:0.00}s, {shift.DraftReview}.");
        }

        private static void CheckEarlyDeparture()
        {
            var shift = Start(0, new DraftShiftSettings { PartnerReturnDelay = 8f });
            shift.SkateTo(new Vector2(180, 200));
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Pass, 8f);
            Require(shift.SlotFeed && shift.Destination == shift.NetFrontAttacker, "Leaving early must expose a live slot feed, even on the point seed.");
            Require(!shift.HandoffAt.HasValue && shift.HoldRead == PracticeShift.ReadResult.Missed, "An early departure should miss the hold read.");
            Require(shift.ContainRead == PracticeShift.ReadResult.Unobserved, "A pass before the handoff must leave containment unobserved.");
        }

        private static void CheckWrongSide()
        {
            var shift = Start(0, new DraftShiftSettings { PartnerCoverageOffset = new Vector2(25, 20) });
            AdvanceUntil(shift, () => shift.Time >= 9f, 10f);
            Require(!shift.PartnerHasCoverage && !shift.HandoffAt.HasValue, "Arrival on the wrong side must never establish the handoff.");
        }

        private static void CheckPauseAndRetry()
        {
            var settings = new DraftShiftSettings { PartnerReturnDelay = 2f };
            var shift = new PracticeShift(1, settings);
            shift.TogglePlay();
            AdvanceUntil(shift, () => shift.TeachingPaused, 2f);
            float time = shift.Time;
            Vector2 puck = shift.Puck;
            Vector2 partner = shift.Partner;
            shift.Step(0.25f);
            Require(shift.Time == time && shift.Puck == puck && shift.Partner == partner, "Guided pause must freeze the entire play.");
            shift.TogglePlay();
            AdvanceUntil(shift, () => shift.Time >= 2.5f, 3f);
            Require(!shift.TeachingPaused && shift.Partner != partner && !shift.PartnerHasCoverage, "Resuming must continue RD's delayed return.");
            shift.TogglePlay();
            time = shift.Time;
            partner = shift.Partner;
            shift.Step(0.25f);
            Require(shift.Time == time && shift.Partner == partner, "Manual pause must freeze the same state.");
            var retry = new PracticeShift(shift.Seed, settings);
            Require(retry.Stage == PracticeShift.Phase.Ready && retry.Time == 0 && retry.Partner == settings.PartnerStart &&
                retry.HoldRead == PracticeShift.ReadResult.Pending && !retry.HandoffAt.HasValue, "Retry must restore starting positions and clear decision history.");
        }

        private static void CheckLoosePuck()
        {
            var shift = Start(1, new DraftShiftSettings { PartnerReturnDelay = 8f });
            shift.SkateTo(new Vector2(235, 374));
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Warning, 5f);
            shift.TieUp();
            Require(shift.Tied, "Stick reach must allow a tie-up before the pass arrives.");
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.LoosePuck, 8f);
            Require(shift.PuckPossession == PracticeShift.Possession.Loose, "A denied reception must create a loose puck.");
            shift.SkateTo(shift.Puck);
            shift.Step(0.02f);
            Require(shift.PuckPossession == PracticeShift.Possession.Loose, "An initial touch cannot establish control.");
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Finished, 10f);
            Require(shift.PuckPossession == PracticeShift.Possession.Blue, "Sustained reach should let Blue establish control.");
        }

        private static void CheckMissedContainment()
        {
            var shift = Start(1, new DraftShiftSettings());
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Finished, 30f);
            Require(shift.HoldRead == PracticeShift.ReadResult.Met && shift.ContainRead == PracticeShift.ReadResult.Missed,
                "Holding net front for the whole play must not count as moving out to contain.");
            Require(shift.Time >= 14f, "Without containment the carrier should have time to settle the play.");
        }

        private static PracticeShift Start(int seed, DraftShiftSettings settings)
        {
            var shift = new PracticeShift(seed, settings, guided: false);
            shift.TogglePlay();
            return shift;
        }

        private static void AdvanceUntil(PracticeShift shift, Func<bool> condition, float timeLimit, Action steer = null)
        {
            while (!condition() && shift.CanSkate && shift.Time < timeLimit)
            {
                steer?.Invoke();
                shift.Step(0.02f);
            }
            Require(condition(), $"Condition never reached at {shift.Time:0.00}s in {shift.Stage}. {shift.DraftReview}. {shift.Message}");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
