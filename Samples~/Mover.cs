using UnityEngine;

namespace Miyu.Tools
{
    public class Mover : MonoBehaviour
    {
        [SerializeField] private Vector3 m_TargetPosition;
        [SerializeField] private AnimationCurve m_MoveCurve;
        [SerializeField] private float m_Duration = 1f;
        [SerializeField] private bool m_Loop = true;
        private Vector3 m_OriginalPosition;
        private float m_CurrentTime = 0f;

        private void Start() => m_OriginalPosition = transform.position;

        private void Update() => Move();

        private void Move()
        {
            m_CurrentTime += Time.deltaTime;

            var time = m_Loop ? 
                (m_CurrentTime / m_Duration) % 1f : 
                Mathf.Clamp01(m_CurrentTime / m_Duration);
            var curveValue = m_MoveCurve.Evaluate(time);
            transform.position = Vector3.Lerp(m_OriginalPosition, m_TargetPosition, curveValue);
        }
    }
}