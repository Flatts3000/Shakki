using System;
using UnityEngine;
using Unity.Netcode;
using Shakki.Core;
using Shakki.UI;

namespace Shakki.Network
{
    /// <summary>
    /// Controls the flow of networked matches.
    /// Handles run state sync, disconnections, and post-match flow.
    /// </summary>
    public class NetworkMatchController : NetworkBehaviour
    {
        public static NetworkMatchController Instance { get; private set; }

        // Network variables for player state
        private NetworkVariable<int> hostLevel = new NetworkVariable<int>(1);
        private NetworkVariable<int> clientLevel = new NetworkVariable<int>(1);
        private NetworkVariable<bool> hostReady = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> clientReady = new NetworkVariable<bool>(false);
        private NetworkVariable<NetworkMatchPhase> matchPhase = new NetworkVariable<NetworkMatchPhase>(NetworkMatchPhase.WaitingForPlayers);
        private NetworkVariable<bool> hostWantsRematch = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> clientWantsRematch = new NetworkVariable<bool>(false);

        // Events
        public event Action OnBothPlayersReady;
        public event Action<PieceColor> OnPlayerDisconnected;
        public event Action<NetworkMatchResult> OnMatchComplete;
        public event Action OnRematchAccepted;
        public event Action OnOpponentDeclinedRematch;

        public NetworkMatchPhase CurrentPhase => matchPhase.Value;
        public bool IsWaitingForRematchResponse => matchPhase.Value == NetworkMatchPhase.WaitingForRematch;

        private NetworkGameManager networkGameManager;
        private NetworkBoard networkBoard;
        private GameFlowController flowController;
        private RunManager runManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            networkGameManager = NetworkGameManager.Instance;
            networkBoard = NetworkBoard.Instance;
            flowController = GameFlowController.Instance;
            runManager = RunManager.Instance;

            // Subscribe to events
            matchPhase.OnValueChanged += HandlePhaseChanged;
            hostReady.OnValueChanged += CheckBothPlayersReady;
            clientReady.OnValueChanged += CheckBothPlayersReady;
            hostWantsRematch.OnValueChanged += CheckRematchConsensus;
            clientWantsRematch.OnValueChanged += CheckRematchConsensus;

            if (networkBoard != null)
            {
                networkBoard.OnMatchEnded += HandleNetworkMatchEnded;
            }

            if (networkGameManager != null)
            {
                networkGameManager.OnLocalPlayerAssigned += HandleLocalPlayerAssigned;
            }

            // Handle disconnections
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            }

            Debug.Log("[NetworkMatchController] Spawned");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            matchPhase.OnValueChanged -= HandlePhaseChanged;
            hostReady.OnValueChanged -= CheckBothPlayersReady;
            clientReady.OnValueChanged -= CheckBothPlayersReady;
            hostWantsRematch.OnValueChanged -= CheckRematchConsensus;
            clientWantsRematch.OnValueChanged -= CheckRematchConsensus;

            if (networkBoard != null)
            {
                networkBoard.OnMatchEnded -= HandleNetworkMatchEnded;
            }

