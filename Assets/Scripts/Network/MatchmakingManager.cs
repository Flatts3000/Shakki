using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace Shakki.Network
{
    /// <summary>
    /// Manages matchmaking using Unity Gaming Services (Lobby + Relay).
    /// Matches players based on level buckets for fair play.
    /// </summary>
    public class MatchmakingManager : MonoBehaviour
    {
        public static MatchmakingManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int levelBucketSize = 3; // Players within 3 levels can match
        [SerializeField] private float lobbyHeartbeatInterval = 15f;
        [SerializeField] private float lobbyPollInterval = 2f;
        [SerializeField] private int maxPlayers = 2;

        // State
        private bool servicesInitialized = false;
        private Lobby currentLobby;
        private string playerId;
        private float heartbeatTimer;
        private float pollTimer;
        private bool isMatchmaking = false;

        // Events
        public event Action OnServicesReady;
        public event Action<string> OnError;
        public event Action OnMatchFound;
        public event Action OnMatchmakingStarted;
        public event Action OnMatchmakingCancelled;
        public event Action<MatchmakingState> OnStateChanged;

        public MatchmakingState CurrentState { get; private set; } = MatchmakingState.NotInitialized;
        public bool IsHost => currentLobby?.HostId == playerId;
        public int PlayerLevel { get; set; } = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private async void Start()
        {
            await InitializeServices();
        }

        private void Update()
        {
            if (currentLobby != null && IsHost)
            {
                // Send heartbeat to keep lobby alive
                heartbeatTimer -= Time.deltaTime;
                if (heartbeatTimer <= 0)
                {
                    heartbeatTimer = lobbyHeartbeatInterval;
                    SendHeartbeat();
                }
            }

            if (isMatchmaking && !IsHost)
            {
                // Poll for lobby updates
                pollTimer -= Time.deltaTime;
                if (pollTimer <= 0)
                {
                    pollTimer = lobbyPollInterval;
                    PollLobbyForUpdates();
                }
            }
        }

        private void OnDestroy()
        {
            LeaveLobby();
        }

        /// <summary>
        /// Initializes Unity Gaming Services.
        /// </summary>
        private async Task InitializeServices()
        {
            SetState(MatchmakingState.Initializing);

            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                playerId = AuthenticationService.Instance.PlayerId;
                servicesInitialized = true;

                SetState(MatchmakingState.Ready);
                OnServicesReady?.Invoke();

                Debug.Log($"[Matchmaking] Services initialized. PlayerId: {playerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Matchmaking] Failed to initialize services: {e.Message}");
                SetState(MatchmakingState.Error);
                OnError?.Invoke($"Failed to initialize: {e.Message}");
            }
        }

        /// <summary>
        /// Starts matchmaking - creates or joins a lobby based on level.
        /// </summary>
        public async void StartMatchmaking(int level)
        {
            if (!servicesInitialized)
            {
                OnError?.Invoke("Services not initialized");
                return;
            }

            PlayerLevel = level;
            isMatchmaking = true;
            SetState(MatchmakingState.Searching);
            OnMatchmakingStarted?.Invoke();

            try
            {
                // Try to find an existing lobby in our level bucket
                var lobby = await FindLobbyInBucket(level);

                if (lobby != null)
                {
                    // Join existing lobby
                    await JoinLobby(lobby);
                }
                else
                {
                    // Create new lobby
                    await CreateLobby(level);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Matchmaking] Error: {e.Message}");
                SetState(MatchmakingState.Error);
                OnError?.Invoke(e.Message);
                isMatchmaking = false;
            }
        }

        /// <summary>
        /// Cancels matchmaking and leaves any lobby.
        /// </summary>
        public async void CancelMatchmaking()
        {
            isMatchmaking = false;
            await LeaveLobby();
            SetState(MatchmakingState.Ready);
            OnMatchmakingCancelled?.Invoke();
        }

        /// <summary>
        /// Finds a lobby matching our level bucket.
        /// </summary>
        private async Task<Lobby> FindLobbyInBucket(int level)
        {
            int minLevel = Mathf.Max(1, level - levelBucketSize);
            int maxLevel = level + levelBucketSize;

            var queryOptions = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new QueryFilter(QueryFilter.FieldOptions.N1, minLevel.ToString(), QueryFilter.OpOptions.GE),
                    new QueryFilter(QueryFilter.FieldOptions.N1, maxLevel.ToString(), QueryFilter.OpOptions.LE)
                }
            };

            var response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            if (response.Results.Count > 0)
            {
                // Return the first available lobby
                Debug.Log($"[Matchmaking] Found {response.Results.Count} lobbies in level range {minLevel}-{maxLevel}");
                return response.Results[0];
            }

            Debug.Log($"[Matchmaking] No lobbies found in level range {minLevel}-{maxLevel}");
            return null;
        }

        /// <summary>
        /// Creates a new lobby and sets up Relay.
        /// </summary>
        private async Task CreateLobby(int level)
        {
            SetState(MatchmakingState.CreatingLobby);

            // Create Relay allocation
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Configure transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Create lobby with level info
            var lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                    { "Level", new DataObject(DataObject.VisibilityOptions.Public, level.ToString(), DataObject.IndexOptions.N1) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync($"Shakki_Level{level}", maxPlayers, lobbyOptions);
            heartbeatTimer = lobbyHeartbeatInterval;

            Debug.Log($"[Matchmaking] Created lobby {currentLobby.Id} with join code {joinCode}");

            SetState(MatchmakingState.WaitingForOpponent);

            // Start as host
            NetworkManager.Singleton.StartHost();
        }

        /// <summary>
        /// Joins an existing lobby via Relay.
        /// </summary>
        private async Task JoinLobby(Lobby lobby)
        {
            SetState(MatchmakingState.JoiningLobby);

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

            // Get join code from lobby data
            var joinCode = currentLobby.Data["JoinCode"].Value;

            // Join Relay
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Configure transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            Debug.Log($"[Matchmaking] Joined lobby {currentLobby.Id}");

            // Start as client
            NetworkManager.Singleton.StartClient();

            SetState(MatchmakingState.Connected);
            OnMatchFound?.Invoke();
        }

        /// <summary>
        /// Sends heartbeat to keep lobby alive.
        /// </summary>
        private async void SendHeartbeat()
        {
            if (currentLobby == null) return;

            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Matchmaking] Heartbeat failed: {e.Message}");
            }
        }

        /// <summary>
        /// Polls lobby for updates (e.g., opponent joined).
        /// </summary>
        private async void PollLobbyForUpdates()
        {
            if (currentLobby == null) return;

            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                // Check if game started (all players connected)
                if (currentLobby.Players.Count >= maxPlayers)
                {
                    isMatchmaking = false;
                    SetState(MatchmakingState.Connected);
                    OnMatchFound?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Matchmaking] Poll failed: {e.Message}");
            }
        }

        /// <summary>
        /// Leaves the current lobby.
        /// </summary>
        private async Task LeaveLobby()
        {
            if (currentLobby == null) return;

            try
            {
                if (IsHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Matchmaking] Leave lobby failed: {e.Message}");
            }

            currentLobby = null;
        }

        private void SetState(MatchmakingState state)
        {
            if (CurrentState != state)
            {
                CurrentState = state;
                OnStateChanged?.Invoke(state);
                Debug.Log($"[Matchmaking] State: {state}");
            }
        }

        /// <summary>
        /// Gets the level bucket range for display.
        /// </summary>
        public (int min, int max) GetLevelBucketRange(int level)
        {
            return (Mathf.Max(1, level - levelBucketSize), level + levelBucketSize);
        }
    }

    public enum MatchmakingState
    {
        NotInitialized,
        Initializing,
        Ready,
        Searching,
        CreatingLobby,
        WaitingForOpponent,
        JoiningLobby,
        Connected,
        Error
    }
}
