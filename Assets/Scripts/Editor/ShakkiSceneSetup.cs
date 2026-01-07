#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Netcode;
using Shakki.Core;
using Shakki.UI;
using Shakki.Network;
using Shakki.Audio;

namespace Shakki.Editor
{
    public static class ShakkiSceneSetup
    {
        [MenuItem("Shakki/Setup Scene", false, 1)]
        public static void SetupScene()
        {
            // Check if already set up
            var existingGM = Object.FindFirstObjectByType<GameManager>();
            if (existingGM != null)
            {
                EditorUtility.DisplayDialog("Already Set Up",
                    "Shakki scene is already set up. Use 'Shakki > Reset Scene' first if you want to recreate it.",
                    "OK");
                Selection.activeGameObject = existingGM.gameObject;
                return;
            }

            // Confirm with user
            if (!EditorUtility.DisplayDialog("Setup Shakki Scene",
                "This will add Shakki game objects to the current scene. Continue?",
                "Yes", "Cancel"))
            {
                return;
            }

            // Create GameController root object (holds all game systems)
            var gameControllerObj = new GameObject("GameController");
            Undo.RegisterCreatedObjectUndo(gameControllerObj, "Create GameController");

            // Add RunManager (manages run state and level progression)
            var runManager = gameControllerObj.AddComponent<RunManager>();

            // Add GameFlowController (manages game states)
            var flowController = gameControllerObj.AddComponent<GameFlowController>();

            // Create GameManager
            var gameManagerObj = new GameObject("GameManager");
            gameManagerObj.transform.SetParent(gameControllerObj.transform);
            var gameManager = gameManagerObj.AddComponent<GameManager>();
            gameManagerObj.AddComponent<PieceSpriteLoader>(); // Load sprites from Resources
            Undo.RegisterCreatedObjectUndo(gameManagerObj, "Create GameManager");

            // Create Board as child
            var boardObj = new GameObject("Board");
            boardObj.transform.SetParent(gameManagerObj.transform);
            var boardView = boardObj.AddComponent<BoardView>();
            Undo.RegisterCreatedObjectUndo(boardObj, "Create Board");

            // Create UI Manager
            var uiManagerObj = new GameObject("UIManager");
            uiManagerObj.transform.SetParent(gameControllerObj.transform);
            uiManagerObj.AddComponent<GameUIManager>();
            Undo.RegisterCreatedObjectUndo(uiManagerObj, "Create UIManager");

            // Create Level Info HUD
            var levelHudObj = new GameObject("LevelInfoHUD");
            levelHudObj.transform.SetParent(gameControllerObj.transform);
            levelHudObj.AddComponent<LevelInfoHUD>();
            Undo.RegisterCreatedObjectUndo(levelHudObj, "Create LevelInfoHUD");

            // Create ShopManager
            var shopManagerObj = new GameObject("ShopManager");
            shopManagerObj.transform.SetParent(gameControllerObj.transform);
            shopManagerObj.AddComponent<ShopManager>();
            Undo.RegisterCreatedObjectUndo(shopManagerObj, "Create ShopManager");

            // Create ShopScreen
            var shopScreenObj = new GameObject("ShopScreen");
            shopScreenObj.transform.SetParent(gameControllerObj.transform);
            shopScreenObj.AddComponent<ShopScreen>();
            Undo.RegisterCreatedObjectUndo(shopScreenObj, "Create ShopScreen");

            // Create SceneTransitionManager
            var transitionObj = new GameObject("SceneTransitionManager");
            transitionObj.transform.SetParent(gameControllerObj.transform);
            transitionObj.AddComponent<SceneTransitionManager>();
            Undo.RegisterCreatedObjectUndo(transitionObj, "Create SceneTransitionManager");

            // Create AudioManager (DontDestroyOnLoad - standalone)
            var audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();
            audioObj.AddComponent<GameAudioHooks>();
            Undo.RegisterCreatedObjectUndo(audioObj, "Create AudioManager");

            // Create PerformanceOptimizer (DontDestroyOnLoad - standalone)
            var perfObj = new GameObject("PerformanceOptimizer");
            perfObj.AddComponent<PerformanceOptimizer>();
            Undo.RegisterCreatedObjectUndo(perfObj, "Create PerformanceOptimizer");

            // Create NetworkManager (required for multiplayer)
            var existingNetworkManager = Object.FindFirstObjectByType<NetworkManager>();
            if (existingNetworkManager == null)
            {
                var networkManagerObj = new GameObject("NetworkManager");
                var networkManager = networkManagerObj.AddComponent<NetworkManager>();

                // Add Unity Transport
                var transport = networkManagerObj.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                networkManager.NetworkConfig = new NetworkConfig();

                // Add our network components (NetworkBehaviours must be on NetworkManager's object)
                networkManagerObj.AddComponent<NetworkGameManager>();
                networkManagerObj.AddComponent<NetworkBoard>();
                networkManagerObj.AddComponent<NetworkMatchController>();

                Undo.RegisterCreatedObjectUndo(networkManagerObj, "Create NetworkManager");
            }

            // Create MatchmakingManager (standalone, uses Unity Gaming Services)
            var matchmakingObj = new GameObject("MatchmakingManager");
            matchmakingObj.transform.SetParent(gameControllerObj.transform);
            matchmakingObj.AddComponent<MatchmakingManager>();
            Undo.RegisterCreatedObjectUndo(matchmakingObj, "Create MatchmakingManager");

            // Create MatchmakingUI
            var matchmakingUIObj = new GameObject("MatchmakingUI");
            matchmakingUIObj.transform.SetParent(gameControllerObj.transform);
            matchmakingUIObj.AddComponent<MatchmakingUI>();
            Undo.RegisterCreatedObjectUndo(matchmakingUIObj, "Create MatchmakingUI");

            // Create LobbyUI
            var lobbyUIObj = new GameObject("LobbyUI");
            lobbyUIObj.transform.SetParent(gameControllerObj.transform);
            lobbyUIObj.AddComponent<LobbyUI>();
            Undo.RegisterCreatedObjectUndo(lobbyUIObj, "Create LobbyUI");

            // Create NetworkPostMatchUI
            var postMatchUIObj = new GameObject("NetworkPostMatchUI");
            postMatchUIObj.transform.SetParent(gameControllerObj.transform);
            postMatchUIObj.AddComponent<NetworkPostMatchUI>();
            Undo.RegisterCreatedObjectUndo(postMatchUIObj, "Create NetworkPostMatchUI");

            // Create EventSystem if one doesn't exist (required for UI interaction)
            var existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (existingEventSystem == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
            }

            // Link references via SerializedObject
            var serializedGM = new SerializedObject(gameManager);
            serializedGM.FindProperty("boardView").objectReferenceValue = boardView;
            serializedGM.FindProperty("runManager").objectReferenceValue = runManager;
            serializedGM.FindProperty("flowController").objectReferenceValue = flowController;
            serializedGM.ApplyModifiedProperties();

            // Link RunManager to FlowController
            var serializedFlow = new SerializedObject(flowController);
            serializedFlow.FindProperty("runManager").objectReferenceValue = runManager;
            serializedFlow.ApplyModifiedProperties();

            // Setup Camera
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Undo.RecordObject(mainCamera.transform, "Setup Camera");
                Undo.RecordObject(mainCamera, "Setup Camera");

                mainCamera.transform.position = new Vector3(0, 0, -10);
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 5f;
                mainCamera.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            }
            else
            {
                Debug.LogWarning("No Main Camera found. Please set up camera manually.");
            }

            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("Shakki scene setup complete! Enter Play mode to test.");

