using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Shakki.Core;

namespace Shakki.Network
{
    /// <summary>
    /// Synchronizes chess board state across the network.
    /// Server is authoritative - clients send move requests, server validates and broadcasts.
    /// </summary>
    public class NetworkBoard : NetworkBehaviour
    {
        public static NetworkBoard Instance { get; private set; }

        // Network variables for match state
        private NetworkVariable<int> whiteScore = new NetworkVariable<int>(0);
        private NetworkVariable<int> blackScore = new NetworkVariable<int>(0);
        private NetworkVariable<int> currentRound = new NetworkVariable<int>(1);
        private NetworkVariable<PieceColor> currentTurn = new NetworkVariable<PieceColor>(PieceColor.White);
        private NetworkVariable<bool> matchInProgress = new NetworkVariable<bool>(false);
        private NetworkVariable<NetworkMatchResult> matchResult = new NetworkVariable<NetworkMatchResult>(NetworkMatchResult.InProgress);

        // Events for UI updates
        public event Action<int, int> OnScoresUpdated; // white, black
        public event Action<int> OnRoundUpdated;
        public event Action<PieceColor> OnTurnChanged;
        public event Action<Move> OnMoveExecuted;
        public event Action<NetworkMatchResult> OnMatchEnded;
        public event Action OnBoardStateReceived;

        // Local state (reconstructed from network)
        private ShakkiMatchState localMatchState;
        private ShakkiMatchConfig matchConfig;

        public int WhiteScore => whiteScore.Value;
        public int BlackScore => blackScore.Value;
        public int CurrentRound => currentRound.Value;
        public PieceColor CurrentTurn => currentTurn.Value;
        public bool MatchInProgress => matchInProgress.Value;
        public ShakkiMatchState LocalMatchState => localMatchState;

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

            whiteScore.OnValueChanged += (_, val) => OnScoresUpdated?.Invoke(val, blackScore.Value);
            blackScore.OnValueChanged += (_, val) => OnScoresUpdated?.Invoke(whiteScore.Value, val);
            currentRound.OnValueChanged += (_, val) => OnRoundUpdated?.Invoke(val);
            currentTurn.OnValueChanged += (_, val) => OnTurnChanged?.Invoke(val);
            matchResult.OnValueChanged += HandleMatchResultChanged;

            if (IsServer)
            {
                // Server initializes the match
                InitializeMatch();
            }
        }

        private void HandleMatchResultChanged(NetworkMatchResult previous, NetworkMatchResult current)
        {
            if (current != NetworkMatchResult.InProgress)
            {
                OnMatchEnded?.Invoke(current);
            }
        }

        /// <summary>
        /// Server: Initializes a new match.
        /// </summary>
        private void InitializeMatch()
        {
            if (!IsServer) return;

            matchConfig = ShakkiMatchConfig.Default;
            localMatchState = new ShakkiMatchState(matchConfig);

            whiteScore.Value = 0;
            blackScore.Value = 0;
            currentRound.Value = 1;
            currentTurn.Value = PieceColor.White;
            matchInProgress.Value = true;
            matchResult.Value = NetworkMatchResult.InProgress;

            // Sync initial board state to all clients
            SyncBoardStateClientRpc(SerializeBoard(localMatchState.Board));

            Debug.Log("[NetworkBoard] Match initialized on server");
        }

