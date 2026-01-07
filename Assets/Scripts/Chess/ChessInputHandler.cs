using UnityEngine;
using UnityEngine.InputSystem;

namespace Chess
{
    public class ChessInputHandler : MonoBehaviour
    {
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(
                    new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));

                ChessGame.Instance.HandleClick(worldPos);
            }
        }
    }
}
