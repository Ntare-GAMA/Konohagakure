using System;
using UnityEngine;

namespace Konohagakure
{
    /// <summary>
    /// Central control architecture for the Konohagakure training simulator.
    /// Tracks the active drill, coordinates the Shinobi (player), Sensei/Senior Shinobi (NPCs),
    /// the UI flow, and the audio system. Only one drill can be active at a time.
    /// </summary>
    public class KonohaManager : MonoBehaviour
    {
        public static KonohaManager Instance { get; private set; }

        public enum DrillType { Walking, Running, Obstacle }
        public enum SimState { DojoSelect, Training, Debrief }

        [Header("Scene References")]
        [SerializeField] private ShinobiController shinobi;
        [SerializeField] private SenseiController sensei;
        [SerializeField] private CheckpointCourse[] courses; // one per DrillType, index-matched

        [Header("Runtime State")]
        public SimState CurrentState { get; private set; } = SimState.DojoSelect;
        public DrillType ActiveDrill { get; private set; }
        public int CheckpointsHit { get; private set; }
        public float ElapsedTime { get; private set; }

        private bool drillRunning;
        private CheckpointCourse activeCourse;

        public event Action<SimState> OnStateChanged;
        public event Action<int, int> OnCheckpointHit; // (hit, total)
        public event Action<float> OnDrillComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (drillRunning)
            {
                ElapsedTime += Time.deltaTime;
            }
        }

        /// <summary>Called from UI (Dojo Select buttons) to begin a drill.</summary>
        public void StartDrill(DrillType type)
        {
            if (drillRunning) return;

            ActiveDrill = type;
            CheckpointsHit = 0;
            ElapsedTime = 0f;
            drillRunning = true;

            activeCourse = courses[(int)type];
            activeCourse.ResetCourse();
            activeCourse.OnCheckpointTriggered += HandleCheckpoint;

            shinobi.ResetToStart(activeCourse.StartPoint.position, activeCourse.StartPoint.rotation);
            shinobi.SetInputEnabled(true);

            AudioManager.Instance.PlayCue(AudioManager.CueType.SenseiYell);
            AudioManager.Instance.PlayCue(AudioManager.CueType.DrillStart);

            SetState(SimState.Training);
        }

        private void HandleCheckpoint(int total)
        {
            CheckpointsHit++;
            AudioManager.Instance.PlayCue(AudioManager.CueType.Checkpoint);
            OnCheckpointHit?.Invoke(CheckpointsHit, total);

            if (CheckpointsHit >= total)
            {
                EndDrill();
            }
        }

        /// <summary>Ends the active drill, disables input, shows debrief results.</summary>
        public void EndDrill()
        {
            if (!drillRunning) return;

            drillRunning = false;
            shinobi.SetInputEnabled(false);
            activeCourse.OnCheckpointTriggered -= HandleCheckpoint;

            sensei.PlayApprove();
            AudioManager.Instance.PlayCue(AudioManager.CueType.DrillComplete);

            OnDrillComplete?.Invoke(ElapsedTime);
            SetState(SimState.Debrief);
        }

        /// <summary>Called from UI to return to Dojo Select from the Debrief screen.</summary>
        public void ReturnToDojoSelect()
        {
            SetState(SimState.DojoSelect);
        }

        private void SetState(SimState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}