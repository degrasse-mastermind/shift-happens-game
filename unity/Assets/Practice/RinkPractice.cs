using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ShiftHappens
{
    public sealed class RinkPractice : MonoBehaviour
    {
        private static readonly Color Navy = new(0.06f, 0.12f, 0.21f);
        private static readonly Color Blue = new(0.07f, 0.35f, 0.54f);
        private static readonly Color Amber = new(1f, 0.81f, 0.37f);

        private PracticeShift shift;
        [SerializeField] private DraftPracticePreset[] presets =
        {
            new() { Name = "Original handoff", Settings = new DraftShiftSettings() },
            new(),
            new() { Name = "Behind net", Settings = DraftShiftSettings.BehindNetPreset() },
            new() { Name = "Behind net · delayed exit", Settings = DraftShiftSettings.BehindNetPreset(2) },
        };
        private int presetIndex;
        private DraftShiftSettings draftCoaching => presets[presetIndex].Settings;
        internal bool IsMirrored => shift.Mirrored;
        internal Texture2D PuckTexture => puckTexture;
        private bool guided = true;
        private Font font;
        private RectTransform rink;
        private RawImage rinkImage;
        private Texture2D puckTexture;
        private RectTransform player, partner, carrier, puck, puckLocator, goalie, tieMarker, passLine;
        private RectTransform blueCenter, blueLeftWing, blueRightWing;
        private RectTransform redSupport, redLeftDefense, redRightDefense, redSlot, redGoalie;
        private Text header, status, playLabel, warning, coverage, modeLabel, presetLabel;
        private RectTransform activeRing;
        private RectTransform[] skaters, blueSkaters, blueNameplates;
        private readonly Rect[] labelBounds = new Rect[6];
        private static readonly Vector2[] LabelOffsets =
        {
            new(0,-23), new(-18,-23), new(18,-23), new(-35,-20), new(35,-20),
            new(0,23), new(-22,23), new(22,23), new(0,-44), new(0,44),
            new(-42,-36), new(42,-36), new(-42,36), new(42,36),
        };
        private Button tieButton;
        private Pointer activePointer;
        private ReviewMode review;
        private CanvasGroup practiceControls;
        private CanvasScaler scaler;
        private RectTransform root;
        private bool? portraitLayout;
        private readonly System.Collections.Generic.List<(RectTransform node, Rect desktop, Rect portrait)> layout = new();

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildDisplay();
            ApplyLayout();
            SelectSeed(1);
            review = gameObject.AddComponent<ReviewMode>();
            review.Initialize(this);
        }

        private void Update()
        {
            ApplyLayout();
            if (review != null && review.Active) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame) shift.TogglePlay();
                if (keyboard.digit4Key.wasPressedThisFrame) shift.TieUp();
            }

            if (shift.CanSkate)
            {
                if (keyboard != null)
                {
                    Vector2 direction = new(
                        (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                        (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0),
                        (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0) -
                        (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0));
                    if (direction != Vector2.zero)
                    {
                        shift.SkateTo(shift.Player + direction.normalized * 90);
                        activePointer = null;
                        usingKeys = true;
                    }
                    else if (usingKeys)
                    {
                        shift.SkateTo(shift.Player);
                        usingKeys = false;
                    }
                }
                ReadPointer();
            }
            else activePointer = null;

            shift.Step(Mathf.Min(Time.unscaledDeltaTime, 0.25f));
            DrawShift();
        }

        public void SetReviewGate(bool active)
        {
            shift.SetReviewing(active);
            practiceControls.interactable = !active;
            practiceControls.blocksRaycasts = !active;
            activePointer = null;
        }

        public ReviewNote CaptureReview(bool starting)
        {
            var state = starting ? new PracticeShift(shift.Seed, draftCoaching, guided) : shift;
            Vector2 RedForwardPosition(DraftShiftSettings.RedForward role) =>
                role == draftCoaching.CarrierRole ? state.Carrier :
                role == draftCoaching.NetFrontRole ? state.NetFrontAttacker : state.RedSupport;
            return new ReviewNote
            {
                scope = starting ? "Starting setup" : "This moment",
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " / " + presets[presetIndex].Name,
                buildVersion = Application.version + " / ReviewMode-v1",
                unityVersion = Application.unityVersion,
                mode = guided ? "Guided" : "Freer",
                seed = shift.Seed,
                gameTime = shift.Time,
                phase = shift.Stage.ToString(),
                possession = state.PuckPossession.ToString(),
                coachingReview = state.ScoreSummary,
                positioningReview = state.PositioningSummary,
                message = state.Message,
                gameWasPaused = shift.Paused,
                teachingWasPaused = shift.TeachingPaused,
                settingsJson = JsonUtility.ToJson(draftCoaching),
                orientation = draftCoaching.Mirror ? "Mirrored: Blue defends the right net." : "Default: Blue defends the left net.",
                entities = new System.Collections.Generic.List<ReviewEntity>
                {
                    new("blue.ld", "Y/LD", "blue", state.Player),
                    new("blue.rd", "RD", "blue", state.Partner),
                    new("blue.c", "C", "blue", state.BlueCenter),
                    new("blue.lw", "LW", "blue", state.BlueLeftWing),
                    new("blue.rw", "RW", "blue", state.BlueRightWing),
                    new("blue.g", "G", "blue", state.Goalie),
                    new("red.lw", "LW", "red", RedForwardPosition(DraftShiftSettings.RedForward.LW)),
                    new("red.ld", "LD", "red", state.RedLeftDefense),
                    new("red.rd", "RD", "red", state.RedRightDefense),
                    new("red.c", "C", "red", RedForwardPosition(DraftShiftSettings.RedForward.C)),
                    new("red.rw", "RW", "red", RedForwardPosition(DraftShiftSettings.RedForward.RW)),
                    new("red.g", "G", "red", state.RedGoalie),
                    new("puck", "", "puck", state.Puck)
                }
            };
        }
        private bool usingKeys;

        private void ReadPointer()
        {
            Pointer pointer = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed
                ? Touchscreen.current : Mouse.current;
            if (pointer == null || !pointer.press.isPressed)
            {
                activePointer = null;
                return;
            }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rink, pointer.position.ReadValue(), null, out Vector2 local)) return;
            if (pointer.press.wasPressedThisFrame && rink.rect.Contains(local)) activePointer = pointer;
            if (activePointer == pointer)
                shift.SkateTo(DefensiveZoneView.Unproject(new Vector2(local.x, -local.y), shift.Mirrored));
        }

        private void SelectSeed(int seed)
        {
            shift = new PracticeShift(seed, draftCoaching, guided);
            presetLabel.text = $"Setup ▸ {presets[presetIndex].Name}";
            usingKeys = false;
            activePointer = null;
            DrawShift();
        }