        /// <summary>
        /// Client: Request to make a move (sent to server for validation).
        /// </summary>
        public void RequestMove(Square from, Square to, PieceType promotion = PieceType.None)
        {
            if (!IsSpawned) return;

            RequestMoveServerRpc(
                from.File, from.Rank,
                to.File, to.Rank,
                (int)promotion
            );
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestMoveServerRpc(int fromFile, int fromRank, int toFile, int toRank, int promotion, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            var networkManager = NetworkGameManager.Instance;

            // Validate it's this player's turn
            PieceColor expectedColor = currentTurn.Value;
            ulong expectedClientId = networkManager.GetPlayerClientId(expectedColor);

            if (clientId != expectedClientId)
            {
                Debug.LogWarning($"[NetworkBoard] Client {clientId} tried to move but it's {expectedColor}'s turn (client {expectedClientId})");
                RejectMoveClientRpc(clientId, "Not your turn");
                return;
            }

            // Try to make the move
            var from = new Square(fromFile, fromRank);
            var to = new Square(toFile, toRank);
            var promotionType = (PieceType)promotion;

            var move = localMatchState.FindLegalMove(from, to, promotionType);
            if (!move.HasValue)
            {
                Debug.LogWarning($"[NetworkBoard] Invalid move from {from} to {to}");
                RejectMoveClientRpc(clientId, "Invalid move");
                return;
            }

            // Execute the move on server
            ExecuteMove(move.Value);
        }

        /// <summary>
        /// Server: Executes a validated move and broadcasts to all clients.
        /// </summary>
        private void ExecuteMove(Move move)
        {
            if (!IsServer) return;

            var capturedPiece = localMatchState.Board[move.To];
            bool wasCapture = !capturedPiece.IsEmpty;

            if (localMatchState.MakeMove(move))
            {
                // Update network variables
                whiteScore.Value = localMatchState.WhiteScore;
                blackScore.Value = localMatchState.BlackScore;
                currentRound.Value = localMatchState.CurrentRound;
                currentTurn.Value = localMatchState.CurrentPlayer;

                // Check for match end
                if (localMatchState.IsMatchOver)
                {
                    matchInProgress.Value = false;
                    matchResult.Value = ConvertResult(localMatchState.Result);
                }

                // Broadcast move to all clients
                BroadcastMoveClientRpc(
                    move.From.File, move.From.Rank,
                    move.To.File, move.To.Rank,
                    (int)move.Promotion,
                    (int)move.Flags
                );

                Debug.Log($"[NetworkBoard] Move executed: {move.From} -> {move.To}");
            }
        }

        [ClientRpc]
        private void BroadcastMoveClientRpc(int fromFile, int fromRank, int toFile, int toRank, int promotion, int flags)
        {
            var from = new Square(fromFile, fromRank);
            var to = new Square(toFile, toRank);
            var move = new Move(from, to, (PieceType)promotion, (MoveFlags)flags);

            // Update local state on clients (not server - already done)
            if (!IsServer)
            {
                localMatchState?.MakeMove(move);
            }

            OnMoveExecuted?.Invoke(move);
        }

        [ClientRpc]
        private void RejectMoveClientRpc(ulong targetClientId, string reason)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                Debug.LogWarning($"[NetworkBoard] Move rejected: {reason}");
            }
        }

        [ClientRpc]
        private void SyncBoardStateClientRpc(BoardData boardData)
        {
            if (!IsServer)
            {
                // Reconstruct match state from server data
                matchConfig = ShakkiMatchConfig.Default;
                localMatchState = new ShakkiMatchState(matchConfig);

                // Apply board state
                DeserializeBoard(boardData, localMatchState.Board);
            }

            OnBoardStateReceived?.Invoke();
            Debug.Log("[NetworkBoard] Board state synchronized");
        }

        /// <summary>
        /// Serializes board state for network transmission.
        /// </summary>
        private BoardData SerializeBoard(Board board)
        {
            var data = new BoardData();
            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    var piece = board[file, rank];
                    int index = file * 8 + rank;
                    data.pieces[index] = EncodePiece(piece);
                }
            }
            return data;
        }

        /// <summary>
        /// Deserializes board state from network data.
        /// </summary>
        private void DeserializeBoard(BoardData data, Board board)
        {
            board.Clear();
            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    int index = file * 8 + rank;
                    var piece = DecodePiece(data.pieces[index]);
                    board[file, rank] = piece;
                }
            }
        }

        private byte EncodePiece(Piece piece)
        {
            if (piece.IsEmpty) return 0;
            // Format: [color (1 bit)][type (3 bits)]
            int colorBit = piece.Color == PieceColor.White ? 0 : 8;
            int typeBits = (int)piece.Type;
            return (byte)(colorBit | typeBits);
        }

        private Piece DecodePiece(byte encoded)
        {
            if (encoded == 0) return Piece.None;
            var color = (encoded & 8) == 0 ? PieceColor.White : PieceColor.Black;
            var type = (PieceType)(encoded & 7);
            return new Piece(type, color);
        }

        private NetworkMatchResult ConvertResult(ShakkiMatchResult result)
        {
            return result switch
            {
                ShakkiMatchResult.WhiteWinsByScore => NetworkMatchResult.WhiteWins,
                ShakkiMatchResult.WhiteWinsByCheckmate => NetworkMatchResult.WhiteWins,
                ShakkiMatchResult.WhiteWinsByRoundLimit => NetworkMatchResult.WhiteWins,
                ShakkiMatchResult.BlackWinsByScore => NetworkMatchResult.BlackWins,
                ShakkiMatchResult.BlackWinsByCheckmate => NetworkMatchResult.BlackWins,
                ShakkiMatchResult.BlackWinsByRoundLimit => NetworkMatchResult.BlackWins,
                ShakkiMatchResult.Draw => NetworkMatchResult.Draw,
                _ => NetworkMatchResult.InProgress
            };
        }
    }

    /// <summary>
    /// Serializable board state for network transmission.
    /// </summary>
    public struct BoardData : INetworkSerializable
    {
        public byte[] pieces; // 64 bytes for 8x8 board

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                pieces = new byte[64];
            }
            else if (pieces == null)
            {
                pieces = new byte[64];
            }

            for (int i = 0; i < 64; i++)
            {
                serializer.SerializeValue(ref pieces[i]);
            }
        }
    }

    public enum NetworkMatchResult
    {
        InProgress,
        WhiteWins,
        BlackWins,
        Draw
    }
}
