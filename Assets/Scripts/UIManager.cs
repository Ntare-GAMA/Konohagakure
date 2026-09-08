using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Konohagakure
{
    /// <summary>
    /// Drives the three UI screens (Dojo Select, Training Ground, Debrief) and their animated
    /// transitions, and reacts live to KonohaManager's state/checkpoint/drill events.
    /// Each panel has its own Animator with "Show"/"Hide" triggers for the transition.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels (each with its own Animator: Show/Hide triggers)")]
        [SerializeField] private Animator dojoSelectPanel;
        [SerializeField] private Animator trainingPanel;
        [SerializeField] private Animator debriefPanel;

        [Header("Training Screen (live, reacts to state)")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text checkpointText;
        [SerializeField] private TMP_Text statusText; // e.g. "Walking" / "Running" / "Jumping"

        [Header("Debrief Screen")]
        [SerializeField] private TMP_Text resultTimeText;
        [SerializeField] private TMP_Text resultCheckpointsText;

        [Header("Audio Controls")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle muteToggle;

        private Animator currentPanel;

        private void Start()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("UIManager: AudioManager.Instance is null — make sure an AudioManager GameObject exists and is active in the scene.");
            }
            else
            {
                if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
                else Debug.LogWarning("UIManager: Volume Slider is not assigned in the Inspector.");

                if (muteToggle != null) muteToggle.onValueChanged.AddListener(AudioManager.Instance.ToggleMute);
                else Debug.LogWarning("UIManager: Mute Toggle is not assigned in the Inspector.");
            }

            if (KonohaManager.Instance == null)
            {
                Debug.LogWarning("UIManager: KonohaManager.Instance is null — make sure a GameManager GameObject with KonohaManager exists and is active in the scene.");
            }
            else
            {
                KonohaManager.Instance.OnStateChanged += HandleStateChanged;
                KonohaManager.Instance.OnCheckpointHit += HandleCheckpointHit;
                KonohaManager.Instance.OnDrillComplete += HandleDrillComplete;
            }

            if (dojoSelectPanel != null) SwitchPanel(dojoSelectPanel);
            else Debug.LogWarning("UIManager: Dojo Select Panel is not assigned in the Inspector.");
        }

        private void Update()
        {
            if (KonohaManager.Instance.CurrentState == KonohaManager.SimState.Training)
            {
                timerText.text = $"Time: {KonohaManager.Instance.ElapsedTime:0.0}s";
            }
        }

        // --- Wired to Dojo Select buttons ---
        public void OnSelectWalkingDrill() => KonohaManager.Instance.StartDrill(KonohaManager.DrillType.Walking);
        public void OnSelectRunningDrill() => KonohaManager.Instance.StartDrill(KonohaManager.DrillType.Running);
        public void OnSelectObstacleDrill() => KonohaManager.Instance.StartDrill(KonohaManager.DrillType.Obstacle);

        // --- Wired to Debrief "Back" button ---
        public void OnReturnToDojoSelect() => KonohaManager.Instance.ReturnToDojoSelect();

        private void HandleStateChanged(KonohaManager.SimState state)
        {
            switch (state)
            {
                case KonohaManager.SimState.DojoSelect:
                    SwitchPanel(dojoSelectPanel);
                    break;
                case KonohaManager.SimState.Training:
                    checkpointText.text = "Checkpoints: 0";
                    statusText.text = "Ready";
                    SwitchPanel(trainingPanel);
                    break;
                case KonohaManager.SimState.Debrief:
                    SwitchPanel(debriefPanel);
                    break;
            }
        }

        private void HandleCheckpointHit(int hit, int total)
        {
            checkpointText.text = $"Checkpoints: {hit}/{total}";
        }

        private void HandleDrillComplete(float finalTime)
        {
            resultTimeText.text = $"Final Time: {finalTime:0.0}s";
            resultCheckpointsText.text = $"Checkpoints: {KonohaManager.Instance.CheckpointsHit}";
        }

        private void SwitchPanel(Animator target)
        {
            if (currentPanel != null)
            {
                currentPanel.SetTrigger("Hide");
            }
            currentPanel = target;
            currentPanel.gameObject.SetActive(true);
            currentPanel.SetTrigger("Show");
        }
    }
}