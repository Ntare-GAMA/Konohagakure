using System;
using UnityEngine;

namespace Konohagakure
{
    /// <summary>
    /// Groups the checkpoint triggers for one drill's course. Each child CheckpointTrigger
    /// reports back here; once all are hit the course reports completion to KonohaManager.
    /// </summary>
    public class CheckpointCourse : MonoBehaviour
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private CheckpointTrigger[] checkpoints;

        public Transform StartPoint => startPoint;
        public int TotalCheckpoints => checkpoints.Length;

        /// <summary>(checkpointsHitSoFar is tracked by KonohaManager; this just reports total)</summary>
        public event Action<int> OnCheckpointTriggered;

        private void Awake()
        {
            foreach (var cp in checkpoints)
            {
                cp.Init(this);
            }
        }

        public void ResetCourse()
        {
            foreach (var cp in checkpoints)
            {
                cp.ResetCheckpoint();
            }
        }

        public void ReportHit()
        {
            OnCheckpointTriggered?.Invoke(TotalCheckpoints);
        }
    }
}
