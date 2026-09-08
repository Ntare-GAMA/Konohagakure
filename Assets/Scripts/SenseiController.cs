using UnityEngine;

namespace Konohagakure
{
    /// <summary>
    /// Sensei NPC. Idle by default; plays an Approve/Nod reaction when the Shinobi completes a
    /// drill. The drill-start cue is a yell audio clip (played by AudioManager) rather than a
    /// separate animation, so no "Explain" state is needed here.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class SenseiController : MonoBehaviour
    {
        [SerializeField] private string approveTrigger = "Approve";

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void PlayApprove() => animator.SetTrigger(approveTrigger);
    }
}