#if UNITY_EDITOR
        public bool ShowEditorReplay(PracticeShift replay, string setup, string focus, System.Action restart)
        {
            if (review.Active) return false;
            enabled = false;
            shift = replay;
            presetLabel.text = setup;
            var replayButton = playLabel.transform.parent.GetComponent<Button>();
            foreach (var button in practiceControls.GetComponentsInChildren<Button>(true))
                button.interactable = button == replayButton;
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() => restart());
            var highlight = new Color(0f, 0.40f, 0.39f, .97f);
            blueNameplates[2].GetComponent<UnityEngine.UI.Image>().color = focus == "C" ? highlight : NameplateBlue;
            blueNameplates[3].GetComponent<UnityEngine.UI.Image>().color = focus == "LW" ? highlight : NameplateBlue;
            blueNameplates[4].GetComponent<UnityEngine.UI.Image>().color = focus == "RW" ? highlight : NameplateBlue;
            return true;
        }

        public void DrawEditorReplay(string title, string caption, string result)
        {
            DrawShift();
            header.text = $"LIVE TEST · {title}    {shift.Time:0.00}s";
            status.text = caption;
            coverage.text = result;
            playLabel.text = "Replay";
            tieButton.interactable = false;
            if (shift.Stage == PracticeShift.Phase.Pass)
                DrawPassLane(shift.Destination);
        }
