using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShiftHappens
{
    public sealed partial class RinkPractice
    {
        private const string SavedTuningKey = "ShiftHappens.OriginalRdDraft.v1";
        private const string DefaultTuningName = "RD return";
        private const int MinReturnSpeed = 24;
        private const int MaxReturnSpeed = 72;
        private const int ReturnSpeedStep = 4;
        private static readonly Vector2 MinPartnerStart = new(110, 190);
        private static readonly Vector2 MaxPartnerStart = new(410, 610);

        [Serializable]
        private sealed class RdDraft
        {
            public string name;
            public Vector2 start;
            public float speed;
        }

        private DraftShiftSettings appliedTuning;
        private string appliedTuningName;
        private DraftShiftSettings workingTuning;
        private GameObject tuningPanel, tuningIce;
        private Button tuneButton, loadDraftButton, slowerButton, fasterButton;
        private InputField draftName;
        private Text tuningSpeed, tuningStatus;
        private RectTransform tuningMarker;

        private void OpenTuning()
        {
            if (presetIndex != 0 || replayFirstGuidedShift.HasValue || review.Active) return;
            workingTuning = draftCoaching.Copy();
            draftName.text = appliedTuningName ?? DefaultTuningName;
            activePointer = null;
            practiceControls.interactable = false;
            practiceControls.blocksRaycasts = false;
            tuningPanel.SetActive(true);
            tuningIce.SetActive(true);
            RdDraft saved = ReadSavedTuning();
            ShowTuningDraft(saved == null ? "Edits apply when you restart." : $"Saved draft: {saved.name}", saved);
        }

        private void CloseTuning()
        {
            if (draftName.isFocused) draftName.DeactivateInputField();
            tuningPanel.SetActive(false);
            tuningIce.SetActive(false);
            workingTuning = null;
            practiceControls.interactable = true;
            practiceControls.blocksRaycasts = true;
        }

        private void ApplyTuning()
        {
            appliedTuning = workingTuning.Copy();
            appliedTuningName = TuningName();
            CloseTuning();
            SelectSeed(shift.Seed);
        }

        private void RestoreOriginal()
        {
            appliedTuning = null;
            appliedTuningName = null;
            CloseTuning();
            SelectSeed(shift.Seed);
        }

        private void PlaceTuningRd(BaseEventData data)
        {
            if (data is not PointerEventData pointer ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(rink, pointer.position, pointer.pressEventCamera,
                    out Vector2 local)) return;
            SetTuningRd(DefensiveZoneView.Unproject(new Vector2(local.x, -local.y), shift.Mirrored));
        }

        private void SetTuningRd(Vector2 simulationPosition)
        {
            Vector2 unmirrored = workingTuning.Map(simulationPosition);
            workingTuning.PartnerStart = Vector2.Max(MinPartnerStart, Vector2.Min(MaxPartnerStart, unmirrored));
            Place(tuningMarker, workingTuning.Map(workingTuning.PartnerStart));
        }

        private void AdjustReturnSpeed(int amount)
        {
            workingTuning.PartnerReturnSpeed = Mathf.Clamp(workingTuning.PartnerReturnSpeed + amount,
                MinReturnSpeed, MaxReturnSpeed);
            ShowTuningSpeed();
        }

        private void ShowTuningSpeed()
        {
            float speed = workingTuning.PartnerReturnSpeed;
            tuningSpeed.text = $"RD return speed\n{speed:0} sim units/s";
            slowerButton.interactable = speed > MinReturnSpeed;
            fasterButton.interactable = speed < MaxReturnSpeed;
        }

        private string TuningName() => string.IsNullOrWhiteSpace(draftName.text) ? DefaultTuningName : draftName.text.Trim();

        private static RdDraft ReadSavedTuning()
        {
            string json = PlayerPrefs.GetString(SavedTuningKey);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var draft = JsonUtility.FromJson<RdDraft>(json);
                return draft != null && !string.IsNullOrWhiteSpace(draft.name) && draft.name.Length <= 24 &&
                    draft.start.x >= MinPartnerStart.x && draft.start.x <= MaxPartnerStart.x &&
                    draft.start.y >= MinPartnerStart.y && draft.start.y <= MaxPartnerStart.y &&
                    draft.speed >= MinReturnSpeed && draft.speed <= MaxReturnSpeed ? draft : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private void SaveTuning()
        {
            var draft = new RdDraft { name = TuningName(), start = workingTuning.PartnerStart,
                speed = workingTuning.PartnerReturnSpeed };
            PlayerPrefs.SetString(SavedTuningKey, JsonUtility.ToJson(draft));
            PlayerPrefs.Save();
            draftName.text = draft.name;
            ShowTuningDraft($"Saved draft: {draft.name}", draft);
        }

        private void LoadTuning()
        {
            var draft = ReadSavedTuning();
            if (draft == null) return;
            workingTuning.PartnerStart = draft.start;
            workingTuning.PartnerReturnSpeed = draft.speed;
            draftName.text = draft.name;
            ShowTuningDraft("Draft loaded. Apply to try it.", draft);
        }

        private void ShowTuningDraft(string message, RdDraft saved)
        {
            tuningStatus.text = message;
            loadDraftButton.interactable = saved != null;
            Place(tuningMarker, workingTuning.Map(workingTuning.PartnerStart));
            ShowTuningSpeed();
        }

        private void BuildTuningDisplay()
        {
            var tune = Control("Tune RD", root, new(720, -683, 268, 40), OpenTuning, out tuneButton);
            Responsive(tune, new(26, -1070, 274, 54));

            var ice = Rect("Place draft RD", rink, Vector2.up, Vector2.up, Vector2.zero, Vector2.zero);
            ice.anchorMax = Vector2.one;
            ice.anchorMin = Vector2.zero;
            ice.offsetMin = ice.offsetMax = Vector2.zero;
            Panel(ice, new Color(0.04f, 0.17f, 0.25f, .14f)).raycastTarget = true;
            ice.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups = true;
            var placement = ice.gameObject.AddComponent<EventTrigger>();
            foreach (var eventId in new[] { EventTriggerType.PointerDown, EventTriggerType.Drag })
            {
                var entry = new EventTrigger.Entry { eventID = eventId };
                entry.callback.AddListener(PlaceTuningRd);
                placement.triggers.Add(entry);
            }
            tuningMarker = Rect("Draft RD marker", ice, Vector2.up, new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(104, 32));
            Panel(tuningMarker, Navy);
            Label("Draft RD", tuningMarker, Vector2.zero, tuningMarker.sizeDelta, 19, HandoffGreen).text = "Draft RD";
            tuningIce = ice.gameObject;

            var panel = Rect("Practice tuning", root, Vector2.up, Vector2.up, new Vector2(704, -55),
                new Vector2(300, 735));
            Panel(panel, Navy).raycastTarget = true;
            panel.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups = true;
            Responsive(panel, new Rect(12, -698, 616, 438));
            tuningPanel = panel.gameObject;

            Text TuningLabel(string name, string value, Rect bounds, int size, Color color)
            {
                var label = Label(name, panel, bounds.position, bounds.size, size, color);
                CenterTuningRow(label.rectTransform);
                label.text = value;
                return label;
            }

            Button TuningButton(string name, Rect bounds, UnityEngine.Events.UnityAction action)
            {
                var label = Control(name, panel, bounds, action, out var button);
                label.fontSize = 18;
                CenterTuningRow((RectTransform)button.transform);
                return button;
            }

            TuningLabel("Tuning heading", "Tune Original · Blue RD", new Rect(-136, -8, 272, 32), 23, Color.white);
            TuningLabel("Placement instruction", "Tap or drag on the ice to place RD.", new Rect(-136, -45, 272, 46), 19, Color.white);
            tuningSpeed = TuningLabel("Return speed", "", new Rect(-87, -94, 174, 48), 18, Color.white);
            slowerButton = TuningButton("-", new Rect(-136, -98, 44, 40), () => AdjustReturnSpeed(-ReturnSpeedStep));
            fasterButton = TuningButton("+", new Rect(92, -98, 44, 40), () => AdjustReturnSpeed(ReturnSpeedStep));

            var nameBox = Rect("Draft name", panel, new Vector2(.5f, 1), Vector2.up,
                new Vector2(-136, -154), new Vector2(272, 38));
            var nameBackground = Panel(nameBox, Blue);
            nameBackground.raycastTarget = true;
            draftName = nameBox.gameObject.AddComponent<InputField>();
            draftName.targetGraphic = nameBackground;
            draftName.textComponent = Label("Draft name text", nameBox, new Vector2(6, 0), new Vector2(260, 38), 19, Color.white);
            draftName.placeholder = Label("Draft name placeholder", nameBox, new Vector2(6, 0), new Vector2(260, 38), 19, Color.white);
            ((Text)draftName.placeholder).text = "Draft name";
            draftName.characterLimit = 24;

            TuningButton("Save draft", new Rect(-136, -200, 132, 38), SaveTuning);
            loadDraftButton = TuningButton("Load draft", new Rect(4, -200, 132, 38), LoadTuning);
            tuningStatus = TuningLabel("Draft status", "", new Rect(-136, -242, 272, 38), 16, Amber);
            TuningButton("Apply & restart", new Rect(-136, -284, 272, 42), ApplyTuning);
            TuningButton("Restore Original", new Rect(-136, -334, 272, 38), RestoreOriginal);
            TuningButton("Cancel", new Rect(-136, -380, 272, 38), CloseTuning);
            tuningIce.SetActive(false);
            tuningPanel.SetActive(false);
        }

        private static void CenterTuningRow(RectTransform row)
        {
            Vector2 offset = row.anchoredPosition;
            row.anchorMin = row.anchorMax = new Vector2(.5f, 1);
            row.anchoredPosition = offset;
        }
    }
}
