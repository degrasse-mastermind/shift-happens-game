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
        private static readonly Color Red = new(0.68f, 0.12f, 0.24f);
        private static readonly Color Amber = new(1f, 0.81f, 0.37f);

        private PracticeShift shift;
        [SerializeField] private DraftShiftSettings draftCoaching = new();
        private bool guided = true;
        private Font font;
        private RectTransform rink;
        private RectTransform player, partner, carrier, puck, goalie, tieMarker, passLine;
        private Text header, status, playLabel, warning, coverage, modeLabel;
        private Button tieButton;
        private Pointer activePointer;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildDisplay();
            SelectSeed(1);
        }

        private void Update()
        {
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
            if (activePointer == pointer) shift.SkateTo(new Vector2(local.x, 70 - local.y));
        }

        private void SelectSeed(int seed)
        {
            shift = new PracticeShift(seed, draftCoaching, guided);
            usingKeys = false;
            activePointer = null;
            DrawShift();
        }

        private void DrawShift()
        {
            Place(player, shift.Player);
            Place(partner, shift.Partner);
            Place(carrier, shift.Carrier);
            Place(puck, shift.Puck);
            Place(goalie, shift.Goalie);

            bool warningActive = shift.Stage == PracticeShift.Phase.Warning;
            warning.transform.parent.gameObject.SetActive(warningActive);
            passLine.gameObject.SetActive(warningActive);
            tieMarker.gameObject.SetActive(warningActive || shift.Tied);
            tieMarker.GetComponent<UnityEngine.UI.Image>().color = shift.Tied ? Blue : Amber;
            if (warningActive)
            {
                Vector2 delta = IceToCanvas(shift.NetFrontAttacker) - IceToCanvas(shift.Puck);
                passLine.anchoredPosition = IceToCanvas(shift.Puck);
                passLine.sizeDelta = new Vector2(delta.magnitude, 5);
                passLine.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }

            header.text = $"SHIFT HAPPENS!  /  DRAFT COACHING FOR DAVID     SEED {shift.Seed}  ·  {shift.Time:0.0}s";
            status.text = shift.Paused ? "Paused" : shift.Message;
            partner.GetComponent<UnityEngine.UI.Image>().color = guided && shift.ContainmentAvailable
                ? new Color(0.13f, 0.47f, 0.35f) : Blue;
            coverage.text = shift.Stage == PracticeShift.Phase.Finished ? shift.DraftReview
                : guided || shift.Stage == PracticeShift.Phase.Ready
                    ? shift.ContainmentAvailable ? "DRAFT  RD covers net front  |  LD may contain from inside"
                        : shift.PuckPossession == PracticeShift.Possession.RedCarrier
                            ? "DRAFT  RD returning from bottom corner  |  LD protects net front"
                            : "DRAFT  Track the puck through the play"
                    : "FREER  /  Draft coaching review after the shift";
            playLabel.text = shift.Stage == PracticeShift.Phase.Ready ? "Start"
                : shift.Stage == PracticeShift.Phase.Finished ? "Retry"
                : shift.TeachingPaused ? "Resume read"
                : shift.Paused ? "Resume" : "Pause";
            modeLabel.text = guided ? "Guided" : "Freer";
            tieButton.interactable = shift.CanSkate;
        }

        private void BuildDisplay()
        {
            var canvasObject = new GameObject("Practice Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 860);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var root = Rect("Practice board", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1280, 860));
            Panel(root, Navy);
            header = Label("Header", root, new(10, -8), new(1260, 48), 25, Color.white);
            rink = Rect("Ice", root, new(0, 1), new(0, 1), new(10, -56), new(1260, 650));
            var rinkImage = rink.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            rinkImage.texture = Resources.Load<Texture2D>("PracticeRink");
            rinkImage.raycastTarget = false;

            tieMarker = Rect("Red C highlight", rink, new(0, 1), new(0.5f, 0.5f), IceToCanvas(draftCoaching.Map(PracticeShift.Center)), new(58, 58));
            Panel(tieMarker, Amber);
            passLine = Rect("Pass lane", rink, new(0, 1), new(0, 0.5f), Vector2.zero, new(0, 5));
            Panel(passLine, Amber);
            partner = Token("RD", draftCoaching.Map(draftCoaching.PartnerStart), Blue);
            Token("C", draftCoaching.Map(new Vector2(225, 465)), Blue);
            Token("LW", draftCoaching.Map(new Vector2(330, 245)), Blue);
            Token("RW", draftCoaching.Map(new Vector2(330, 550)), Blue);
            Token("LW", draftCoaching.Map(new Vector2(260, 500)), Red);
            Token("LD", draftCoaching.Map(new Vector2(390, 635)), Red);
            Token("RD", draftCoaching.Map(PracticeShift.Point), Red);
            Token("C", draftCoaching.Map(PracticeShift.Center), Red);
            Token("G", draftCoaching.Map(new Vector2(1163, 397)), Red);
            player = Token("Y", PracticeShift.StartPosition, Blue, 48);
            carrier = Token("RW", new(230, 315), Red);
            goalie = Token("G", new(105, 397), Blue);
            puck = Token("", new(213, 324), Color.black, 16);

            var warningPanel = Rect("Pass warning panel", rink, new(0, 1), new(0, 1), new(12, -12), new(322, 45));
            Panel(warningPanel, Amber);
            warning = Label("Pass warning", warningPanel, Vector2.zero, warningPanel.sizeDelta, 24, Navy);
            warning.text = "PASS COMING TO RED C";

            status = Label("Play by play", root, new(12, -707), new(1256, 40), 21, Color.white);
            coverage = Label("Draft coverage review", root, new(12, -747), new(1256, 36), 21, Amber);
            playLabel = Control("Start", root, 12, () =>
            {
                if (shift.Stage == PracticeShift.Phase.Finished) SelectSeed(shift.Seed);
                shift.TogglePlay();
            }, out _);
            Control("Point look · 0", root, 222, () => SelectSeed(0), out _);
            Control("Slot look · 1", root, 432, () => SelectSeed(1), out _);
            Control("New opponent", root, 642, () => SelectSeed(Random.Range(0, int.MaxValue)), out _);
            modeLabel = Control("Guided", root, 852, () =>
            {
                guided = !guided;
                SelectSeed(shift.Seed);
            }, out _);
            Control("Tie up · 4", root, 1062, () => shift.TieUp(), out tieButton);

            var input = new GameObject("Practice Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            input.transform.SetParent(transform, false);
        }

        private RectTransform Token(string title, Vector2 position, Color color, float size = 42)
        {
            var node = Rect("Skater " + title, rink, new(0, 1), new(0.5f, 0.5f), IceToCanvas(position), new(size, size));
            Panel(node, color);
            if (title.Length > 0) Label(title, node, Vector2.zero, new(size, size), 21, Color.white).text = title;
            return node;
        }

        private Text Control(string title, Transform parent, float x, UnityEngine.Events.UnityAction onClick, out Button button)
        {
            var node = Rect(title, parent, new(0, 1), new(0, 1), new(x, -794), new(196, 54));
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

        private static Vector2 IceToCanvas(Vector2 point) => new(point.x, 70 - point.y);
        private static void Place(RectTransform node, Vector2 point) => node.anchoredPosition = IceToCanvas(point);
    }
}