            // Select the GameController
            Selection.activeGameObject = gameControllerObj;
        }

        [MenuItem("Shakki/Setup Scene", true)]
        public static bool ValidateSetupScene()
        {
            // Don't allow in play mode
            return !Application.isPlaying;
        }

        [MenuItem("Shakki/Reset Scene", false, 2)]
        public static void ResetScene()
        {
            // Count what we'll remove
            var runManagers = Object.FindObjectsByType<RunManager>(FindObjectsSortMode.None);
            var flowControllers = Object.FindObjectsByType<GameFlowController>(FindObjectsSortMode.None);
            var gameManagers = Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None);
            var boards = Object.FindObjectsByType<BoardView>(FindObjectsSortMode.None);
            var huds = Object.FindObjectsByType<GameHUD>(FindObjectsSortMode.None);
            var uiManagers = Object.FindObjectsByType<GameUIManager>(FindObjectsSortMode.None);
            var levelHuds = Object.FindObjectsByType<LevelInfoHUD>(FindObjectsSortMode.None);
            var shopManagers = Object.FindObjectsByType<ShopManager>(FindObjectsSortMode.None);
            var shopScreens = Object.FindObjectsByType<ShopScreen>(FindObjectsSortMode.None);
            var networkManagers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
            var lobbyUIs = Object.FindObjectsByType<LobbyUI>(FindObjectsSortMode.None);
            var matchmakingManagers = Object.FindObjectsByType<MatchmakingManager>(FindObjectsSortMode.None);
            var matchmakingUIs = Object.FindObjectsByType<MatchmakingUI>(FindObjectsSortMode.None);
            var postMatchUIs = Object.FindObjectsByType<NetworkPostMatchUI>(FindObjectsSortMode.None);
            var audioManagers = Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None);
            var transitionManagers = Object.FindObjectsByType<SceneTransitionManager>(FindObjectsSortMode.None);
            var perfOptimizers = Object.FindObjectsByType<PerformanceOptimizer>(FindObjectsSortMode.None);

            int totalCount = runManagers.Length + flowControllers.Length + gameManagers.Length +
                             boards.Length + huds.Length + uiManagers.Length + levelHuds.Length +
                             shopManagers.Length + shopScreens.Length + networkManagers.Length + lobbyUIs.Length +
                             matchmakingManagers.Length + matchmakingUIs.Length + postMatchUIs.Length +
                             audioManagers.Length + transitionManagers.Length + perfOptimizers.Length;

            if (totalCount == 0)
            {
                EditorUtility.DisplayDialog("Nothing to Reset",
                    "No Shakki game objects found in the scene.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Reset Shakki Scene",
                $"This will remove {totalCount} Shakki game object(s). Continue?",
                "Yes", "Cancel"))
            {
                return;
            }

            // Destroy RunManagers first (they're root objects now)
            foreach (var rm in runManagers)
            {
                if (rm != null)
                    Undo.DestroyObjectImmediate(rm.gameObject);
            }

            // Find and destroy ALL GameManagers
            foreach (var gm in gameManagers)
            {
                if (gm != null && gm.gameObject != null)
                    Undo.DestroyObjectImmediate(gm.gameObject);
            }

            // Find and destroy any orphaned boards
            foreach (var board in boards)
            {
                if (board != null && board.gameObject != null)
                    Undo.DestroyObjectImmediate(board.gameObject);
            }

            // Find and destroy any orphaned HUDs
            foreach (var hud in huds)
            {
                if (hud != null && hud.gameObject != null)
                    Undo.DestroyObjectImmediate(hud.gameObject);
            }

            // Find and destroy UI managers
            foreach (var ui in uiManagers)
            {
                if (ui != null && ui.gameObject != null)
                    Undo.DestroyObjectImmediate(ui.gameObject);
            }

            // Find and destroy level HUDs
            foreach (var lh in levelHuds)
            {
                if (lh != null && lh.gameObject != null)
                    Undo.DestroyObjectImmediate(lh.gameObject);
            }

            // Find and destroy flow controllers
            foreach (var fc in flowControllers)
            {
                if (fc != null && fc.gameObject != null)
                    Undo.DestroyObjectImmediate(fc.gameObject);
            }

            // Find and destroy shop managers
            foreach (var sm in shopManagers)
            {
                if (sm != null && sm.gameObject != null)
                    Undo.DestroyObjectImmediate(sm.gameObject);
            }

            // Find and destroy shop screens
            foreach (var ss in shopScreens)
            {
                if (ss != null && ss.gameObject != null)
                    Undo.DestroyObjectImmediate(ss.gameObject);
            }

            // Find and destroy network managers
            foreach (var nm in networkManagers)
            {
                if (nm != null && nm.gameObject != null)
                    Undo.DestroyObjectImmediate(nm.gameObject);
            }

            // Find and destroy lobby UIs
            foreach (var lu in lobbyUIs)
            {
                if (lu != null && lu.gameObject != null)
                    Undo.DestroyObjectImmediate(lu.gameObject);
            }

            // Find and destroy matchmaking managers
            foreach (var mm in matchmakingManagers)
            {
                if (mm != null && mm.gameObject != null)
                    Undo.DestroyObjectImmediate(mm.gameObject);
            }

            // Find and destroy matchmaking UIs
            foreach (var mui in matchmakingUIs)
            {
                if (mui != null && mui.gameObject != null)
                    Undo.DestroyObjectImmediate(mui.gameObject);
            }

            // Find and destroy post match UIs
            foreach (var pmu in postMatchUIs)
            {
                if (pmu != null && pmu.gameObject != null)
                    Undo.DestroyObjectImmediate(pmu.gameObject);
            }

            // Find and destroy audio managers
            foreach (var am in audioManagers)
            {
                if (am != null && am.gameObject != null)
                    Undo.DestroyObjectImmediate(am.gameObject);
            }

            // Find and destroy transition managers
            foreach (var tm in transitionManagers)
            {
                if (tm != null && tm.gameObject != null)
                    Undo.DestroyObjectImmediate(tm.gameObject);
            }

            // Find and destroy performance optimizers
            foreach (var po in perfOptimizers)
            {
                if (po != null && po.gameObject != null)
                    Undo.DestroyObjectImmediate(po.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Shakki scene reset complete. Removed {totalCount} object(s).");
        }
    }
}
#endif