            if (networkGameManager != null)
            {
                networkGameManager.OnLocalPlayerAssigned -= HandleLocalPlayerAssigned;
            }

            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }
        }

        private void HandleLocalPlayerAssigned(PieceColor color)
        {
            // When assigned a color, send our level to the server
            int level = runManager?.CurrentRun?.CurrentLevel ?? 1;
            SubmitPlayerLevelServerRpc(level);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitPlayerLevelServerRpc(int level, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                hostLevel.Value = level;
            }
            else
            {
                clientLevel.Value = level;
            }

            Debug.Log($"[NetworkMatchController] Player {clientId} level: {level}");
        }

        /// <summary>
        /// Signals that the local player is ready to play.
        /// </summary>
        public void SetReady()
        {
            SetReadyServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetReadyServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                hostReady.Value = true;
            }
            else
            {
                clientReady.Value = true;
            }
        }

        private void CheckBothPlayersReady(bool previous, bool current)
        {
            if (!IsServer) return;

            if (hostReady.Value && clientReady.Value)
            {
                matchPhase.Value = NetworkMatchPhase.Playing;
                OnBothPlayersReady?.Invoke();
            }
        }

        private void HandlePhaseChanged(NetworkMatchPhase previous, NetworkMatchPhase current)
        {
            Debug.Log($"[NetworkMatchController] Phase: {previous} -> {current}");

            if (current == NetworkMatchPhase.Playing)
            {
                // Both players ready - start the match
                OnBothPlayersReady?.Invoke();
            }
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (!IsServer) return;

            // Determine which player disconnected
            var disconnectedColor = networkGameManager.GetPlayerClientId(PieceColor.White) == clientId
                ? PieceColor.White
                : PieceColor.Black;

            Debug.Log($"[NetworkMatchController] Player disconnected: {disconnectedColor}");

            // If match was in progress, the other player wins
            if (matchPhase.Value == NetworkMatchPhase.Playing)
            {
                var result = disconnectedColor == PieceColor.White
                    ? NetworkMatchResult.BlackWins
                    : NetworkMatchResult.WhiteWins;

                matchPhase.Value = NetworkMatchPhase.Completed;
                NotifyDisconnectionClientRpc(disconnectedColor, result);
            }
        }

        [ClientRpc]
        private void NotifyDisconnectionClientRpc(PieceColor disconnectedColor, NetworkMatchResult result)
        {
            Debug.Log($"[NetworkMatchController] {disconnectedColor} disconnected. Result: {result}");
            OnPlayerDisconnected?.Invoke(disconnectedColor);
            OnMatchComplete?.Invoke(result);
        }

        private void HandleNetworkMatchEnded(NetworkMatchResult result)
        {
            if (!IsServer) return;

            matchPhase.Value = NetworkMatchPhase.Completed;

            // Reset rematch flags
            hostWantsRematch.Value = false;
            clientWantsRematch.Value = false;
        }

        /// <summary>
        /// Requests a rematch after the current match ends.
        /// </summary>
        public void RequestRematch()
        {
            RequestRematchServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestRematchServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                hostWantsRematch.Value = true;
            }
            else
            {
                clientWantsRematch.Value = true;
            }

            matchPhase.Value = NetworkMatchPhase.WaitingForRematch;
        }

        /// <summary>
        /// Declines a rematch request.
        /// </summary>
        public void DeclineRematch()
        {
            DeclineRematchServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void DeclineRematchServerRpc(ServerRpcParams rpcParams = default)
        {
            matchPhase.Value = NetworkMatchPhase.Ended;
            NotifyRematchDeclinedClientRpc();
        }

        [ClientRpc]
        private void NotifyRematchDeclinedClientRpc()
        {
            OnOpponentDeclinedRematch?.Invoke();
        }

        private void CheckRematchConsensus(bool previous, bool current)
        {
            if (!IsServer) return;

            if (hostWantsRematch.Value && clientWantsRematch.Value)
            {
                // Both want rematch - start new match
                StartRematch();
            }
        }

        private void StartRematch()
        {
            // Reset state
            hostReady.Value = false;
            clientReady.Value = false;
            hostWantsRematch.Value = false;
            clientWantsRematch.Value = false;
            matchPhase.Value = NetworkMatchPhase.WaitingForPlayers;

            // Notify clients
            NotifyRematchStartingClientRpc();
        }

        [ClientRpc]
        private void NotifyRematchStartingClientRpc()
        {
            OnRematchAccepted?.Invoke();
        }

        /// <summary>
        /// Gets the effective level for the match (average of both players).
        /// </summary>
        public int GetMatchLevel()
        {
            return Mathf.Max(hostLevel.Value, clientLevel.Value);
        }

        /// <summary>
        /// Checks if levels are compatible for a match.
        /// </summary>
        public bool AreLevelsCompatible(int bucketSize = 3)
        {
            return Mathf.Abs(hostLevel.Value - clientLevel.Value) <= bucketSize;
        }
    }

    public enum NetworkMatchPhase
    {
        WaitingForPlayers,
        Playing,
        Completed,
        WaitingForRematch,
        Ended
    }
}
