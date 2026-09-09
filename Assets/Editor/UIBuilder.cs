#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Animations;
using TMPro;

namespace Konohagakure.EditorTools
{
    /// <summary>
    /// One-click generator for the Konohagakure Canvas:
    /// DojoSelectPanel / TrainingPanel / DebriefPanel + a UIManager wired to all of it.
    /// Run via Tools > Konohagakure > Build UI Canvas.
    ///
    /// Requires TextMeshPro (TMP Essentials) already imported, and UIManager.cs in the project
    /// under the Konohagakure namespace.
    /// </summary>
    public static class UIBuilder
    {
        private const string AnimatorFolder = "Assets/Konohagakure/Animators";

        [MenuItem("Tools/Konohagakure/Build UI Canvas")]
        public static void BuildCanvas()
        {
            EnsureEventSystem();
            EnsureFolder(AnimatorFolder);

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panelController = BuildPanelAnimatorController();

            GameObject dojoPanel = BuildDojoSelectPanel(canvasGO.transform, panelController);
            GameObject trainingPanel = BuildTrainingPanel(canvasGO.transform, panelController);
            GameObject debriefPanel = BuildDebriefPanel(canvasGO.transform, panelController);

            var uiManagerGO = new GameObject("UIManager", typeof(Konohagakure.UIManager));
            var uiManager = uiManagerGO.GetComponent<Konohagakure.UIManager>();

            var so = new SerializedObject(uiManager);
            so.FindProperty("dojoSelectPanel").objectReferenceValue = dojoPanel.GetComponent<Animator>();
            so.FindProperty("trainingPanel").objectReferenceValue = trainingPanel.GetComponent<Animator>();
            so.FindProperty("debriefPanel").objectReferenceValue = debriefPanel.GetComponent<Animator>();

            so.FindProperty("timerText").objectReferenceValue = trainingPanel.transform.Find("TimerText").GetComponent<TMP_Text>();
            so.FindProperty("checkpointText").objectReferenceValue = trainingPanel.transform.Find("CheckpointText").GetComponent<TMP_Text>();
            so.FindProperty("statusText").objectReferenceValue = trainingPanel.transform.Find("StatusText").GetComponent<TMP_Text>();

            so.FindProperty("resultTimeText").objectReferenceValue = debriefPanel.transform.Find("ResultTimeText").GetComponent<TMP_Text>();
            so.FindProperty("resultCheckpointsText").objectReferenceValue = debriefPanel.transform.Find("ResultCheckpointsText").GetComponent<TMP_Text>();

            var volumeSlider = dojoPanel.transform.Find("VolumeSlider").GetComponent<Slider>();
            var muteToggle = dojoPanel.transform.Find("MuteToggle").GetComponent<Toggle>();
            so.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
            so.FindProperty("muteToggle").objectReferenceValue = muteToggle;
            so.ApplyModifiedProperties();

            WireButton(dojoPanel.transform.Find("WalkingButton").GetComponent<Button>(), uiManager, "OnSelectWalkingDrill");
            WireButton(dojoPanel.transform.Find("RunningButton").GetComponent<Button>(), uiManager, "OnSelectRunningDrill");
            WireButton(dojoPanel.transform.Find("ObstacleButton").GetComponent<Button>(), uiManager, "OnSelectObstacleDrill");
            WireButton(debriefPanel.transform.Find("BackButton").GetComponent<Button>(), uiManager, "OnReturnToDojoSelect");

            // Only Dojo Select should start active; UIManager.Start() will switch panels from here.
            trainingPanel.SetActive(false);
            debriefPanel.SetActive(false);

            Selection.activeGameObject = canvasGO;
            EditorUtility.DisplayDialog(
                "UI Builder",
                "Canvas built successfully.\n\nStill to do:\n" +
                "- Make sure a KonohaManager and AudioManager exist and are active in the scene.\n" +
                "- Panel transitions currently have no visual animation clips — Show/Hide states exist but are empty. " +
                "Add real Animation clips to each panel's Animator if you want fade/slide transitions.\n" +
                "- Double-check button label text and background colors match your art direction.",
                "OK");
        }