#endif

        private void DrawShift()
        {
            ApplyLayout();
            rinkImage.uvRect = DefensiveZoneView.TextureRegion(shift.Mirrored);
            Place(player, shift.Player);
            Place(partner, shift.Partner);
            Place(carrier, shift.Carrier);
            Place(puck, shift.Puck);
            Place(puckLocator, shift.Puck);
            puckLocator.gameObject.SetActive(shift.PuckPossession == PracticeShift.Possession.RedCarrier ||
                shift.Stage == PracticeShift.Phase.Pass);
            Place(goalie, shift.Goalie);
            Place(blueCenter, shift.BlueCenter);
            Place(blueLeftWing, shift.BlueLeftWing);
            Place(blueRightWing, shift.BlueRightWing);
            Place(redSupport, shift.RedSupport);
            Place(redLeftDefense, shift.RedLeftDefense);
            Place(redRightDefense, shift.RedRightDefense);
            Place(redSlot, shift.NetFrontAttacker);
            Place(redGoalie, shift.RedGoalie);
            Face(player, shift.Puck - shift.Player);
            Face(partner, shift.PartnerFacing);
            Face(carrier, shift.Puck - shift.Carrier);
            Face(blueCenter, shift.Puck - shift.BlueCenter);
            Face(blueLeftWing, shift.Puck - shift.BlueLeftWing);
            Face(blueRightWing, shift.Puck - shift.BlueRightWing);
            Face(redSupport, shift.Puck - shift.RedSupport);
            Face(redLeftDefense, shift.Puck - shift.RedLeftDefense);
            Face(redRightDefense, shift.Puck - shift.RedRightDefense);
            Face(redSlot, shift.Puck - shift.NetFrontAttacker);
            Face(goalie, shift.Puck - shift.Goalie);
            Face(redGoalie, shift.Puck - shift.RedGoalie);
            Place(tieMarker, shift.NetFrontAttacker);
            Place(activeRing, shift.Player);
            PlaceBlueNameplates();

            bool warningActive = shift.Stage == PracticeShift.Phase.Warning;
            warning.transform.parent.gameObject.SetActive(warningActive);
            passLine.gameObject.SetActive(warningActive);
            tieMarker.gameObject.SetActive(warningActive || shift.Tied);
            tieMarker.GetComponent<RawImage>().color = shift.Tied ? Blue : Amber;
            if (warningActive) DrawPassLane(shift.NetFrontAttacker);

            header.text = $"{(shift.BehindNetScenario ? "DRAFT COACHING" : "CUES & SCORING FINAL")}     SEED {shift.Seed}  ·  {shift.Time:0.0}s";
            status.text = shift.Paused ? "Paused" : shift.Message;
            coverage.text = shift.Stage == PracticeShift.Phase.Finished ? shift.ScoreSummary
                : shift.BehindNetScenario ? BehindNetCoverage()
                : guided || shift.Stage == PracticeShift.Phase.Ready
                    ? shift.ContainmentAvailable ? "RD covers net front  |  LD may contain from inside"
                        : shift.PuckPossession == PracticeShift.Possession.RedCarrier
                            ? shift.PartnerArrivedWrongSide
                                ? $"RD is on the wrong side of Red {draftCoaching.NetFrontLabel}. Hold net front."
                                : "RD returning  |  LD protects the slot lane"
                            : "Track the puck through the play"
                    : "FREER  /  Hold and Contain scores after the shift";
            playLabel.text = shift.Stage == PracticeShift.Phase.Ready ? "Start"
                : shift.Stage == PracticeShift.Phase.Finished ? "Retry"
                : shift.TeachingPaused ? "Resume read"
                : shift.Paused ? "Resume" : "Pause";
            modeLabel.text = guided ? "Guided" : "Freer";
            tieButton.interactable = shift.CanSkate;
        }

        private string BehindNetCoverage()
        {
            if (shift.PuckPossession != PracticeShift.Possession.RedCarrier) return "Track the puck through the play";
            if (!shift.Guided && shift.Stage != PracticeShift.Phase.Ready) return "FREER  /  Draft scores after the shift";
            return shift.BehindNetExitAt.HasValue ? "RD takes the puck side  |  LD stays with Red C"
                : "LD holds inside  |  Watch Red C";
        }

        private void DrawPassLane(Vector2 destination)
        {
            Vector2 delta = IceToCanvas(destination) - IceToCanvas(shift.Puck);
            passLine.gameObject.SetActive(true);
            passLine.anchoredPosition = IceToCanvas(shift.Puck);
            passLine.sizeDelta = new Vector2(delta.magnitude, 5);
            passLine.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void BuildDisplay()
        {
            var canvasObject = new GameObject("Practice Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            practiceControls = canvasObject.AddComponent<CanvasGroup>();
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 860);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root = Rect("Practice board", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1024, 860));
            var backdrop = Rect("Game background", canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.SetAsFirstSibling();
            Panel(backdrop, Navy);
            header = Label("Header", root, new(22, -10), new(668, 42), 22, Color.white);
            Responsive(header.rectTransform, new(12, -4, 616, 38));
            var controls = Rect("Practice controls", root, new(0, 1), new(0, 1), new(704, -55), new(300, 735));
            Panel(controls, new Color(.09f, .17f, .27f));
            Responsive(controls, new(12, -698, 616, 438));
            presetLabel = Control("Setup", root, new(720, -75, 268, 44), () =>
            {
                presetIndex = (presetIndex + 1) % presets.Length;
                SelectSeed(shift.Seed);
            }, out _);
            presetLabel.fontSize = 17;
            Responsive(presetLabel, new(26, -815, 274, 66));
            Responsive(Control("Review / Export", root, new(720, -726, 268, 44), () => review.Begin(), out _),
                new(26, -1048, 274, 66));
            rink = Rect("Defensive zone", root, new(0, 1), new(0, 1), new(22, -55),
                new(DefensiveZoneView.Width, DefensiveZoneView.Height));
            Responsive(rink, new((640f - DefensiveZoneView.Width) / 2f, -42, DefensiveZoneView.Width, DefensiveZoneView.Height));
            rink.gameObject.AddComponent<RectMask2D>();
            rinkImage = rink.gameObject.AddComponent<RawImage>();
            rinkImage.texture = Resources.Load<Texture2D>("PracticeZoneIce");
            rinkImage.raycastTarget = false;

            tieMarker = Art("Net-front threat highlight", "PracticeActiveRing", 43);
            passLine = Rect("Pass lane", rink, new(0, 1), new(0, 0.5f), Vector2.zero, new(0, 5));
            Panel(passLine, Amber);
            activeRing = Art("Active player ring", "PracticeActiveRing", 58);
            var youthHockey = Resources.Load<Texture2D>("PracticeYouthHockey");
            partner = HockeyPlayer("Blue RD", youthHockey);
            blueCenter = HockeyPlayer("Blue C", youthHockey);
            blueLeftWing = HockeyPlayer("Blue LW", youthHockey);
            blueRightWing = HockeyPlayer("Blue RW", youthHockey);
            redSupport = HockeyPlayer("Red support", youthHockey, red: true);
            redLeftDefense = HockeyPlayer("Red point", youthHockey, red: true);
            redRightDefense = HockeyPlayer("Red point", youthHockey, red: true);
            redSlot = HockeyPlayer("Red slot threat", youthHockey, red: true);
            redGoalie = HockeyPlayer("Red goalie", youthHockey, red: true, goaltender: true);
            player = HockeyPlayer("Controlled LD", youthHockey);
            carrier = HockeyPlayer("Red carrier", youthHockey, red: true);
            goalie = HockeyPlayer("Blue goalie", youthHockey, goaltender: true);
            puckTexture = CreatePuckTexture();
            // Keep the 3:1 diameter-to-thickness ratio; enlarge the disc for tracking on screen.
            puckLocator = Art("Puck contrast outline", "PracticePuckOutline", 18f);
            puck = Art("Puck", puckTexture, Vector2.one * PuckSize);
            blueSkaters = new[] { player, partner, blueCenter, blueLeftWing, blueRightWing, goalie };
            skaters = new[] { player, partner, blueCenter, blueLeftWing, blueRightWing, goalie, redSupport,
                redLeftDefense, redRightDefense, redSlot, redGoalie, carrier, puckLocator };
            blueNameplates = new[] { Nameplate("LD"), Nameplate("RD"), Nameplate("C"), Nameplate("LW"),
                Nameplate("RW"), Nameplate("G") };

            var warningPanel = Rect("Pass warning panel", rink, new(0, 1), new(0, 1), new(12, -12), new(322, 45));
            Panel(warningPanel, Amber);
            warning = Label("Pass warning", warningPanel, Vector2.zero, warningPanel.sizeDelta, 24, Navy);
            warning.text = "PASS TOWARD THE SLOT";

            status = Label("Play by play", root, new(720, -134), new(268, 118), 22, Color.white);
            coverage = Label("Coaching cue and scoring", root, new(720, -255), new(268, 126), 22, Amber);
            Responsive(status.rectTransform, new(26, -704, 274, 100));
            Responsive(coverage.rectTransform, new(340, -704, 274, 100));
            playLabel = Control("Start", root, new(720, -408, 268, 60), () =>
            {
                if (shift.Stage == PracticeShift.Phase.Finished) SelectSeed(shift.Seed);
                shift.TogglePlay();
            }, out _);
            Responsive(playLabel, new(340, -815, 274, 66));
            var point = Control("Point look · 0", root, new(720, -484, 129, 46), () => SelectSeed(0), out _);
            var slot = Control("Slot look · 1", root, new(859, -484, 129, 46), () => SelectSeed(1), out _);
            point.fontSize = slot.fontSize = 17;
            Responsive(point, new(26, -893, 274, 66));
            Responsive(slot, new(340, -893, 274, 66));
            Responsive(Control("New opponent", root, new(720, -542, 268, 46), () => SelectSeed(Random.Range(0, int.MaxValue)), out _),
                new(340, -1048, 274, 66));
            modeLabel = Control("Guided", root, new(720, -600, 129, 46), () =>
            {
                guided = !guided;
                SelectSeed(shift.Seed);
            }, out _);
            Responsive(modeLabel, new(26, -971, 274, 66));
            Responsive(Control("Tie up · 4", root, new(859, -600, 129, 46), () => shift.TieUp(), out tieButton),
                new(340, -971, 274, 66));

            var input = new GameObject("Practice Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            input.transform.SetParent(transform, false);
        }

        private void Responsive(Text label, Rect portrait)
        {
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            Responsive((RectTransform)label.transform.parent, portrait);
        }

        private void Responsive(RectTransform node, Rect portrait) =>
            layout.Add((node, new Rect(node.anchoredPosition, node.sizeDelta), portrait));

        private void ApplyLayout()
        {
            bool portrait = Screen.height > Screen.width;
            if (portraitLayout == portrait) return;
            portraitLayout = portrait;
            root.sizeDelta = portrait ? new Vector2(640, 1148) : new Vector2(1024, 860);
            scaler.referenceResolution = root.sizeDelta;
            rink.localScale = Vector3.one * (portrait ? 1f : 1.13f);
            foreach (var item in layout)
            {
                Rect bounds = portrait ? item.portrait : item.desktop;
                item.node.anchoredPosition = bounds.position;
                item.node.sizeDelta = bounds.size;
            }
        }

        private static readonly Color NameplateBlue = new(.08f, .21f, .32f, .94f);
        private static readonly Vector2 NameplateSize = new(37, 22);

        // This box includes the stick, skates and padding. Youth proportions are in the artwork.
        internal const float PlayerSize = 6.1f * DefensiveZoneView.UnitsPerFoot;
        internal const float PuckSize = 5f * (3f / 12f) * DefensiveZoneView.UnitsPerFoot;

        internal static Rect PlayerTextureRegion(bool red, bool goaltender) =>
            new(red ? .5f : 0, goaltender ? 0 : .5f, .5f, .5f);

        private RectTransform HockeyPlayer(string name, Texture2D atlas, bool red = false, bool goaltender = false)
        {
            var node = Art(name, atlas, Vector2.one * PlayerSize);
            node.GetComponent<RawImage>().uvRect = PlayerTextureRegion(red, goaltender);
            return node;
        }

        private RectTransform Art(string name, string resource, float size) =>
            Art(name, Resources.Load<Texture2D>(resource), new Vector2(size, size));

        private RectTransform Art(string name, Texture2D texture, Vector2 size)
        {
            var node = Rect(name, rink, new(0, 1), new(.5f, .5f), Vector2.zero, size);
            var image = node.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return node;
        }

        private RectTransform Nameplate(string role)
        {
            var node = Rect("Blue nameplate " + role, rink, new(0, 1), new(.5f, .5f), Vector2.zero, NameplateSize);
            Panel(node, NameplateBlue);
            Label(role, node, Vector2.zero, NameplateSize, 18, Color.white).text = role;
            return node;
        }

        private void PlaceBlueNameplates()
        {
            for (int i = 0; i < blueSkaters.Length; i++)
            {
                Vector2 origin = blueSkaters[i].anchoredPosition;
                float bestCost = float.PositiveInfinity;
                Rect best = default;
                for (int slot = 0; slot < LabelOffsets.Length; slot++)
                {
                    Vector2 center = origin + LabelOffsets[slot];
                    center.x = Mathf.Clamp(center.x, NameplateSize.x / 2f, DefensiveZoneView.Width - NameplateSize.x / 2f);
                    center.y = Mathf.Clamp(center.y, -DefensiveZoneView.Height + NameplateSize.y / 2f, -NameplateSize.y / 2f);
                    var bounds = new Rect(center - NameplateSize * .5f, NameplateSize);
                    float cost = slot * 2f;
                    foreach (var skater in skaters)
                        if (bounds.Overlaps(new Rect(skater.anchoredPosition - skater.sizeDelta * .5f, skater.sizeDelta)))
                            cost += 40f;
                    for (int prior = 0; prior < i; prior++)
                        if (bounds.Overlaps(labelBounds[prior])) cost += 120f;
                    if (cost >= bestCost) continue;
                    best = bounds;
                    bestCost = cost;
                }
                labelBounds[i] = best;
                blueNameplates[i].anchoredPosition = best.center;
                blueNameplates[i].gameObject.SetActive(rink.rect.Contains(origin));
            }
        }

        private Text Control(string title, Transform parent, Rect bounds, UnityEngine.Events.UnityAction onClick, out Button button)
        {
            var node = Rect(title, parent, new(0, 1), new(0, 1), bounds.position, bounds.size);
            var image = Panel(node, Blue);
            image.raycastTarget = true;
            button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            var label = Label(title, node, Vector2.zero, node.sizeDelta, 21, Color.white);
            label.text = title;
            return label;
        }

        private Text Label(string name, Transform parent, Vector2 offset, Vector2 size, int fontSize, Color color)
        {
            var node = Rect(name, parent, new(0, 1), new(0, 1), offset, size);
            var label = node.gameObject.AddComponent<UnityEngine.UI.Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static UnityEngine.UI.Image Panel(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
        {
            var node = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            node.SetParent(parent, false);
            node.anchorMin = node.anchorMax = anchor;
            node.pivot = pivot;
            node.anchoredPosition = offset;
            node.sizeDelta = size;
            return node;
        }

        private Vector2 IceToCanvas(Vector2 point)
        {
            Vector2 view = DefensiveZoneView.Project(point, shift.Mirrored);
            return new Vector2(view.x, -view.y);
        }

        private void Place(RectTransform node, Vector2 point) => node.anchoredPosition = IceToCanvas(point);

        private void Face(RectTransform node, Vector2 direction)
        {
            if (direction.sqrMagnitude > 1f)
            {
                Vector2 canvas = node.anchoredPosition;
                Vector2 ice = DefensiveZoneView.Unproject(new Vector2(canvas.x, -canvas.y), shift.Mirrored);
                node.localRotation = Quaternion.Euler(0, 0,
                    Vector2.SignedAngle(Vector2.up, IceToCanvas(ice + direction) - canvas));
            }
        }

        private static Texture2D CreatePuckTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Black rubber puck" };
            var pixels = new Color32[size * size];
            Color32 top = new(27, 30, 35, 255);
            Color32 edge = new(8, 10, 13, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - 31.5f) / 31f;
                    float dy = (y - 39f) / 24f;
                    float bottom = (y - 18.3f) / 24f;
                    if (dx * dx + dy * dy <= 1f) pixels[y * size + x] = top;
                    else if (dx * dx + bottom * bottom <= 1f) pixels[y * size + x] = edge;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (puckTexture != null) Destroy(puckTexture);
        }
    }
}
