using UnityEngine;

namespace Konohagakure
{
    /// <summary>Place on a trigger collider along the course. Fires once per drill run, but only
    /// if the Shinobi was performing the required action (walking/running/jumping) at contact.</summary>
    [RequireComponent(typeof(Collider))]
    public class CheckpointTrigger : MonoBehaviour
    {
        public enum RequiredAction { Walk, Run, Jump, Strafe }

        [Tooltip("The Shinobi must be performing this action at the moment of contact for the checkpoint to count.")]
        [SerializeField] private RequiredAction requiredAction = RequiredAction.Walk;

        private CheckpointCourse course;
        private bool triggered;

        public void Init(CheckpointCourse owner) => course = owner;

        public void ResetCheckpoint() => triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered || !other.CompareTag("Player")) return;

            var shinobi = other.GetComponent<ShinobiController>();
            if (shinobi == null) return;

            if (!IsPerformingRequiredAction(shinobi)) return;

            triggered = true;
            course.ReportHit();
        }

        private bool IsPerformingRequiredAction(ShinobiController shinobi)
        {
            float speed = shinobi.CurrentSpeed;
            float strafe = shinobi.CurrentStrafe;

            switch (requiredAction)
            {
                case RequiredAction.Walk:
                    // walk (1) or walk_backwards (-1) tier, not run_backwards (-2) or idle (0)
                    return Mathf.Abs(speed) >= 0.5f && Mathf.Abs(speed) < 1.5f;
                case RequiredAction.Run:
                    // only run tier is run_backwards (-2)
                    return speed <= -1.5f;
                case RequiredAction.Jump:
                    return !shinobi.IsGrounded;
                case RequiredAction.Strafe:
                    return Mathf.Abs(strafe) >= 0.5f;
                default:
                    return false;
            }
        }
    }
}