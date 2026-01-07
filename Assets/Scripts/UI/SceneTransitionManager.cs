using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using Shakki.Core;

namespace Shakki.UI
{
    /// <summary>
    /// Manages smooth transitions between game states with fade effects.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private Color fadeColor = Color.black;

        private Canvas fadeCanvas;
        private Image fadeImage;
        private bool isTransitioning;

        private static SceneTransitionManager instance;
        public static SceneTransitionManager Instance => instance;

        public event Action OnFadeOutComplete;
        public event Action OnFadeInComplete;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            CreateFadeCanvas();
        }

        private void Start()
        {
            var flowController = GameFlowController.Instance;
            if (flowController != null)
            {
                flowController.OnStateChanged += HandleStateChanged;
            }

            // Start with fade in
            fadeImage.color = fadeColor;
            StartCoroutine(FadeIn());
        }

        private void OnDestroy()
        {
            var flowController = GameFlowController.Instance;
            if (flowController != null)
            {
                flowController.OnStateChanged -= HandleStateChanged;
            }
        }

        private void CreateFadeCanvas()
        {
            var canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform);
            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // Always on top

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var fadeObj = new GameObject("FadeImage");
            fadeObj.transform.SetParent(canvasObj.transform, false);

            var rect = fadeObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            fadeImage = fadeObj.AddComponent<Image>();
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.raycastTarget = false;
        }

        private void HandleStateChanged(GameFlowController.GameState oldState, GameFlowController.GameState newState)
        {
            // Quick fade for state transitions
            if (!isTransitioning)
            {
                StartCoroutine(QuickTransition());
            }
        }

        private IEnumerator QuickTransition()
        {
            isTransitioning = true;

            // Quick fade out
            float elapsed = 0f;
            float quickDuration = fadeDuration * 0.5f;

            while (elapsed < quickDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 0.5f, elapsed / quickDuration);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            // Quick fade in
            elapsed = 0f;
            while (elapsed < quickDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.5f, 0f, elapsed / quickDuration);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            isTransitioning = false;
        }

        /// <summary>
        /// Fades the screen to black.
        /// </summary>
        public void FadeOut(Action onComplete = null)
        {
            StartCoroutine(FadeOutCoroutine(onComplete));
        }

        /// <summary>
        /// Fades the screen from black.
        /// </summary>
        public void FadeIn(Action onComplete = null)
        {
            StartCoroutine(FadeInCoroutine(onComplete));
        }

        private IEnumerator FadeOut()
        {
            isTransitioning = true;
            fadeImage.raycastTarget = true;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            fadeImage.color = fadeColor;
            OnFadeOutComplete?.Invoke();
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.raycastTarget = false;
            isTransitioning = false;
            OnFadeInComplete?.Invoke();
        }

        private IEnumerator FadeOutCoroutine(Action onComplete)
        {
            yield return FadeOut();
            onComplete?.Invoke();
        }

        private IEnumerator FadeInCoroutine(Action onComplete)
        {
            yield return FadeIn();
            onComplete?.Invoke();
        }

        /// <summary>
        /// Performs a full fade out, executes action, then fades in.
        /// </summary>
        public void TransitionWithAction(Action duringFade)
        {
            StartCoroutine(TransitionCoroutine(duringFade));
        }

        private IEnumerator TransitionCoroutine(Action duringFade)
        {
            yield return FadeOut();
            duringFade?.Invoke();
            yield return FadeIn();
        }
    }
}
