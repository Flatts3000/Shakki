using UnityEngine;
using System.Collections.Generic;

namespace Shakki.Core
{
    /// <summary>
    /// Handles performance optimizations for the game.
    /// Includes object pooling and frame rate management.
    /// </summary>
    public class PerformanceOptimizer : MonoBehaviour
    {
        [Header("Frame Rate")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool vSyncEnabled = false;

        [Header("Quality")]
        [SerializeField] private bool reduceBatteryDrain = true;

        private static PerformanceOptimizer instance;
        public static PerformanceOptimizer Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            ApplySettings();
        }

        private void ApplySettings()
        {
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;

            // VSync
            QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;

            // Reduce battery drain on mobile
            if (reduceBatteryDrain)
            {
                // Lower frame rate when app is in background
                Application.runInBackground = false;

                // Optimize for mobile
                #if UNITY_ANDROID || UNITY_IOS
                // Use lower frame rate on mobile to save battery
                Application.targetFrameRate = 30;
                #endif
            }

            Debug.Log($"[Performance] Target FPS: {Application.targetFrameRate}, VSync: {QualitySettings.vSyncCount}");
        }

        /// <summary>
        /// Call this during intensive operations to temporarily lower quality.
        /// </summary>
        public void EnterLowPowerMode()
        {
            Application.targetFrameRate = 30;
        }

        /// <summary>
        /// Call this when intensive operation is complete.
        /// </summary>
        public void ExitLowPowerMode()
        {
            Application.targetFrameRate = targetFrameRate;
        }

        /// <summary>
        /// Forces garbage collection during a safe time (like loading screens).
        /// </summary>
        public void CollectGarbage()
        {
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }
    }

    /// <summary>
    /// Generic object pool for reusing frequently created objects.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> pool = new Queue<T>();
        private readonly List<T> active = new List<T>();

        public ObjectPool(T prefab, Transform parent, int initialSize = 10)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                CreatePooledObject();
            }
        }

        private T CreatePooledObject()
        {
            var obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
            return obj;
        }

        public T Get()
        {
            T obj = pool.Count > 0 ? pool.Dequeue() : CreatePooledObject();
            obj.gameObject.SetActive(true);
            active.Add(obj);
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null) return;

            obj.gameObject.SetActive(false);
            active.Remove(obj);
            pool.Enqueue(obj);
        }

        public void ReturnAll()
        {
            foreach (var obj in active.ToArray())
            {
                Return(obj);
            }
        }

        public int ActiveCount => active.Count;
        public int PooledCount => pool.Count;
    }
}
