using UnityEngine;

namespace Shakki.UI
{
    /// <summary>
    /// Handles safe area insets for notched devices (iPhone X+, Android punch-holes, etc.)
    /// Attach to a RectTransform that should respect the safe area.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;

        private RectTransform rectTransform;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            // Check if safe area changed (device rotation, etc.)
            if (lastSafeArea != Screen.safeArea ||
                lastScreenSize.x != Screen.width ||
                lastScreenSize.y != Screen.height ||
                lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            // Convert safe area to anchor values (0-1)
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Apply selective edges
            if (!applyLeft) anchorMin.x = 0;
            if (!applyBottom) anchorMin.y = 0;
            if (!applyRight) anchorMax.x = 1;
            if (!applyTop) anchorMax.y = 1;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Creates a SafeAreaHandler on the given RectTransform.
        /// </summary>
        public static SafeAreaHandler AddTo(RectTransform rectTransform, bool top = true, bool bottom = true, bool left = true, bool right = true)
        {
            var handler = rectTransform.gameObject.AddComponent<SafeAreaHandler>();
            handler.applyTop = top;
            handler.applyBottom = bottom;
            handler.applyLeft = left;
            handler.applyRight = right;
            return handler;
        }
    }
}
