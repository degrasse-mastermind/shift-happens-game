using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ShiftHappens.VisualProof
{
    public sealed class StylizedProProof : MonoBehaviour
    {
        private const float StepSeconds = .02f;
        private const float ContainPreviewSeconds = .7f;
        private const float PassFollowThroughSeconds = .22f;
        private const float ReplaySpeed = .7f;
        private const float HudFraction = .18f;
        private const float SkaterScale = 1.55f;
        private const float GoalieScale = 1.48f;
        private const float SkaterLabelHeight = 5.5f * SkaterScale + 1.4f;
        [SerializeField] private bool retroPolygons;
        [SerializeField] private GameObject blueDefensemanModel;
        private static readonly Vector2[] LabelOffsets =
        {
            Vector2.zero, new(-31, 0), new(31, 0), new(0, -21), new(-31, -21), new(31, -21)
        };
        private static readonly Vector3 CreaseFocus = new(0, 1.4f, 19f);
        private static readonly Vector3[] CreaseFrame =
        {
            new(-34, 0, -7), new(34, 0, -7), new(-30, 0, 38), new(30, 0, 38),
            new(-22, 9, 36), new(22, 9, 36)
        };
        private static readonly Vector3 CameraFocus = new(0, 1.2f, -8.5f);
        private static readonly Vector3 CameraForward = new Vector3(0, -.59f, .81f).normalized;
        private static readonly Quaternion CarrierStickOffset = Quaternion.Euler(0, -28, 0);
        private static readonly Color Background = new(.027f, .052f, .098f);

        private Camera rinkCamera;
        private List<Mesh> defensemanPreviewMeshes;
        private Texture2D activeRingOverlay, passOutlineOverlay;
        private StylizedProPlayer ld, goalie;
        private Transform puck, outline, ring;
        private StylizedProPlayer rd, carrier, threat, blueCenter, blueLw, blueRw, redSupport, redLwPoint, redRwPoint;
        private StylizedProPlayer[] players;
        private PracticeShift shift;
        private bool marked;
        private bool frozen;
        private float holdUntil;
        private float pending;
        private float? containStart;
        private bool containCheckpointSeen;
        private float? passReleaseAt;
        private Vector3 passReleasePosition, passDirection;

        private float viewTop, viewHeight, hudHeight;
        private Vector3[] rinkBounds;
        private Vector2Int fittedResolution;
        private GUIStyle heading, caption, chip, button;
        private readonly Rect[] nameplates = new Rect[6];
        private int nameplateCount;

        private string Preset => retroPolygons ? "Crowded handoff" : marked ? "Marked upper corner" : "Original handoff";
        private string DisplayPreset => marked && !retroPolygons ? "Marked right corner" : Preset;
        private bool PassOutlineVisible => !retroPolygons && shift.Stage == PracticeShift.Phase.Pass;

        private void Awake()
        {
            BuildLighting();
            var cameraObject = new GameObject("Elevated zone camera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            rinkCamera = cameraObject.GetComponent<Camera>();
            rinkCamera.fieldOfView = 36;
            rinkCamera.clearFlags = CameraClearFlags.SolidColor;
            rinkCamera.backgroundColor = Background;
            rinkCamera.nearClipPlane = .3f;
            rinkCamera.farClipPlane = 500f;
            var cameraRendering = rinkCamera.GetUniversalAdditionalCameraData();
            cameraRendering.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraRendering.antialiasingQuality = AntialiasingQuality.High;
            rinkCamera.transform.rotation = Quaternion.LookRotation(CameraForward);

            var rink = new StylizedProRink(transform, retroPolygons);
            rinkBounds = rink.Build();
            ld = AddSkater(rink, "Blue LD");
            if (!retroPolygons && blueDefensemanModel != null)
                defensemanPreviewMeshes = rink.ReplaceDefensemanVisual(ld.Root, blueDefensemanModel);
            goalie = rink.MakePlayer("Blue goalie", goalie: true);
            goalie.Root.localScale = Vector3.one * GoalieScale;
            puck = rink.MakePuck();
            outline = rink.PuckOutline();
            ring = rink.LdRing();
            if (retroPolygons)
            {
                ring.gameObject.SetActive(false);
                activeRingOverlay = CreateRingOverlay("Retro LD contrast ring", new(.43f, 1f, .96f), new(.035f, .13f, .22f));
            }
            else
                passOutlineOverlay = CreateRingOverlay("Puck in flight", Color.white, new(.04f, .08f, .15f));
            rd = AddSkater(rink, "Blue RD");
            blueCenter = AddSkater(rink, "Blue C");
            blueLw = AddSkater(rink, "Blue LW");
            blueRw = AddSkater(rink, "Blue RW");
            carrier = AddSkater(rink, "Red puck carrier", redTeam: true, tracksPuck: !retroPolygons);
            threat = AddSkater(rink, "Red net-front threat", redTeam: true);
            redSupport = AddSkater(rink, "Red support", redTeam: true);
            redLwPoint = AddSkater(rink, "Red left defense", redTeam: true);
            redRwPoint = AddSkater(rink, "Red right defense", redTeam: true);
            players = new[] { ld, rd, blueCenter, blueLw, blueRw, goalie, carrier, threat, redSupport, redLwPoint, redRwPoint };
            if (retroPolygons) RetroPolygonMeshes.Apply(players);
            StartReplay(false);
        }

        private static StylizedProPlayer AddSkater(StylizedProRink rink, string name, bool redTeam = false, bool tracksPuck = false)
        {
            var skater = rink.MakePlayer(name, redTeam: redTeam, tracksPuck: tracksPuck);
            skater.Root.localScale = Vector3.one * SkaterScale;
            return skater;
        }

        public void StartReplay(bool markedPreset)
        {
            marked = markedPreset;
            var settings = marked ? DraftShiftSettings.MarkedPreset() : new DraftShiftSettings();
            if (retroPolygons)
            {
                settings.PartnerStart = new Vector2(145, 459);
                settings.BlueCenter = new Vector2(184, 381);
            }
            shift = new PracticeShift(marked ? 0 : 1, settings, guided: false);
            frozen = false;
            containStart = null;
            containCheckpointSeen = false;
            passReleaseAt = null;
            pending = 0;
            holdUntil = Time.unscaledTime + 2;
            shift.TogglePlay();
            foreach (var player in players) player.Reset();
            PosePlayers(0);
            DrawFrame();
        }

        private void Update()
        {
            FitCamera();
            if (frozen || shift.Stage == PracticeShift.Phase.Finished || Time.unscaledTime < holdUntil) return;
            pending += Mathf.Min(Time.unscaledDeltaTime, .1f) * ReplaySpeed;
            while (pending >= StepSeconds && shift.CanSkate)
            {
                pending -= StepSeconds;
                if (shift.ContainmentAvailable)
                {
                    Vector2 approach = retroPolygons
                        ? Vector2.Lerp(shift.Carrier, shift.NetFrontAttacker, .4f) + new Vector2(-12, 0)
                        : shift.Carrier + new Vector2(-12, 60);
                    shift.SkateTo(approach);
                }
                var before = shift.Stage;
                shift.Step(StepSeconds);
                bool released = !retroPolygons && before != PracticeShift.Phase.Pass && shift.Stage == PracticeShift.Phase.Pass;
                if (released)
                {
                    passReleaseAt = shift.Time;
                    passReleasePosition = World(shift.Puck, .13f);
                    passDirection = (World(shift.Destination, .13f) - passReleasePosition).normalized;
                }
                PosePlayers(StepSeconds);

                if (released)
                {
                    LogFrame("release");
                    pending = 0;
                    break;
                }
                if (!retroPolygons && before == PracticeShift.Phase.Pass && shift.Stage != before)
                    LogFrame("pass-end");
                if (!retroPolygons && shift.Stage == PracticeShift.Phase.Finished)
                {
                    LogFrame("finish");
                    Debug.Log($"STYLIZED_PRO_RESULT {Preset} time={shift.Time:0.00} {shift.Message} {shift.ScoreSummary}");
                    break;
                }
                if (shift.HandoffAt == shift.Time && shift.ContainmentAvailable)
                {
                    holdUntil = Time.unscaledTime + (retroPolygons ? 1.25f : 4f);
                    pending = 0;
                    LogFrame("handoff");
                    break;
                }
                if (retroPolygons && shift.HandoffAt is float handoff && shift.Time >= handoff + 2.2f)
                {
                    frozen = true;
                    LogFrame("close");
                    break;
                }
                if (!shift.Containing)
                {
                    containStart = null;
                    continue;
                }
                containStart ??= shift.Time;
                if (retroPolygons || containCheckpointSeen || shift.Time < containStart.Value + ContainPreviewSeconds) continue;
                containCheckpointSeen = true;
                holdUntil = Time.unscaledTime + 2;
                pending = 0;
                LogFrame("contain");
                break;
            }
            DrawFrame();
        }

        private void LogFrame(string checkpoint) =>
            Debug.Log(FormattableString.Invariant(
                $"STYLIZED_PRO_REPLAY {checkpoint} {Preset} time={shift.Time:0.00} LD={shift.Player} RD={shift.Partner} carrier={shift.Carrier} slotCovered={shift.SlotLaneCovered} rdCovering={shift.PartnerHasCoverage} containing={shift.Containing}"));

        private void DrawFrame()
        {
            puck.position = World(shift.Puck, .13f);
            outline.position = World(shift.Puck, .045f);
            outline.gameObject.SetActive(shift.PuckPossession == PracticeShift.Possession.RedCarrier ||
                (retroPolygons && shift.Stage == PracticeShift.Phase.Pass));
            ring.position = World(shift.Player, .06f);
            if (shift.PuckPossession == PracticeShift.Possession.RedCarrier)
                carrier.HoldPuckAtBlade(puck.position, puck.lossyScale.x / 2);
            else if (PassFollowThroughPoint() is Vector3 followThrough)
                carrier.HoldPuckAtBlade(followThrough, puck.lossyScale.x / 2);
        }

        private Vector3? PassFollowThroughPoint()
        {
            if (!passReleaseAt.HasValue) return null;
            float elapsed = shift.Time - passReleaseAt.Value;
            return elapsed <= PassFollowThroughSeconds
                ? passReleasePosition + passDirection * (elapsed / PassFollowThroughSeconds * 1.1f)
                : null;
        }

        private void PosePlayers(float dt)
        {
            Vector3 puckPosition = World(shift.Puck);
            Vector3 carrierPosition = World(shift.Carrier);
            PosePlayer(ld, World(shift.Player), carrierPosition, dt);
            PosePlayer(goalie, World(shift.Goalie), puckPosition, dt, keepGoalieOpen: true);
            PosePlayer(rd, World(shift.Partner), World(shift.Partner + shift.PartnerFacing * 10), dt);
            PosePlayer(blueCenter, World(shift.BlueCenter), puckPosition, dt);
            PosePlayer(blueLw, World(shift.BlueLeftWing), puckPosition, dt);
            PosePlayer(blueRw, World(shift.BlueRightWing), puckPosition, dt);
            // The forehand blade sits to the right of the model's forward axis.
            Vector3 carrierTarget = carrierPosition + CarrierStickOffset *
                ((PassFollowThroughPoint() ?? puckPosition) - carrierPosition);
            PosePlayer(carrier, carrierPosition, carrierTarget, dt);
            PosePlayer(threat, World(shift.NetFrontAttacker), puckPosition, dt);
            PosePlayer(redSupport, World(shift.RedSupport), puckPosition, dt);
            PosePlayer(redLwPoint, World(shift.RedLeftDefense), puckPosition, dt);
            PosePlayer(redRwPoint, World(shift.RedRightDefense), puckPosition, dt);
        }

        private Vector3 World(Vector2 simulation, float height = 0)
        {
            Vector2 feet = DefensiveZoneView.Project(simulation, false) / DefensiveZoneView.UnitsPerFoot;
            float across = feet.y - 2.5f;
            // Reflect the full formation and its motion across the middle of the zone.
            if (!retroPolygons) across = 85 - across;
            return StylizedProRink.At(feet.x - 3, across, height);
        }

        private void PosePlayer(StylizedProPlayer model, Vector3 position, Vector3 target, float dt, bool keepGoalieOpen = false)
        {
            Transform player = model.Root;
            Vector3 displacement = dt > 0 ? position - player.position : Vector3.zero;
            player.position = position;
            Vector3 direction = target - position;
            direction.y = 0;
            if (direction.sqrMagnitude > .001f)
            {
                Quaternion facing = Quaternion.LookRotation(direction);
                player.rotation = keepGoalieOpen
                    ? Quaternion.RotateTowards(Quaternion.LookRotation(Vector3.back), facing, 40)
                    : facing;
            }
            model.Pose(displacement, dt, shift.Time);
        }

        private void FitCamera()
        {
            var resolution = new Vector2Int(Screen.width, Screen.height);
            if (fittedResolution == resolution) return;
            fittedResolution = resolution;
            float scale = UiScale;
            hudHeight = Mathf.Max(96 * scale, Screen.height * HudFraction);
            if (Screen.width < Screen.height) hudHeight = 99 * scale;
            viewHeight = Mathf.Min(Mathf.Max(1, Screen.height - hudHeight), Screen.width * .92f);
            viewTop = Mathf.Max(0, (Screen.height - viewHeight - hudHeight) * .5f);
            rinkCamera.rect = new Rect(0, 1 - (viewTop + viewHeight) / Screen.height, 1, viewHeight / Screen.height);
            float aspect = Mathf.Max(.1f, Screen.width / viewHeight);
            rinkCamera.aspect = aspect;
            Vector3 focus = retroPolygons ? CreaseFocus : CameraFocus;
            Vector3[] frame = retroPolygons ? CreaseFrame : rinkBounds;
            float vertical = Mathf.Tan(rinkCamera.fieldOfView * .5f * Mathf.Deg2Rad) * .965f;
            float horizontal = vertical * aspect;
            float distance = 1;
            Vector3 cameraUp = rinkCamera.transform.up;
            foreach (Vector3 point in frame)
            {
                Vector3 relative = point - focus;
                float depth = Vector3.Dot(relative, CameraForward);
                float widthFit = Mathf.Abs(relative.x) / horizontal - depth;
                float heightFit = Mathf.Abs(Vector3.Dot(relative, cameraUp)) / vertical - depth;
                distance = Mathf.Max(distance, Mathf.Max(widthFit, heightFit));
            }
            rinkCamera.transform.position = focus - CameraForward * distance;
        }

        private static float UiScale => Mathf.Clamp(Screen.width / 1000f, .75f, 1.6f);

        private void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.68f, .75f, .87f);
            RenderSettings.ambientEquatorColor = new Color(.43f, .49f, .61f);
            RenderSettings.ambientGroundColor = new Color(.32f, .37f, .49f);
            var key = new GameObject("Arena key light", typeof(Light)).GetComponent<Light>();
            key.transform.SetParent(transform, false);
            key.type = LightType.Directional;
            key.color = new Color(.94f, .97f, 1f);
            key.intensity = .88f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = .43f;
            key.transform.rotation = Quaternion.Euler(53, -32, 0);
            RenderSettings.sun = key;
            var fill = new GameObject("Arena fill", typeof(Light)).GetComponent<Light>();
            fill.transform.SetParent(transform, false);
            fill.type = LightType.Directional;
            fill.intensity = .24f;
            fill.transform.rotation = Quaternion.Euler(37, 142, 0);
        }

        private void OnGUI()
        {
            if (shift == null) return;
            float scale = UiScale;
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                caption = new GUIStyle(GUI.skin.label) { wordWrap = true };
                chip = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            }
            heading.fontSize = Mathf.RoundToInt(20 * scale);
            caption.fontSize = Mathf.RoundToInt(16 * scale);
            chip.fontSize = Mathf.RoundToInt(13 * scale);
            button.fontSize = Screen.width < 480 ? 10 : Mathf.RoundToInt(15 * scale);

            if (retroPolygons && Event.current.type == EventType.Repaint) DrawActiveRingOverlay(scale);
            nameplateCount = 0;
            Label(ld.Root, "LD", SkaterLabelHeight, scale, horizontalOffset: retroPolygons ? -27 : 27);
            Label(goalie.Root, "G", 5.5f * GoalieScale + 1.4f, scale);
            Label(rd.Root, "RD", SkaterLabelHeight, scale);
            Label(blueCenter.Root, "C", SkaterLabelHeight, scale);
            Label(blueLw.Root, "LW", SkaterLabelHeight, scale);
            Label(blueRw.Root, "RW", SkaterLabelHeight, scale);
            if (PassOutlineVisible && Event.current.type == EventType.Repaint)
                DrawPassOutlineOverlay(scale);

            float margin = 14 * scale;
            float top = viewTop + viewHeight;
            Color old = GUI.color;
            GUI.color = Background;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, viewTop), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, top, Screen.width, Screen.height - top), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(margin, top + 3 * scale, Screen.width - 2 * margin, 27 * scale),
                $"{(retroPolygons ? "Retro Polygons" : "Stylized Pro")}   ·   {DisplayPreset}   ·   {shift.Time:0.00}s", heading);
            string cue = retroPolygons && frozen ? "LD has moved out to contain. RD covers net front. Select Replay to watch again."
                : !retroPolygons && shift.Stage != PracticeShift.Phase.Carry ? shift.Message
                : containCheckpointSeen && Time.unscaledTime < holdUntil ? "LD contains from inside. Replay resumes for the pass."
                : shift.HandoffAt.HasValue && Time.unscaledTime < holdUntil ? "RD takes coverage. Replay resumes automatically."
                : shift.HandoffAt.HasValue ? "LD closes from inside while RD covers net front."
                : marked ? "LD holds the passing lane to net front."
                : "LD holds net front while RD returns.";
            GUI.Label(new Rect(margin, top + 30 * scale, Screen.width - 2 * margin, 40 * scale), cue, caption);
            if (retroPolygons)
            {
                if (GUI.Button(new Rect(margin, top + hudHeight - 37 * scale, 120 * scale, 29 * scale), "Replay", button))
                    StartReplay(false);
                return;
            }
            float buttonWidth = (Screen.width - 4 * margin) / 3;
            float buttonTop = top + hudHeight - 37 * scale;
            if (GUI.Button(new Rect(margin, buttonTop, buttonWidth, 29 * scale), "Original handoff", button)) StartReplay(false);
            if (GUI.Button(new Rect(2 * margin + buttonWidth, buttonTop, buttonWidth, 29 * scale), "Marked right corner", button)) StartReplay(true);
            if (GUI.Button(new Rect(3 * margin + 2 * buttonWidth, buttonTop, buttonWidth, 29 * scale), "Replay", button)) StartReplay(marked);
        }

        private void DrawActiveRingOverlay(float scale)
        {
            Vector3 center = rinkCamera.WorldToScreenPoint(ld.Root.position + Vector3.up * .07f);
            if (center.z <= 0) return;
            float width = Mathf.Max(40 * scale, 2 * Mathf.Abs(
                rinkCamera.WorldToScreenPoint(ld.Root.position + Vector3.right * 3.3f).x - center.x));
            float height = width * .48f;
            GUI.DrawTexture(new Rect(center.x - width / 2, Screen.height - center.y - height / 2,
                width, height), activeRingOverlay);
        }

        private void DrawPassOutlineOverlay(float scale)
        {
            Vector3 center = rinkCamera.WorldToScreenPoint(puck.position);
            if (center.z <= 0) return;
            Vector3 right = rinkCamera.WorldToScreenPoint(puck.position + Vector3.right);
            float width = Mathf.Max(13 * scale, 2 * Mathf.Abs(right.x - center.x));
            float height = width * .64f;
            GUI.DrawTexture(new Rect(center.x - width / 2, Screen.height - center.y - height / 2,
                width, height), passOutlineOverlay);
        }

        private static Texture2D CreateRingOverlay(string name, Color highlight, Color edge)
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2(x + .5f - size / 2f, y + .5f - size / 2f).magnitude;
                if (radius >= 35 && radius <= 46)
                    pixels[y * size + x] = radius >= 38 && radius <= 43 ? highlight : edge;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (defensemanPreviewMeshes != null)
                foreach (var mesh in defensemanPreviewMeshes) Destroy(mesh);
            if (activeRingOverlay != null) Destroy(activeRingOverlay);
            if (passOutlineOverlay != null) Destroy(passOutlineOverlay);
        }

        private void Label(Transform player, string name, float height, float scale, float horizontalOffset = 0)
        {
            Vector3 screen = rinkCamera.WorldToScreenPoint(player.position + Vector3.up * height);
            var rect = new Rect(screen.x + (horizontalOffset - 14) * scale, Screen.height - screen.y - 11 * scale,
                28 * scale, 17 * scale);
            foreach (Vector2 offset in LabelOffsets)
            {
                var candidate = new Rect(rect.position + offset * scale, rect.size);
                bool available = true;
                for (int i = 0; i < nameplateCount; i++)
                    available &= !candidate.Overlaps(nameplates[i]);
                if (!available) continue;
                rect = candidate;
                break;
            }
            nameplates[nameplateCount++] = rect;
            Color old = GUI.color;
            GUI.color = new Color(.06f, .16f, .28f, .94f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(rect, name, chip);
        }
    }
}







