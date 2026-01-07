#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

namespace Shakki.Editor
{
    /// <summary>
    /// Build automation tools for Shakki.
    /// Provides menu items for building to different platforms.
    /// </summary>
    public static class BuildTools
    {
        private const string BuildFolder = "Builds";
        private const string GameName = "Shakki";

        [MenuItem("Shakki/Build/Android (APK)", false, 100)]
        public static void BuildAndroid()
        {
            BuildAndroidInternal(false);
        }

        [MenuItem("Shakki/Build/Android (AAB for Play Store)", false, 101)]
        public static void BuildAndroidAAB()
        {
            BuildAndroidInternal(true);
        }

        [MenuItem("Shakki/Build/iOS (Xcode Project)", false, 102)]
        public static void BuildiOS()
        {
            string path = GetBuildPath("iOS");

            var options = new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = path,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            ConfigureiOSSettings();
            PerformBuild(options, "iOS");
        }

        [MenuItem("Shakki/Build/Windows", false, 110)]
        public static void BuildWindows()
        {
            string path = GetBuildPath("Windows", $"{GameName}.exe");

            var options = new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            PerformBuild(options, "Windows");
        }

        [MenuItem("Shakki/Build/macOS", false, 111)]
        public static void BuildMacOS()
        {
            string path = GetBuildPath("macOS", $"{GameName}.app");

            var options = new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = path,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            PerformBuild(options, "macOS");
        }

        [MenuItem("Shakki/Build/All Mobile", false, 120)]
        public static void BuildAllMobile()
        {
            BuildAndroid();
            BuildiOS();
        }

        [MenuItem("Shakki/Build/Open Build Folder", false, 200)]
        public static void OpenBuildFolder()
        {
            string fullPath = Path.GetFullPath(BuildFolder);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            EditorUtility.RevealInFinder(fullPath);
        }

        private static void BuildAndroidInternal(bool useAAB)
        {
            string extension = useAAB ? ".aab" : ".apk";
            string path = GetBuildPath("Android", $"{GameName}{extension}");

            var options = new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            ConfigureAndroidSettings(useAAB);
            PerformBuild(options, "Android");
        }

        private static string[] GetScenePaths()
        {
            var scenes = new System.Collections.Generic.List<string>();

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            if (scenes.Count == 0)
            {
                // Fall back to current scene
                scenes.Add(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            }

            return scenes.ToArray();
        }

        private static string GetBuildPath(string platform, string filename = "")
        {
            string version = PlayerSettings.bundleVersion;
            string folder = Path.Combine(BuildFolder, platform, version);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return string.IsNullOrEmpty(filename) ? folder : Path.Combine(folder, filename);
        }

        private static void ConfigureAndroidSettings(bool useAAB)
        {
            // Set build format
            EditorUserBuildSettings.buildAppBundle = useAAB;

            // Ensure required settings (API 25 is minimum for modern features)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // ARM64 for modern devices
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Bundle identifier
            if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.shakki.chess");
            }

            Debug.Log($"[Build] Android configured: AAB={useAAB}, Min SDK={PlayerSettings.Android.minSdkVersion}");
        }

        private static void ConfigureiOSSettings()
        {
            // Bundle identifier
            if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS)))
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.shakki.chess");
            }

            // Minimum iOS version
            PlayerSettings.iOS.targetOSVersionString = "14.0";

            // Required capabilities
            PlayerSettings.iOS.cameraUsageDescription = "";
            PlayerSettings.iOS.microphoneUsageDescription = "";

            Debug.Log($"[Build] iOS configured: Target={PlayerSettings.iOS.targetOSVersionString}");
        }

        private static void PerformBuild(BuildPlayerOptions options, string platformName)
        {
            Debug.Log($"[Build] Starting {platformName} build...");
            Debug.Log($"[Build] Output: {options.locationPathName}");
            Debug.Log($"[Build] Scenes: {string.Join(", ", options.scenes)}");

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] {platformName} build succeeded!");
                Debug.Log($"[Build] Size: {report.summary.totalSize / (1024 * 1024):F2} MB");
                Debug.Log($"[Build] Time: {report.summary.totalTime.TotalSeconds:F1}s");

                EditorUtility.DisplayDialog("Build Complete",
                    $"{platformName} build completed successfully!\n\nOutput: {options.locationPathName}",
                    "OK");
            }
            else
            {
                Debug.LogError($"[Build] {platformName} build failed: {report.summary.result}");

                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Error)
                        {
                            Debug.LogError($"[Build Error] {message.content}");
                        }
                    }
                }

                EditorUtility.DisplayDialog("Build Failed",
                    $"{platformName} build failed. Check Console for details.",
                    "OK");
            }
        }

        [MenuItem("Shakki/Build/Configure Player Settings", false, 50)]
        public static void ConfigurePlayerSettings()
        {
            // Common settings
            PlayerSettings.productName = GameName;
            PlayerSettings.companyName = "Shakki Games";
            PlayerSettings.bundleVersion = "1.0.0";

            // Mobile orientation
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Icon background color
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.1f, 0.1f, 0.15f);

            // Android
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.shakki.chess");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

            // iOS
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.shakki.chess");
            PlayerSettings.iOS.targetOSVersionString = "14.0";

            Debug.Log("[Build] Player settings configured for Shakki");
            EditorUtility.DisplayDialog("Settings Configured",
                "Player settings have been configured for mobile builds.",
                "OK");
        }
    }
}
#endif
