using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;

namespace Konohagakure
{
    /// <summary>
    /// Generates real "Show"/"Hide" animation clips for the panels created by UIBuilder.cs
    /// and assigns them to the shared PanelTransition AnimatorController's Show/Hide states.
    ///
    /// Run AFTER UIBuilder's "Build UI Canvas" has already created the Canvas + panels.
    ///
    /// Menu: Tools → Konohagakure → Build UI Animations
    /// </summary>
    public static class UIAnimationBuilder
    {
        private const string ClipFolder = "Assets/Konohagakure/Animations/UI";
        private const string ControllerPath = "Assets/Konohagakure/Animators/PanelTransition.controller";

        [MenuItem("Tools/Konohagakure/Build UI Animations")]
        public static void BuildUIAnimations()
        {
            if (!Directory.Exists(ClipFolder))
                Directory.CreateDirectory(ClipFolder);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            bool needsRebuild = controller == null || !HasState(controller, "Showing") || !HasState(controller, "Hiding");
            if (needsRebuild)
            {
                if (controller != null)
                {
                    Debug.Log("[UIAnimationBuilder] Existing PanelTransition controller uses the old layout " +
                              "— rebuilding it so Hidden/Visible hold a fixed pose instead of doing nothing.");
                    AssetDatabase.DeleteAsset(ControllerPath);
                }
                else
                {
                    Debug.Log("[UIAnimationBuilder] No PanelTransition controller found — creating one at " + ControllerPath);
                }
                controller = CreatePanelTransitionController();
            }

            // Make sure every panel has a CanvasGroup — that's what we animate (alpha)
            // in addition to scale, so fades actually block raycasts while hidden.
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[UIAnimationBuilder] No 'Canvas' GameObject found in the scene.");
                return;
            }

            string[] panelNames = { "DojoSelectPanel", "TrainingPanel", "DebriefPanel" };

            AnimationClip showClip = BuildClip("PanelShow", show: true);
            AnimationClip hideClip = BuildClip("PanelHide", show: false);
            AnimationClip visiblePoseClip = BuildPoseClip("PanelVisiblePose", visible: true);
            AnimationClip hiddenPoseClip = BuildPoseClip("PanelHiddenPose", visible: false);

            AssignClipsToController(controller, showClip, hideClip, visiblePoseClip, hiddenPoseClip);

            foreach (var panelName in panelNames)
            {
                var panelTransform = canvas.transform.Find(panelName);
                if (panelTransform == null)
                {
                    Debug.LogWarning($"[UIAnimationBuilder] Panel '{panelName}' not found under Canvas — skipped.");
                    continue;
                }

                var go = panelTransform.gameObject;
                var group = go.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = go.AddComponent<CanvasGroup>();
                }