        private static AnimatorController BuildPanelAnimatorController()
        {
            string path = AnimatorFolder + "/PanelTransition.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Show", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hide", AnimatorControllerParameterType.Trigger);

            var rootSM = controller.layers[0].stateMachine;
            var hidden = rootSM.AddState("Hidden");
            var visible = rootSM.AddState("Visible");
            rootSM.defaultState = hidden;

            var toVisible = hidden.AddTransition(visible);
            toVisible.hasExitTime = false;
            toVisible.duration = 0f;
            toVisible.AddCondition(AnimatorConditionMode.If, 0, "Show");

            var toHidden = visible.AddTransition(hidden);
            toHidden.hasExitTime = false;
            toHidden.duration = 0f;
            toHidden.AddCondition(AnimatorConditionMode.If, 0, "Hide");

            return controller;
        }

        private static GameObject BuildDojoSelectPanel(Transform parent, AnimatorController controller)
        {
            var panel = CreatePanelBase("DojoSelectPanel", parent, controller, new Color(0.08f, 0.08f, 0.1f, 0.9f));

            CreateText("Title", panel.transform, "Select Training Ground", 42,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(800, 80));

            CreateButton("WalkingButton", panel.transform, "Walking Drill", new Vector2(0, 80), new Vector2(400, 70));
            CreateButton("RunningButton", panel.transform, "Running Drill", new Vector2(0, 0), new Vector2(400, 70));
            CreateButton("ObstacleButton", panel.transform, "Obstacle Drill", new Vector2(0, -80), new Vector2(400, 70));

            CreateSlider("VolumeSlider", panel.transform, new Vector2(-350, -300), new Vector2(300, 30));
            CreateToggle("MuteToggle", panel.transform, "Mute", new Vector2(150, -300), new Vector2(160, 30));

            return panel;
        }

        private static GameObject BuildTrainingPanel(Transform parent, AnimatorController controller)
        {
            var panel = CreatePanelBase("TrainingPanel", parent, controller, new Color(0, 0, 0, 0));

            CreateText("TimerText", panel.transform, "Time: 0.0s", 32,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160, -50), new Vector2(300, 50), TextAlignmentOptions.Left);

            CreateText("CheckpointText", panel.transform, "Checkpoints: 0", 32,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160, -50), new Vector2(300, 50), TextAlignmentOptions.Right);

            CreateText("StatusText", panel.transform, "Ready", 28,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(300, 50));

            return panel;
        }

        private static GameObject BuildDebriefPanel(Transform parent, AnimatorController controller)
        {
            var panel = CreatePanelBase("DebriefPanel", parent, controller, new Color(0.08f, 0.08f, 0.1f, 0.9f));

            CreateText("ResultTimeText", panel.transform, "Final Time: 0.0s", 36,
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(600, 60));

            CreateText("ResultCheckpointsText", panel.transform, "Checkpoints: 0", 36,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(600, 60));

            CreateButton("BackButton", panel.transform, "Back to Dojo Select", new Vector2(0, -220), new Vector2(400, 70));

            return panel;
        }

        // ---------- Low-level builders ----------

        private static GameObject CreatePanelBase(string name, Transform parent, AnimatorController controller, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Animator));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.GetComponent<Image>().color = bgColor;
            go.GetComponent<Animator>().runtimeAnimatorController = controller;

            return go;
        }

        private static void CreateText(string name, Transform parent, string text, float fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
        }

        private static void CreateButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private static void CreateSlider(string name, Transform parent, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Background
            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(go.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);

            // Fill Area / Fill
            var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(go.transform, false);
            var fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5, 0);
            fillAreaRT.offsetMax = new Vector2(-5, 0);

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(1f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            fillGO.GetComponent<Image>().color = new Color(0.4f, 0.7f, 1f);

            slider.fillRect = fillRT;
            slider.targetGraphic = fillGO.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
        }

        private static void CreateToggle(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(go.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.5f);
            bgRT.anchorMax = new Vector2(0f, 0.5f);
            bgRT.anchoredPosition = new Vector2(15, 0);
            bgRT.sizeDelta = new Vector2(28, 28);
            bgGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGO.transform.SetParent(bgGO.transform, false);
            var checkRT = checkGO.GetComponent<RectTransform>();
            checkRT.anchorMin = Vector2.zero;
            checkRT.anchorMax = Vector2.one;
            checkRT.offsetMin = new Vector2(4, 4);
            checkRT.offsetMax = new Vector2(-4, -4);
            checkGO.GetComponent<Image>().color = new Color(0.4f, 0.7f, 1f);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = bgGO.GetComponent<Image>();
            toggle.graphic = checkGO.GetComponent<Image>();
            toggle.isOn = false;

            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.offsetMin = new Vector2(38, 0);
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.white;
        }

        private static void WireButton(Button button, Konohagakure.UIManager target, string methodName)
        {
            var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), target, methodName);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
