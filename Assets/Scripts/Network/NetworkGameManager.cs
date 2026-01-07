using System;
using UnityEngine;
using Unity.Netcode;
using Shakki.Core;

namespace Shakki.Network
{
    /// <summary>
    /// Manages networked chess matches using Unity Netcode.
    /// Handles host/client logic, player connections, and game state coordination.
    /// </summary>
    public class NetworkGameManager : NetworkBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }

        // Network variables for game state
        private NetworkVariable<GamePhase> currentPhase = new NetworkVariable<GamePhase>(
            GamePhase.WaitingForPlayers,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<ulong> whitePlayerId = new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<ulong> blackPlayerId = new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Events
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<string> OnConnectionError;
        public event Action OnGameReady;
        public event Action<PieceColor> OnLocalPlayerAssigned;

        public GamePhase CurrentPhase => currentPhase.Value;
        public bool IsHost => NetworkManager.Singleton?.IsHost ?? false;
        public bool IsConnected => NetworkManager.Singleton?.IsConnectedClient ?? false;
        public PieceColor? LocalPlayerColor { get; private set; }

        private NetworkBoard networkBoard;

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

            currentPhase.OnValueChanged += HandlePhaseChanged;

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }

            // Find or create NetworkBoard
            networkBoard = FindFirstObjectByType<NetworkBoard>();

            Debug.Log($"[Network] Spawned as {(IsHost ? "Host" : "Client")}. LocalClientId: {NetworkManager.Singleton.LocalClientId}");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            currentPhase.OnValueChanged -= HandlePhaseChanged;

            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void HandlePhaseChanged(GamePhase previous, GamePhase current)
        {
            Debug.Log($"[Network] Phase changed: {previous} -> {current}");
            OnPhaseChanged?.Invoke(current);

            if (current == GamePhase.Playing)
            {
                OnGameReady?.Invoke();
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            Debug.Log($"[Network] Client connected: {clientId}");

            if (!IsServer) return;

            // Assign player to a color
            if (whitePlayerId.Value == ulong.MaxValue)
            {
                whitePlayerId.Value = clientId;
                NotifyPlayerColorClientRpc(clientId, true);
                Debug.Log($"[Network] Assigned client {clientId} to White");
            }
            else if (blackPlayerId.Value == ulong.MaxValue)
            {
                blackPlayerId.Value = clientId;
                NotifyPlayerColorClientRpc(clientId, false);
                Debug.Log($"[Network] Assigned client {clientId} to Black");

                // Both players connected - start game
                currentPhase.Value = GamePhase.Playing;
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            Debug.Log($"[Network] Client disconnected: {clientId}");

            if (!IsServer) return;

            // Handle disconnection during game
            if (currentPhase.Value == GamePhase.Playing)
            {
                // Player disconnected - other player wins
                if (clientId == whitePlayerId.Value)
                {
                    currentPhase.Value = GamePhase.BlackWins;
                }
                else if (clientId == blackPlayerId.Value)
                {
                    currentPhase.Value = GamePhase.WhiteWins;
                }
            }

            // Reset player assignments if in waiting state
            if (currentPhase.Value == GamePhase.WaitingForPlayers)
            {
                if (clientId == whitePlayerId.Value)
                    whitePlayerId.Value = ulong.MaxValue;
                if (clientId == blackPlayerId.Value)
                    blackPlayerId.Value = ulong.MaxValue;
            }
        }

        [ClientRpc]
        private void NotifyPlayerColorClientRpc(ulong targetClientId, bool isWhite)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                LocalPlayerColor = isWhite ? PieceColor.White : PieceColor.Black;
                Debug.Log($"[Network] Local player assigned: {LocalPlayerColor}");
                OnLocalPlayerAssigned?.Invoke(LocalPlayerColor.Value);
            }
        }

        /// <summary>
        /// Starts hosting a game.
        /// </summary>
        public void StartHost()
        {
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("[Network] Started hosting");
            }
            else
            {
                OnConnectionError?.Invoke("Failed to start host");
            }
        }

        /// <summary>
        /// Joins a game as client.
        /// </summary>
        public void StartClient(string address = "127.0.0.1", ushort port = 7777)
        {
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(address, port);
            }

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log($"[Network] Connecting to {address}:{port}");
            }
            else
            {
                OnConnectionError?.Invoke("Failed to start client");
            }
        }

        /// <summary>
        /// Disconnects from the current game.
        /// </summary>
        public void Disconnect()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                LocalPlayerColor = null;
            }
        }

        /// <summary>
        /// Checks if it's the local player's turn.
        /// </summary>
        public bool IsLocalPlayerTurn(PieceColor currentTurn)
        {
            return LocalPlayerColor.HasValue && LocalPlayerColor.Value == currentTurn;
        }

        /// <summary>
        /// Gets the client ID for a specific color.
        /// </summary>
        public ulong GetPlayerClientId(PieceColor color)
        {
            return color == PieceColor.White ? whitePlayerId.Value : blackPlayerId.Value;
        }
    }

    public enum GamePhase
    {
        WaitingForPlayers,
        Playing,
        WhiteWins,
        BlackWins,
        Draw,
        Disconnected
    }
}