                var animator = go.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = go.AddComponent<Animator>();
                    Debug.Log($"[UIAnimationBuilder] '{panelName}' had no Animator — added one.");
                }
                animator.runtimeAnimatorController = controller;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UIAnimationBuilder] Done. Show = fade+scale in (0.25s), Hide = fade+scale out (0.2s). " +
                      "CanvasGroup added to each panel so hidden panels also stop blocking clicks.");
        }

        private static bool HasState(AnimatorController controller, string stateName)
        {
            foreach (var childState in controller.layers[0].stateMachine.states)
            {
                if (childState.state.name == stateName) return true;
            }
            return false;
        }

        private static AnimatorController CreatePanelTransitionController()
        {
            string folder = Path.GetDirectoryName(ControllerPath).Replace('\\', '/');
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Show", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hide", AnimatorControllerParameterType.Trigger);

            var rootSM = controller.layers[0].stateMachine;

            // Hidden/Visible get a static single-key "pose" clip assigned after clip creation
            // (see AssignClipsToController) — that's what makes entering them at Start (via
            // the default state) actually snap alpha/raycasts to the right value instead of
            // leaving whatever was last serialized in the Editor.
            var hidden = rootSM.AddState("Hidden");
            var visible = rootSM.AddState("Visible");
            rootSM.defaultState = hidden;

            // Transitional states — these actually play the fade clips, then auto-advance
            // to the matching resting state once the clip finishes.
            var showing = rootSM.AddState("Showing");
            var hiding = rootSM.AddState("Hiding");

            var toShowing = hidden.AddTransition(showing);
            toShowing.hasExitTime = false;
            toShowing.duration = 0f;
            toShowing.AddCondition(AnimatorConditionMode.If, 0, "Show");

            var showingToVisible = showing.AddTransition(visible);
            showingToVisible.hasExitTime = true;
            showingToVisible.exitTime = 1f;
            showingToVisible.duration = 0f;
            showingToVisible.hasFixedDuration = true;

            var toHiding = visible.AddTransition(hiding);
            toHiding.hasExitTime = false;
            toHiding.duration = 0f;
            toHiding.AddCondition(AnimatorConditionMode.If, 0, "Hide");

            var hidingToHidden = hiding.AddTransition(hidden);
            hidingToHidden.hasExitTime = true;
            hidingToHidden.exitTime = 1f;
            hidingToHidden.duration = 0f;
            hidingToHidden.hasFixedDuration = true;

            return controller;
        }

        /// <summary>
        /// A single-keyframe "pose" clip — holds alpha/scale/interactable/blocksRaycasts at a
        /// fixed value with no animation over time. Used for the static Hidden/Visible resting
        /// states so entering them (including at scene Start, via the default state) snaps the
        /// panel to the correct value instead of leaving whatever was last set in the Editor.
        /// </summary>
        private static AnimationClip BuildPoseClip(string clipName, bool visible)
        {
            string path = $"{ClipFolder}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, path);
            }
            else
            {
                clip.ClearCurves();
            }

            clip.frameRate = 60;

            float alpha = visible ? 1f : 0f;
            float scale = visible ? 1f : 0.92f;
            float flag = visible ? 1f : 0f;

            clip.SetCurve("", typeof(CanvasGroup), "m_Alpha", ConstantCurve(alpha));
            clip.SetCurve("", typeof(Transform), "m_LocalScale.x", ConstantCurve(scale));
            clip.SetCurve("", typeof(Transform), "m_LocalScale.y", ConstantCurve(scale));
            clip.SetCurve("", typeof(Transform), "m_LocalScale.z", ConstantCurve(1f));
            clip.SetCurve("", typeof(CanvasGroup), "m_Interactable", ConstantCurve(flag));
            clip.SetCurve("", typeof(CanvasGroup), "m_BlocksRaycasts", ConstantCurve(flag));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationCurve ConstantCurve(float value)
        {
            var curve = new AnimationCurve();
            curve.AddKey(0f, value);
            return curve;
        }

        private static AnimationClip BuildClip(string clipName, bool show)
        {
            string path = $"{ClipFolder}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, path);
            }
            else
            {
                clip.ClearCurves();
            }

            clip.frameRate = 60;

            float duration = show ? 0.25f : 0.2f;

            // Alpha curve (CanvasGroup)
            AnimationCurve alphaCurve = show
                ? AnimationCurve.EaseInOut(0f, 0f, duration, 1f)
                : AnimationCurve.EaseInOut(0f, 1f, duration, 0f);

            clip.SetCurve("", typeof(CanvasGroup), "m_Alpha", alphaCurve);

            // Scale curve — subtle pop-in / pop-out, not a jarring resize
            float fromScale = show ? 0.92f : 1f;
            float toScale = show ? 1f : 0.92f;

            AnimationCurve scaleX = AnimationCurve.EaseInOut(0f, fromScale, duration, toScale);
            AnimationCurve scaleY = AnimationCurve.EaseInOut(0f, fromScale, duration, toScale);
            AnimationCurve scaleZ = AnimationCurve.EaseInOut(0f, 1f, duration, 1f);

            clip.SetCurve("", typeof(Transform), "m_LocalScale.x", scaleX);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.y", scaleY);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.z", scaleZ);

            // Also drive interactable/blocksRaycasts so a hidden panel can't be clicked mid/after fade
            AnimationCurve interactableCurve = new AnimationCurve();
            interactableCurve.AddKey(0f, show ? 0f : 1f);
            interactableCurve.AddKey(duration, show ? 1f : 0f);
            clip.SetCurve("", typeof(CanvasGroup), "m_Interactable", interactableCurve);
            clip.SetCurve("", typeof(CanvasGroup), "m_BlocksRaycasts", interactableCurve);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void AssignClipsToController(AnimatorController controller, AnimationClip showClip,
            AnimationClip hideClip, AnimationClip visiblePoseClip, AnimationClip hiddenPoseClip)
        {
            var layer = controller.layers[0];
            var stateMachine = layer.stateMachine;

            // Transitional states play the animated fade clips.
            AssignClipToState(stateMachine, "Showing", showClip);
            AssignClipToState(stateMachine, "Hiding", hideClip);

            // Resting states hold a fixed pose — this is what makes the default "Hidden" state
            // actually hide a panel at scene Start, instead of leaving it at whatever alpha was
            // last saved in the Editor.
            AssignClipToState(stateMachine, "Visible", visiblePoseClip);
            AssignClipToState(stateMachine, "Hidden", hiddenPoseClip);

            EditorUtility.SetDirty(controller);
        }

        private static void AssignClipToState(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                {
                    childState.state.motion = clip;
                    return;
                }
            }

            Debug.LogWarning($"[UIAnimationBuilder] No state named '{stateName}' found in PanelTransition controller — " +
                              "clip wasn't assigned. Check the controller's state names match 'Visible'/'Hidden'.");
        }
    }
}