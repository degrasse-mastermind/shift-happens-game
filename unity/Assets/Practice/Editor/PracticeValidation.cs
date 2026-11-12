using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ShiftHappens.Editor
{
    public static class PracticeValidation
    {
        [MenuItem("Tools/Shift Happens/Validate Draft Practice")]
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
                CheckMarkedStartingPositions();
                CheckResponsiveSkating(new DraftShiftSettings());
                CheckResponsiveSkating(DraftShiftSettings.MarkedPreset());
                CheckInterruptedContainment(new DraftShiftSettings());
                CheckInterruptedContainment(DraftShiftSettings.MarkedPreset());
                CheckSupportPositions(new DraftShiftSettings());
                CheckSupportPositions(DraftShiftSettings.MarkedPreset());
                CheckPointLaneCut(LateCenterSettings(), upperPoint: false);
                CheckPointLaneCut(new DraftShiftSettings(), upperPoint: true);
                CheckBehindNet(false);
                CheckBehindNet(true);
                CheckBehindNetLateRecovery();
                Debug.Log("PRACTICE_CHECKS_PASSED (19 checks)");
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

        public static void BuildPhonePreview()
        {
            string destination = Environment.GetEnvironmentVariable("SHIFT_HAPPENS_PHONE_PREVIEW");
            Require(!string.IsNullOrWhiteSpace(destination), "Set the local phone preview destination.");
            Run();
            var previousCompression = PlayerSettings.WebGL.compressionFormat;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled), scene => scene.path),
                    locationPathName = destination,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.StrictMode,
                });
                Require(report.summary.result == BuildResult.Succeeded, $"Phone preview build result: {report.summary.result}");
                Debug.Log($"PRACTICE_PHONE_PREVIEW_BUILD_PASSED {report.summary.totalSize} bytes");
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = previousCompression;
            }
        }

        [MenuItem("Tools/Shift Happens/Playtest Weak-Side Cycle")]
        public static void PlaytestWeakSideCycle()
        {
            Vector2[] standardApproach = { new(-12, 60) };
            float[] immediate = { 0f };
            PlaytestPointCycle(new DraftShiftSettings(), "Original", standardApproach, immediate, 64);
            PlaytestPointCycle(DraftShiftSettings.MarkedPreset(), "Marked", standardApproach, immediate, 64);
            Vector2[] variedApproaches = { new(-48, 28), new(-32, 50), new(-12, 35), new(-12, 70), new(5, 50), new(5, 70) };
            float[] delays = { 0f, 1f, 3f, 5f };
            PlaytestPointCycle(DraftShiftSettings.MarkedPreset(), "Marked_varied", variedApproaches, delays, 32);

            PlaytestPointCycle(LateCenterSettings(), "Marked_late_C", standardApproach, immediate, 32);
        }

        [MenuItem("Tools/Shift Happens/Playtest Puck-Side Point")]
        public static void PlaytestPuckSidePoint()
        {
            var original = new DraftShiftSettings();
            var marked = DraftShiftSettings.MarkedPreset();
            CheckSupportPositions(original);
            CheckSupportPositions(marked, replaySeed: 2);
            Vector2[] approach = { new(-12, 60) };
            float[] immediate = { 0f };
            PlaytestPointCycle(original, "Original", approach, immediate, 64, upperPoint: true);
            PlaytestPointCycle(marked, "Marked", approach, immediate, 64, upperPoint: true);
        }

        private static DraftShiftSettings LateCenterSettings()
        {
            // Move only C's start in a test copy to observe recovery when the first lane defender is late.
            var settings = DraftShiftSettings.MarkedPreset();
            settings.BlueCenter = new Vector2(380, 620);
            return settings;
        }

        [MenuItem("Tools/Shift Happens/Watch Behind Net/Hold net front")]
        public static void WatchBehindNetHold() => WatchBehindNet(false, 0);

        [MenuItem("Tools/Shift Happens/Watch Behind Net/Chase the carrier")]
        public static void WatchBehindNetChase() => WatchBehindNet(true, 0);

        [MenuItem("Tools/Shift Happens/Watch Behind Net/Delayed exit")]
        public static void WatchBehindNetDelayed() => WatchBehindNet(false, 2);

        [MenuItem("Tools/Shift Happens/Watch Behind Net/Late recovery")]
        public static void WatchBehindNetLateRecovery() => WatchBehindNet(false, 0, recoverLate: true);

        private static void WatchBehindNet(bool chase, float wait, bool recoverLate = false)
        {
            string setup = wait > 0 ? "Behind net · delayed exit" : "Behind net";
            WatchReplay(DraftShiftSettings.BehindNetPreset(wait), 1, setup, "BEHIND THE NET · DRAFT", "LD",
                recoverLate ? "Watch LD reopen and then close the slot lane"
                    : chase ? "Compare what opens when LD chases" : "Watch LD cover Red C from the inside",
                replay =>
                {
                    if (recoverLate) SteerLateRecovery(replay);
                    else SteerBehindNet(replay, chase, trackThreat: wait <= 0f);
                    replay.Step(ReplayStep);
                    if (replay.Stage != PracticeShift.Phase.Finished) return null;
                    Debug.Log($"LIVE_BEHIND_NET chase={chase} lateRecovery={recoverLate} wait={wait:0.0} " +
                        $"slot={replay.SlotFeed} end={replay.Time:0.00} {replay.Message} {replay.ScoreSummary}");
                    return $"{replay.Message}  {replay.ScoreSummary}";
                },
                replay => replay.Stage == PracticeShift.Phase.Carry ? replay.BehindNetRoleCue : replay.Message,
                guided: true);
        }

        private static void SteerBehindNet(PracticeShift shift, bool chase, bool trackThreat)
        {
            if (chase)
            {
                if (shift.Time >= 2f && !shift.BehindNetExitAt.HasValue)
                    shift.SkateTo(shift.Carrier);
                return;
            }
            if (!trackThreat || shift.Stage != PracticeShift.Phase.Carry) return;

            Vector2 net = new(shift.Goalie.x, PracticeShift.Center.y);
            Vector2 threat = shift.NetFrontAttacker;
            shift.SkateTo(threat + (net - threat).normalized * 55f);
        }

        private static void CheckBehindNet(bool delayed)
        {
            var settings = DraftShiftSettings.BehindNetPreset(delayed ? 2 : 0);
            settings.Mirror = delayed;
            PracticeShift Play(bool chase)
            {
                var shift = new PracticeShift(1, settings, guided: true);
                shift.TogglePlay();
                bool sawPause = false;
                bool passed = false;
                float? firstPost = null;
                while (shift.Stage != PracticeShift.Phase.Finished)
                {
                    SteerBehindNet(shift, chase, trackThreat: !delayed);
                    shift.Step(ReplayStep);
                    if (shift.TeachingPaused)
                    {
                        Require(shift.BehindNetCrossing,
                            "The behind-net pause must start during the crossing.");
                        sawPause = true;
                        shift.TogglePlay();
                    }
                    Vector2 localPartner = settings.Map(shift.Partner);
                    if (localPartner.x < 135 && localPartner.y > 425 && localPartner.y < 445)
                        firstPost ??= shift.Time;
                    if (shift.Stage == PracticeShift.Phase.Pass) passed = true;
                }
                Require(sawPause && passed && shift.BehindNetExitAt.HasValue && firstPost < shift.BehindNetExitAt,
                    "The replay must show RD sealing before Red exits and reach a pass.");
                Debug.Log($"BEHIND_NET_CHECK delayed={delayed} chase={chase} exit={shift.BehindNetExitAt:0.00} " +
                    $"slot={shift.SlotFeed} end={shift.Time:0.00} {shift.Message} {shift.ScoreSummary}");
                return shift;
            }
            PracticeShift held = Play(false);
            PracticeShift chased = Play(true);
            Require(Mathf.Abs(held.BehindNetExitAt.Value - (delayed ? 9.8f : 7.8f)) < .05f,
                "The configured wait must change when Red exits.");
            Require(!held.SlotFeed && chased.SlotFeed, "LD's route must change whether Red tries the slot.");
            Require(held.HoldRead == PracticeShift.ReadResult.Met && held.ContainRead == PracticeShift.ReadResult.Met &&
                chased.ContainRead == PracticeShift.ReadResult.Missed, "Draft reads must reflect LD's coverage.");
        }

        private static void SteerLateRecovery(PracticeShift shift)
        {
            if (!shift.BehindNetExitAt.HasValue)
            {
                SteerBehindNet(shift, chase: false, trackThreat: true);
                return;
            }
            Vector2 threat = shift.NetFrontAttacker;
            if (shift.Stage == PracticeShift.Phase.Carry)
                shift.SkateTo(threat);
            else if (shift.Stage == PracticeShift.Phase.Warning)
            {
                Vector2 net = new(shift.Goalie.x, PracticeShift.Center.y);
                shift.SkateTo(threat + (net - threat).normalized * 45f);
            }
        }

        private static void CheckBehindNetLateRecovery()
        {
            var shift = new PracticeShift(1, DraftShiftSettings.BehindNetPreset(), guided: false);
            shift.TogglePlay();
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Warning, 10f,
                () => SteerLateRecovery(shift));
            Require(!shift.SlotLaneCovered, "Red must see an open slot before LD recovers.");
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Finished, 20f,
                () => SteerLateRecovery(shift));
            Require(!shift.SlotFeed && shift.HoldRead == PracticeShift.ReadResult.Met &&
                shift.ContainRead == PracticeShift.ReadResult.Missed &&
                shift.ScoreSummary.Contains("Red saw an open slot."),
                "Recovery should redirect the pass without erasing the opening LD gave Red.");
            Debug.Log($"BEHIND_NET_LATE_RECOVERY slot={shift.SlotFeed} {shift.ScoreSummary}");
        }

        private static EditorApplication.CallbackFunction liveReplayTick;
        private const float ReplayStep = 0.02f;

        [MenuItem("Tools/Shift Happens/Watch First Guided Shift/Hold then close")]
        private static void WatchFirstGuidedHold() => WatchFirstGuidedShift(leaveEarly: false);

        [MenuItem("Tools/Shift Happens/Watch First Guided Shift/Leave early")]
        private static void WatchFirstGuidedEarly() => WatchFirstGuidedShift(leaveEarly: true);

        [MenuItem("Tools/Shift Happens/Watch First Guided Shift/Hold then close in current setup")]
        private static void WatchCurrentOriginal()
        {
            var board = UnityEngine.Object.FindAnyObjectByType<RinkPractice>();
            if (!EditorApplication.isPlaying || board == null ||
                !board.TryGetOriginalReplay(out var settings, out int seed, out string setup))
            {
                Debug.LogWarning("In Play Mode, select Original or apply an RD draft before watching the replay.");
                return;
            }
            WatchFirstGuidedShift(leaveEarly: false, settings, seed, setup);
        }

        private static void WatchFirstGuidedShift(bool leaveEarly, DraftShiftSettings settings = null, int seed = 1,
            string setup = "Original")
        {
            string choice = leaveEarly ? "Leave early" : "Hold then close";
            WatchReplay(settings ?? new DraftShiftSettings(), seed, setup, $"{choice.ToUpperInvariant()} · SEED {seed}", "LD",
                leaveEarly ? "Watch LD leave before RD is set." : "Watch LD wait for RD, then close from inside.",
                replay =>
                {
                    if (replay.Stage == PracticeShift.Phase.Carry)
                    {
                        if (leaveEarly && replay.Time >= 0.8f)
                            replay.SkateTo(new Vector2(180, 200));
                        else if (!leaveEarly && replay.ContainmentAvailable)
                            replay.SkateTo(replay.Carrier + new Vector2(-12, 60));
                    }
                    replay.Step(ReplayStep);
                    if (replay.Stage != PracticeShift.Phase.Finished) return null;
                    Debug.Log($"FIRST_GUIDED_REPLAY leaveEarly={leaveEarly} handoff={replay.HandoffAt:0.00} " +
                        $"openedSlot={replay.OpenedSlotBeforeHandoff} time={replay.Time:0.00} " +
                        $"{replay.Message} {replay.ScoreSummary}");
                    return replay.Message;
                },
                guided: true,
                checkpoint: replay => replay.ContainmentAvailable && replay.HandoffAt == replay.Time
                    ? "RD has net front." : null,
                showFirstGuidedShift: true);
        }

        [MenuItem("Tools/Shift Happens/Watch Weak-Side Lane Cut")]
        public static void WatchWeakSideLaneCut() =>
            WatchPointLaneCut(LateCenterSettings(), 0, "Marked upper corner · late C", upperPoint: false);

        [MenuItem("Tools/Shift Happens/Watch Puck-Side Cut/Original handoff")]
        public static void WatchOriginalPuckSideCut() =>
            WatchPointLaneCut(new DraftShiftSettings(), 0, "Original handoff · seed 0", upperPoint: true);

        [MenuItem("Tools/Shift Happens/Watch Puck-Side Cut/Marked upper corner")]
        public static void WatchMarkedPuckSideCut() =>
            WatchPointLaneCut(DraftShiftSettings.MarkedPreset(), 2, "Marked upper corner · seed 2", upperPoint: true);

        [MenuItem("Tools/Shift Happens/Watch Center Support/Original handoff")]
        public static void WatchOriginalCenterSupport() =>
            WatchCenterSupport(new DraftShiftSettings(), 1, "Original handoff · seed 1");

        [MenuItem("Tools/Shift Happens/Watch Center Support/Marked upper corner")]
        public static void WatchMarkedCenterSupport() =>
            WatchCenterSupport(DraftShiftSettings.MarkedPreset(), 0, "Marked upper corner · seed 0");

        private static void WatchCenterSupport(DraftShiftSettings settings, int seed, string setup)
        {
            float? containStart = null;
            WatchReplay(settings, seed, setup, "C SUPPORT FINAL", "C",
                "Watch teal C support from inside the puck-side circle", replay =>
                {
                    if (replay.Time == 0) containStart = null;
                    if (replay.ContainmentAvailable) replay.SkateTo(replay.Carrier + new Vector2(-12, 60));
                    replay.Step(ReplayStep);
                    if (!replay.Containing) return null;
                    containStart ??= replay.Time;
                    if (replay.Time < containStart.Value + 0.7f) return null;
                    float margin = CenterShoulderDepth(replay, settings.Map(new Vector2(105, 397)));
                    Debug.Log($"LIVE_CENTER_SUPPORT seed={seed} time={replay.Time:0.00} C={replay.BlueCenter} insideMargin={margin:0.0}");
                    return $"C supports inside the puck-side circle · {(margin > 0f ? "inside shoulder" : "outside shoulder")}";
                },
                replay => !replay.HandoffAt.HasValue ? "Before the handoff · RD is returning and LD holds the slot lane"
                    : "After the handoff · RD covers net front and LD moves out to contain");
        }

        [MenuItem("Tools/Shift Happens/Watch LD Positioning/Original handoff")]
        public static void WatchOriginalLdPositioning() =>
            WatchLdPositioning(new DraftShiftSettings(), 1, "Original handoff · seed 1");

        [MenuItem("Tools/Shift Happens/Watch LD Positioning/Marked upper corner")]
        public static void WatchMarkedLdPositioning() =>
            WatchLdPositioning(DraftShiftSettings.MarkedPreset(), 0, "Marked upper corner · seed 0");

        private static void WatchLdPositioning(DraftShiftSettings settings, int seed, string setup)
        {
            float? containStart = null;
            string hold = settings.HoldFromPassingLane
                ? "LD holds the passing lane to net front"
                : "LD holds net front";
            WatchReplay(settings, seed, setup, "LD POSITIONING APPROVED", "LD",
                "Follow the ring under Blue LD", replay =>
                {
                    if (replay.Time == 0) containStart = null;
                    if (replay.ContainmentAvailable) replay.SkateTo(replay.Carrier + new Vector2(-12, 60));
                    replay.Step(ReplayStep);
                    if (!replay.Containing)
                    {
                        containStart = null;
                        return null;
                    }
                    containStart ??= replay.Time;
                    if (replay.Time < containStart.Value + 0.7f) return null;
                    LogLdPosition("contain", setup, replay);
                    return "LD contains from inside while RD covers net front";
                },
                replay => replay.HandoffAt.HasValue ? "RD has net front · LD closes from inside" : hold + " while RD returns",
                checkpoint: replay =>
                {
                    if (!replay.ContainmentAvailable || replay.HandoffAt != replay.Time) return null;
                    LogLdPosition("handoff", setup, replay);
                    return hold + " until RD takes coverage";
                });
        }

        private static void LogLdPosition(string read, string setup, PracticeShift replay) =>
            Debug.Log(FormattableString.Invariant(
                $"LIVE_LD_POSITION {read} {setup} time={replay.Time:0.00} LD={replay.Player} RD={replay.Partner} carrier={replay.Carrier} slotCovered={replay.SlotLaneCovered} rdCovering={replay.PartnerHasCoverage} containing={replay.Containing}"));

        [MenuItem("Tools/Shift Happens/Watch RD Return/Original handoff")]
        public static void WatchOriginalRdReturn() =>
            WatchRdReturn(new DraftShiftSettings(), 1, "Original handoff · seed 1");

        [MenuItem("Tools/Shift Happens/Watch RD Return/Marked upper corner")]
        public static void WatchMarkedRdReturn() =>
            WatchRdReturn(DraftShiftSettings.MarkedPreset(), 0, "Marked upper corner · seed 0");

        private static void WatchRdReturn(DraftShiftSettings settings, int seed, string setup)
        {
            float? insideAt = null;
            float? insideAndInReachAt = null;
            Vector2 net = settings.Map(new Vector2(105, 397));
            void Observe(PracticeShift replay)
            {
                Vector2 threat = replay.NetFrontAttacker;
                if (Vector2.Dot(replay.Partner - threat, net - threat) <= 0f) return;
                insideAt ??= replay.Time;
                if (Vector2.Distance(replay.Partner, threat) <= settings.CoverageReach)
                    insideAndInReachAt ??= replay.Time;
            }
            WatchReplay(settings, seed, setup, "RD RETURN FINAL", "RD",
                "Follow teal RD returning to the net-front threat", replay =>
                {
                    if (replay.Time == 0)
                    {
                        insideAt = null;
                        insideAndInReachAt = null;
                        Observe(replay);
                    }
                    replay.Step(ReplayStep);
                    Observe(replay);
                    if (!replay.PartnerHasCoverage) return null;
                    Debug.Log(FormattableString.Invariant(
                        $"LIVE_RD_RETURN {setup} inside={insideAt:0.00} insideAndInReach={insideAndInReachAt:0.00} coverage={replay.Time:0.00} RD={replay.Partner} threat={replay.NetFrontAttacker}"));
                    return $"RD establishes coverage on Red {settings.NetFrontLabel} at {replay.Time:0.00}s · LD may contain";
                },
                _ => insideAndInReachAt.HasValue
                    ? $"RD reached the inside shoulder within coverage reach at {insideAndInReachAt:0.00}s"
                    : "LD holds the slot lane while RD returns");
        }

        private static void WatchPointLaneCut(DraftShiftSettings settings, int seed, string setup, bool upperPoint)
        {
            string wing = upperPoint ? "LW" : "RW";
            string title = upperPoint ? "LW PUCK SIDE FINAL" : "RW WEAK SIDE FINAL";
            WatchReplay(settings, seed, setup, title, wing, $"Watch teal {wing} cut the lane on Red's release", replay =>
            {
                if (replay.ContainmentAvailable) replay.SkateTo(replay.Carrier + new Vector2(-12, 60));
                var before = replay.Stage;
                Vector2 puckBefore = replay.Puck;
                replay.Step(ReplayStep);
                if (before != PracticeShift.Phase.Pass || replay.Stage == before) return null;

                Vector2 defender = upperPoint ? replay.BlueLeftWing : replay.BlueRightWing;
                Vector2 receiver = upperPoint ? replay.RedRightDefense : replay.RedLeftDefense;
                bool inside = Vector2.Dot(defender - receiver, settings.Map(new Vector2(105, 397)) - receiver) > 0;
                bool wingBrokeItUp = PassDeflector(replay, puckBefore) == "Blue_" + wing;
                Debug.Log($"LIVE_LANE_CUT wing={wing} seed={seed} time={replay.Time:0.00} breakup={wingBrokeItUp} inside={inside}");
                return wingBrokeItUp
                    ? $"{wing} breaks up the pass · {(inside ? "inside shoulder" : "outside shoulder")}"
                    : replay.Message;
            });
        }

        [MenuItem("Tools/Shift Happens/Watch Crowded Handoff")]
        private static void WatchCrowdedHandoff() => WatchCrowdedHandoff(false);

        [MenuItem("Tools/Shift Happens/Watch Mirrored Crowded Handoff")]
        private static void WatchMirroredCrowdedHandoff() => WatchCrowdedHandoff(true);

        private static void WatchCrowdedHandoff(bool mirrored)
        {
            var settings = new DraftShiftSettings
            {
                Mirror = mirrored,
                PartnerStart = new Vector2(145, 459),
                BlueCenter = new Vector2(184, 381),
            };
            string setup = mirrored ? "Mirrored crowd · seed 1" : "Crowded handoff · seed 1";
            WatchReplay(settings, 1, setup, "CROWDED NET FRONT", "LD",
                "Watch LD hold the slot as RD returns from below the net.", replay =>
                {
                    replay.Step(ReplayStep);
                    if (replay.TeachingPaused)
                        LogCrowd("hold", mirrored, replay);
                    if (!replay.ContainmentAvailable) return null;
                    LogCrowd("handoff", mirrored, replay);
                    return "RD covers net front  |  LD may contain from inside";
                }, guided: true);
        }

        private static void LogCrowd(string read, bool mirrored, PracticeShift replay) =>
            Debug.Log($"LIVE_CROWDED_HANDOFF {read} mirrored={mirrored} seed={replay.Seed} time={replay.Time:0.00} " +
                $"LD={replay.Player} RD={replay.Partner} BlueC={replay.BlueCenter} RedC={replay.NetFrontAttacker} " +
                $"slotCovered={replay.SlotLaneCovered} containAvailable={replay.ContainmentAvailable}");

        private static void WatchReplay(DraftShiftSettings settings, int seed, string setup, string title, string focus,
            string prompt, Func<PracticeShift, string> advance, Func<PracticeShift, string> caption = null,
            bool guided = false, Func<PracticeShift, string> checkpoint = null, bool showFirstGuidedShift = false)
        {
            var board = UnityEngine.Object.FindAnyObjectByType<RinkPractice>();
            if (!EditorApplication.isPlaying || board == null)
            {
                Debug.LogWarning("Open Defensive Practice and enter Play Mode to watch the replay.");
                return;
            }

            PracticeShift replay = null;
            double beginsAt = 0;
            double lastTick = 0;
            double pending = 0;
            double? resumeTeachingAt = null;
            double resumeCheckpointAt = 0;
            EditorApplication.update -= liveReplayTick;

            void Restart()
            {
                replay = new PracticeShift(seed, settings, guided);
                beginsAt = EditorApplication.timeSinceStartup + 4;
                lastTick = beginsAt;
                pending = 0;
                resumeTeachingAt = null;
                resumeCheckpointAt = 0;
                if (!board.ShowEditorReplay(replay, setup, focus, Restart, Stop, showFirstGuidedShift))
                {
                    Debug.LogWarning("Close Review Mode before starting the live replay.");
                    return;
                }
                board.DrawEditorReplay(title, "Replay starts in 4…", prompt);
                EditorApplication.update -= liveReplayTick;
                EditorApplication.update += liveReplayTick;
            }

            void Stop() => EditorApplication.update -= Tick;

            void Tick()
            {
                if (!EditorApplication.isPlaying || board == null)
                {
                    EditorApplication.update -= liveReplayTick;
                    return;
                }
                double now = EditorApplication.timeSinceStartup;
                if (now < beginsAt)
                {
                    board.DrawEditorReplay(title, $"Replay starts in {Math.Ceiling(beginsAt - now):0}…", prompt);
                    return;
                }
                if (now < resumeCheckpointAt)
                {
                    lastTick = now;
                    return;
                }
                if (replay.Stage == PracticeShift.Phase.Ready) replay.TogglePlay();
                if (replay.TeachingPaused)
                {
                    resumeTeachingAt ??= now + 4;
                    lastTick = now;
                    pending = 0;
                    if (now < resumeTeachingAt.Value)
                    {
                        board.DrawEditorReplay(title, replay.Message, "Teaching pause · replay resumes automatically");
                        return;
                    }
                    replay.TogglePlay();
                }
                pending += Math.Min(now - lastTick, 0.1) * 0.7;
                lastTick = now;
                while (pending >= ReplayStep && replay.CanSkate)
                {
                    string result = advance(replay);
                    pending -= ReplayStep;
                    if (result != null)
                    {
                        board.DrawEditorReplay(title, "Replay paused. Select Replay to watch again.", result);
                        EditorApplication.update -= liveReplayTick;
                        return;
                    }
                    string checkpointCaption = checkpoint?.Invoke(replay);
                    if (checkpointCaption != null)
                    {
                        resumeCheckpointAt = now + 4;
                        pending = 0;
                        board.DrawEditorReplay(title, checkpointCaption, "Handoff pause · replay resumes automatically");
                        return;
                    }
                }
                board.DrawEditorReplay(title, caption?.Invoke(replay) ?? replay.Message, prompt);
                if (replay.Stage == PracticeShift.Phase.Finished) EditorApplication.update -= liveReplayTick;
            }

            liveReplayTick = Tick;
            Restart();
        }

        private static void AdvanceToCycle(PracticeShift shift, Vector2 approach, float delay)
        {
            while (shift.Stage == PracticeShift.Phase.Carry && shift.Time < 14f)
            {
                if (shift.ContainmentAvailable && shift.Time >= shift.HandoffAt.GetValueOrDefault() + delay)
                    shift.SkateTo(shift.Carrier + approach);
                shift.Step(0.02f);
            }
        }

        private static void CheckPointLaneCut(DraftShiftSettings settings, bool upperPoint)
        {
            var shift = Start(0, settings);
            string receiver = upperPoint ? "RD" : "LD";
            string defender = upperPoint ? "Blue_LW" : "Blue_RW";
            AdvanceToCycle(shift, new Vector2(-12, 60), 0f);
            Require(shift.Stage == PracticeShift.Phase.Pass && !shift.SlotFeed && shift.PassReceiver == receiver,
                $"The seed-0 comparison must release to Red {receiver} at the point.");
            Vector2 Wing() => upperPoint ? shift.BlueLeftWing : shift.BlueRightWing;
            Vector2 wingAtRelease = Wing();
            Vector2 previousPuck = shift.Puck;
            shift.Step(0.02f);
            Require(Vector2.Distance(Wing(), wingAtRelease) > 0.001f, $"{defender} should move on release.");
            if (!upperPoint)
                Require(Wing().y < wingAtRelease.y, "RW should start cutting toward the weak-side lane on release.");
            while (shift.Stage == PracticeShift.Phase.Pass)
            {
                previousPuck = shift.Puck;
                shift.Step(0.02f);
                Vector2 opponent = upperPoint ? shift.RedRightDefense : shift.RedLeftDefense;
                Require(Vector2.Dot(Wing() - opponent, new Vector2(105, 397) - opponent) > 0,
                    $"{defender} must stay on the net side of Red {receiver} while cutting the lane.");
            }
            Require(PassDeflector(shift, previousPuck) == defender,
                $"{defender} should break up the seed-0 point pass before it reaches Red {receiver}.");
        }

        private static void PlaytestPointCycle(DraftShiftSettings settings, string preset,
            Vector2[] approaches, float[] delays, int seedCount, bool upperPoint = false)
        {
            const float step = 0.02f;
            string logTag = upperPoint ? "PUCK_SIDE_CYCLE" : "WEAK_CYCLE";
            string receiver = upperPoint ? "RD" : "LD";
            string wingLabel = upperPoint ? "lw" : "rw";
            var counts = new Dictionary<string, int>();
            var examples = new Dictionary<string, string>();
            float smallestShoulderDepth = float.PositiveInfinity;
            float latestRecoveryStart = 0f;
            foreach (Vector2 approach in approaches)
            foreach (float delay in delays)
            for (int seed = 0; seed < seedCount; seed++)
            {
                var shift = Start(seed, settings);
                AdvanceToCycle(shift, approach, delay);
                if (shift.Stage != PracticeShift.Phase.Pass || shift.SlotFeed || shift.PassReceiver != receiver ||
                    shift.ContainRead != PracticeShift.ReadResult.Met || shift.Puck.y >= 397f)
                    continue;

                Vector2 net = settings.Map(new Vector2(105, 397));
                Vector2 Wing() => upperPoint ? shift.BlueLeftWing : shift.BlueRightWing;
                Vector2 PointSkater() => upperPoint ? shift.RedRightDefense : shift.RedLeftDefense;
                float ShoulderDepth() => Vector2.Dot(Wing() - PointSkater(), (net - PointSkater()).normalized);
                string Point(Vector2 point) => FormattableString.Invariant($"{point.x:0.0},{point.y:0.0}");
                string Snapshot() => $"{wingLabel}={Point(Wing())} point={Point(PointSkater())} puck={Point(shift.Puck)}";

                float released = shift.Time;
                Vector2 wingAtRelease = Wing();
                string releaseFrame = Snapshot();
                float minShoulderDepth = ShoulderDepth();
                float minPuckDistance = Vector2.Distance(Wing(), shift.Puck);
                float? recoveryStart = null;
                string outcome = null;
                while (shift.Stage == PracticeShift.Phase.Pass)
                {
                    Vector2 previous = shift.Puck;
                    shift.Step(step);
                    if (!recoveryStart.HasValue && Vector2.Distance(Wing(), wingAtRelease) > 0.001f)
                        recoveryStart = shift.Time - released;
                    minShoulderDepth = Mathf.Min(minShoulderDepth, ShoulderDepth());
                    minPuckDistance = Mathf.Min(minPuckDistance, LaneDistance(Wing(), previous, shift.Puck));
                    outcome = PassDeflector(shift, previous);
                }
                smallestShoulderDepth = Mathf.Min(smallestShoulderDepth, minShoulderDepth);
                latestRecoveryStart = Mathf.Max(latestRecoveryStart, recoveryStart ?? float.PositiveInfinity);
                outcome ??= shift.Stage == PracticeShift.Phase.Shot ? "Red_shot" : "Loose_at_point";
                counts.TryGetValue(outcome, out int count);
                counts[outcome] = count + 1;
                string endFrame = Snapshot();
                float elapsed = shift.Time - released;
                float endGap = Vector2.Distance(Wing(), PointSkater());
                AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Finished, 30f);
                examples.TryAdd(outcome, FormattableString.Invariant(
                    $"{logTag} {preset} seed={seed} approach={Point(approach)} delay={delay:0.0} outcome={outcome} release={released:0.00} recover={recoveryStart:0.00} flight={elapsed:0.00} insideMin={minShoulderDepth:0.0} puckGap={minPuckDistance:0.0} receiverGap={endGap:0.0} finish={shift.PuckPossession} start {releaseFrame} end {endFrame}"));
            }
            Debug.Log(FormattableString.Invariant(
                $"{logTag} {preset} runs={seedCount * approaches.Length * delays.Length} insideMin={smallestShoulderDepth:0.0} latestRecovery={latestRecoveryStart:0.00} {string.Join(", ", counts)}"));
            foreach (string example in examples.Values) Debug.Log(example);
        }

        private static string PassDeflector(PracticeShift shift, Vector2 previous)
        {
            if (shift.Stage != PracticeShift.Phase.LoosePuck || !shift.Message.StartsWith("Blue's stick")) return null;
            return LaneDistance(shift.Player, previous, shift.Puck) < 18f ? "Blue_LD"
                : LaneDistance(shift.Partner, previous, shift.Puck) < 18f ? "Blue_RD"
                : LaneDistance(shift.BlueCenter, previous, shift.Puck) < 16f ? "Blue_C"
                : LaneDistance(shift.BlueLeftWing, previous, shift.Puck) < 16f ? "Blue_LW"
                : "Blue_RW";
        }

        private static float LaneDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 lane = end - start;
            float along = lane.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - start, lane) / lane.sqrMagnitude) : 0f;
            return Vector2.Distance(point, start + along * lane);
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
            Require(!shift.OpenedSlotBeforeHandoff, "A successful hold must not report an early slot opening.");
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
            Debug.Log($"Original hold and contain: mirror={mirrored}, handoff={shift.HandoffAt:0.00}s, Red {shift.PassReceiver}, {shift.Time:0.00}s, {shift.PuckPossession}, {shift.Message} {shift.ScoreSummary}");
        }

        private static void CheckEarlyDeparture()
        {
            var shift = Start(0, new DraftShiftSettings { PartnerReturnDelay = 8f });
            shift.SkateTo(new Vector2(180, 200));
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Pass, 8f);
            Require(shift.SlotFeed && shift.Destination == shift.NetFrontAttacker, "Leaving early must expose a live slot feed, even on the point seed.");
            Require(!shift.HandoffAt.HasValue && shift.HoldRead == PracticeShift.ReadResult.Missed, "An early departure should miss the hold read.");
            Require(shift.OpenedSlotBeforeHandoff, "The review must retain the early slot opening after Red passes.");
            Require(shift.ContainRead == PracticeShift.ReadResult.Unobserved, "A pass before the handoff must leave containment unobserved.");
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Finished, 30f);
            Require(shift.OpenedSlotBeforeHandoff, "The finished review must retain the early slot opening.");
            Debug.Log($"Original early departure: Red {shift.PassReceiver}, {shift.Time:0.00}s, {shift.PuckPossession}, {shift.Message} {shift.ScoreSummary}");
        }

        private static void CheckWrongSide()
        {
            var settings = new DraftShiftSettings { Mirror = true, PartnerCoverageOffset = new Vector2(25, 20) };
            var shift = Start(0, settings);
            AdvanceUntil(shift, () => shift.Time >= 5.9f, 7f);
            Require(shift.PartnerArrivedWrongSide, "The wrong-side cue should be available once RD arrives.");
            Require(shift.PositioningSummary.Contains("RD (final) outside"), "The positioning review must flag RD on the outside shoulder.");
            Require(!shift.PartnerHasCoverage && !shift.HandoffAt.HasValue, "Arrival on the wrong side must never establish the handoff.");
            shift.SkateTo(settings.Map(new Vector2(170, 160)));
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Pass, 12f);
            Require(shift.SlotFeed && shift.Destination == shift.NetFrontAttacker, "A late departure before coverage must expose the slot feed.");
            Require(shift.HoldRead == PracticeShift.ReadResult.Missed && shift.ContainRead == PracticeShift.ReadResult.Unobserved,
                "Earlier hold time must not erase an exposed slot before coverage.");
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

        private static void CheckMarkedStartingPositions()
        {
            var settings = DraftShiftSettings.MarkedPreset();
            var early = new PracticeShift(0, settings);
            Require(early.Carrier == settings.CarrierStart && early.Puck == settings.PuckStart &&
                early.NetFrontAttacker == settings.NetFrontStart && early.Player == settings.PlayerStart &&
                early.Partner == settings.PartnerStart, "The marked scene must load the marked starting positions.");
            early.TogglePlay();
            AdvanceUntil(early, () => early.TeachingPaused, 2f);
            early.TogglePlay();
            early.SkateTo(new Vector2(245, 306));
            AdvanceUntil(early, () => early.Stage == PracticeShift.Phase.Warning, 10f);
            Require(early.SlotFeed && !early.SlotLaneCovered && !early.HandoffAt.HasValue,
                "Leaving the marked lane early should expose the feed to LW before RD is set.");
            AdvanceUntil(early, () => early.Stage == PracticeShift.Phase.Pass, 10f);
            Require(early.SlotFeed == !early.SlotLaneCovered && early.HoldRead == PracticeShift.ReadResult.Missed,
                "Red should release to LW if the lane is still open, and RD's recovery must not erase the early miss.");
            AdvanceUntil(early, () => early.Stage == PracticeShift.Phase.Finished, 30f);
            Debug.Log($"Marked early departure: Red {early.PassReceiver}, {early.Time:0.00}s, {early.PuckPossession}, {early.Message} {early.ScoreSummary}");

            var held = Start(0, settings);
            AdvanceUntil(held, () => held.ContainmentAvailable, 10f);
            Require(held.HoldRead == PracticeShift.ReadResult.Met, "LD should hold the marked passing lane until RD is set.");
            Require(!held.Containing, "The marked hold position must leave LD a separate containment decision.");
            AdvanceUntil(held, () => held.Stage == PracticeShift.Phase.Pass, 14f, () =>
                held.SkateTo(held.Carrier + new Vector2(-12, 60)));
            Require(!held.SlotFeed && (held.Destination == held.RedLeftDefense || held.Destination == held.RedRightDefense) &&
                held.ContainRead == PracticeShift.ReadResult.Met, "Containment from inside should send Red C to the marked point.");
            AdvanceUntil(held, () => held.Stage == PracticeShift.Phase.Finished, 30f);
            Debug.Log($"Marked hold and contain: Red {held.PassReceiver}, {held.Time:0.00}s, {held.PuckPossession}, {held.Message} {held.ScoreSummary}");
        }

        private static void CheckInterruptedContainment(DraftShiftSettings settings)
        {
            var shift = Start(0, settings);
            AdvanceUntil(shift, () => shift.ContainmentAvailable, 10f);
            float total = 0;
            float longest = 0;
            void Steer(float range) => shift.SkateTo(shift.Carrier + new Vector2(-12, range));

            for (int attempt = 0; attempt < 2; attempt++)
            {
                AdvanceUntil(shift, () => shift.Containing, 11f, () => Steer(settings.ContainDistance - 5f));
                float began = shift.Time;
                AdvanceUntil(shift, () => shift.Time - began >= 0.56f, 12f,
                    () => Steer(settings.ContainDistance - 5f));
                Require(shift.Containing, "The test must hold position through each short attempt.");
                AdvanceUntil(shift, () => !shift.Containing, 13f, () => Steer(settings.ContainDistance + 25f));
                float duration = shift.Time - began;
                total += duration;
                longest = Mathf.Max(longest, duration);
                Require(shift.ContainmentAvailable && shift.Stage == PracticeShift.Phase.Carry,
                    "A short attempt must end through LD's movement while RD still covers and Red still carries.");
            }

            Require(total >= 1f && longest < 1f, $"Expected two short attempts totaling at least one second, got {total:0.00}s total and {longest:0.00}s longest.");
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Pass, 16f,
                () => Steer(settings.ContainDistance + 25f));
            Require(shift.HoldRead == PracticeShift.ReadResult.Met && shift.ContainRead == PracticeShift.ReadResult.Missed &&
                shift.Time >= 14f, "Broken attempts must score Missed and leave Red time to carry.");
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Finished, 30f);
            Debug.Log($"Interrupted containment: corner={settings.CornerCarry}, {total:0.00}s total, {longest:0.00}s longest, Red {shift.PassReceiver}, {shift.Time:0.00}s, {shift.PuckPossession}. {shift.ScoreSummary}");
        }

        private static void CheckSupportPositions(DraftShiftSettings settings, int? replaySeed = null)
        {
            var shift = Start(replaySeed ?? (settings.CornerCarry ? 0 : 1), settings);
            string preset = settings.CornerCarry ? "Marked" : "Original";
            string Snapshot(string frame)
            {
                string Point(Vector2 p) => FormattableString.Invariant($"{p.x:0.0},{p.y:0.0}");
                return FormattableString.Invariant(
                    $"BOX_POSITION {preset} {frame} seed={shift.Seed} time={shift.Time:0.00} C={Point(shift.BlueCenter)} LW={Point(shift.BlueLeftWing)} RW={Point(shift.BlueRightWing)} LD={Point(shift.Player)} RD={Point(shift.Partner)} Carrier={Point(shift.Carrier)} Threat={Point(shift.NetFrontAttacker)} Point={Point(shift.RedRightDefense)} FarPoint={Point(shift.RedLeftDefense)} Puck={Point(shift.Puck)} Support={Point(shift.RedSupport)}");
            }

            string before = null;
            string shouldersBefore = null;
            Vector2 centerBefore = shift.BlueCenter;
            Vector2 weakWingBefore = shift.BlueRightWing;
            Vector2 net = settings.Map(new Vector2(105, 397));
            float shoulderBefore = 0f;
            AdvanceUntil(shift, () => shift.ContainmentAvailable, 10f, () =>
            {
                before = Snapshot("before");
                shouldersBefore = shift.PositioningSummary;
                centerBefore = shift.BlueCenter;
                weakWingBefore = shift.BlueRightWing;
                shoulderBefore = CenterShoulderDepth(shift, net);
            });
            Require(shift.PartnerHasCoverage && !shift.Containing, "RD must cover before LD closes.");

            float minimumShoulder = shoulderBefore;
            void CloseFromInside()
            {
                minimumShoulder = Mathf.Min(minimumShoulder, CenterShoulderDepth(shift, net));
                shift.SkateTo(shift.Carrier + new Vector2(-12, 60));
            }
            AdvanceUntil(shift, () => shift.Containing, 10f, CloseFromInside);
            float containStart = shift.Time;
            AdvanceUntil(shift, () => shift.Time >= containStart + 0.7f, 12f, CloseFromInside);
            Vector2 circleDot = new(242, 245);
            Require(shift.Containing && shift.PartnerHasCoverage && shift.Stage == PracticeShift.Phase.Carry,
                "Capture support with LD containing, RD covering and Red still carrying.");
            Require(Vector2.Distance(shift.BlueCenter, circleDot) <= 75f &&
                Vector2.Distance(shift.BlueCenter, circleDot) + 15f < Vector2.Distance(centerBefore, circleDot),
                $"Blue C should move inside the puck-side circle. {before} {Snapshot("after")}");
            string shouldersAfter = shift.PositioningSummary;
            Require(shouldersBefore.Contains("LW inside   RW inside") &&
                shouldersAfter.Contains("LD (draft) inside   RD (final) inside   C inside   LW inside   RW inside"),
                $"Expected both wings inside before the handoff and all five skaters inside during contain. {shouldersBefore} / {shouldersAfter}");
            Require(weakWingBefore.y is >= 397f and <= 451f && shift.BlueRightWing.y is >= 397f and <= 451f,
                $"The weak-side wing should tuck toward the high-slot center. {before} {Snapshot("after")}");
            Debug.Log(before);
            Debug.Log(Snapshot("after"));
            Debug.Log($"BOX_SHOULDERS {preset} before={shouldersBefore} / after={shouldersAfter}");
            float shoulderAfter = CenterShoulderDepth(shift, net);
            AdvanceUntil(shift, () => shift.Stage == PracticeShift.Phase.Pass, 14f, CloseFromInside);
            Debug.Log(FormattableString.Invariant(
                $"CENTER_SUPPORT {preset} seed={shift.Seed} before={shoulderBefore:0.0} after={shoulderAfter:0.0} minimum={minimumShoulder:0.0} release={shift.Time:0.00}"));
            Require(shift.HoldRead == PracticeShift.ReadResult.Met && shift.ContainRead == PracticeShift.ReadResult.Met,
                "The center rotation must preserve a valid Hold and Contain.");
        }

        private static float CenterShoulderDepth(PracticeShift shift, Vector2 net) =>
            Vector2.Dot(shift.BlueCenter - shift.RedSupport, (net - shift.RedSupport).normalized);

        private static void CheckResponsiveSkating(DraftShiftSettings settings)
        {
            var shift = Start(0, settings);
            var retry = Start(0, settings);
            var differentSeed = Start(4, settings);
            var differentDefense = Start(0, settings);
            var mirrorSettings = settings.Copy();
            mirrorSettings.Mirror = true;
            var mirror = Start(0, mirrorSettings);
            differentDefense.SkateTo(settings.CornerCarry ? new Vector2(220, 326) : new Vector2(245, 338));
            float upperPointTravel = 0;
            float lowerPointTravel = 0;
            float redResponse = 0;
            float blueResponse = 0;
            float seedVariation = 0;
            for (int i = 0; i < 125; i++)
            {
                shift.Step(0.02f);
                retry.Step(0.02f);
                differentSeed.Step(0.02f);
                differentDefense.Step(0.02f);
                mirror.Step(0.02f);
                upperPointTravel = Mathf.Max(upperPointTravel, Vector2.Distance(shift.RedRightDefense, settings.RedRightDefense));
                lowerPointTravel = Mathf.Max(lowerPointTravel, Vector2.Distance(shift.RedLeftDefense, settings.RedLeftDefense));
                redResponse = Mathf.Max(redResponse, Vector2.Distance(shift.RedSupport, differentDefense.RedSupport));
                blueResponse = Mathf.Max(blueResponse, Vector2.Distance(shift.BlueCenter, differentDefense.BlueCenter));
                seedVariation = Mathf.Max(seedVariation, Vector2.Distance(shift.RedSupport, differentSeed.RedSupport));
            }
            Require(Vector2.Distance(shift.BlueCenter, settings.BlueCenter) > 15f &&
                Vector2.Distance(shift.BlueLeftWing, settings.BlueLeftWing) > 15f &&
                Vector2.Distance(shift.BlueRightWing, settings.BlueRightWing) > 8f, "The supporting Blue skaters must rotate.");
            Require(Vector2.Distance(shift.RedSupport, settings.RedSupport) > 15f &&
                lowerPointTravel > 10f && upperPointTravel > 10f,
                $"Red must move to create outlets. Support {Vector2.Distance(shift.RedSupport, settings.RedSupport):0.0}, points {lowerPointTravel:0.0}/{upperPointTravel:0.0}.");
            Require(redResponse > 5f && blueResponse > 5f,
                $"Changing LD's position should move a Red outlet and Blue's low support. Red {redResponse:0.0}, Blue {blueResponse:0.0}.");
            Require(shift.RedSupport == retry.RedSupport && shift.BlueCenter == retry.BlueCenter &&
                shift.Puck == retry.Puck, "Same-seed retries must reproduce skating and puck state.");
            Require(seedVariation > 8f, $"New seeds should vary the attacking route. Support changed by {seedVariation:0.0}.");
            Require(Vector2.Distance(shift.RedSupport, mirrorSettings.Map(mirror.RedSupport)) < 0.01f &&
                Vector2.Distance(shift.BlueCenter, mirrorSettings.Map(mirror.BlueCenter)) < 0.01f,
                "Responsive movement must reflect correctly on the mirrored rink.");
            shift.TogglePlay();
            Vector2 before = shift.RedSupport;
            shift.Step(0.25f);
            Require(shift.RedSupport == before, "Team skating must stop when the shift is paused.");
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
            Require(condition(), $"Condition never reached at {shift.Time:0.00}s in {shift.Stage}. {shift.ScoreSummary}. {shift.Message}");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
