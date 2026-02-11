using UnityEngine;
using System.Collections;

namespace Miyu.Tools
{
    public class FreeCamera : MonoBehaviour
    {
        [SerializeField] private float m_MoveSpeed = 10f;
        [SerializeField] private float m_LookSpeed = 2f;
        [SerializeField] private float m_SpeedMultiplier = 2f;
        private float m_Yaw;
        private float m_Pitch;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) Reset();

            if (Input.GetKeyDown(KeyCode.P)) Time.timeScale = Time.timeScale == 1 ? 0 : 1;

            if (Input.GetKeyDown(KeyCode.Period) && Time.timeScale == 0) StartCoroutine(StepFrame());

            if (Input.GetMouseButton(1))
            {
                CameraRotation();
                GetMovement(GetSpeed());
            }
        }

        private void CameraRotation()
        {
            m_Yaw += Input.GetAxis("Mouse X") * m_LookSpeed;
            m_Pitch -= Input.GetAxis("Mouse Y") * m_LookSpeed;

            m_Pitch = Mathf.Clamp(m_Pitch, -90f, 90f);
            transform.rotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
        }

        private void GetMovement(float speed)
        {
            var input = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0),
                Input.GetAxisRaw("Vertical")
            );
            transform.position += speed * Time.unscaledDeltaTime * transform.TransformDirection(input.normalized);
        }

        private IEnumerator StepFrame()
        {
            Time.timeScale = 1;
            yield return null;
            Time.timeScale = 0;
        }

        private float GetSpeed() => Input.GetKey(KeyCode.LeftShift) ? m_MoveSpeed * m_SpeedMultiplier : m_MoveSpeed;

        private void Reset()
        {
            m_Yaw = 0f;
            m_Pitch = 0f;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}