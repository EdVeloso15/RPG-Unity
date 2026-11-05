using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkSessionManager : MonoBehaviour
{
    public static NetworkSessionManager Instance { get; private set; }

    private enum SessionOperationState
    {
        Idle,
        Creating,
        Joining,
        Leaving
    }
    private ISession subscribedSession;

    private Action<string> playerJoinedHandler;
    private Action<string> playerHasLeftHandler;
    private Action playerPropertiesChangedHandler;
    private Action removedFromSessionHandler;

    private ISession finalizedSession;

    private SessionOperationState sessionOperationState =
        SessionOperationState.Idle;

    [Header("Session Settings")]
    [SerializeField, Min(1)]
    private int maxPlayers = 4;

    [SerializeField]
    private bool isPrivate = false;

    [SerializeField]
    private bool isLocked = false;

    [SerializeField]
    private string sessionName = "OnlineGame";

    [Header("Dependencies")]
    [SerializeField]
    private UGSInitializer ugsInitializer;

    public ISession CurrentSession { get; private set; }

    public bool IsInSession =>
        CurrentSession != null;

    public bool IsHost =>
        CurrentSession != null &&
        CurrentSession.IsHost;

    public string JoinCode =>
        CurrentSession?.Code ?? string.Empty;

    public string SessionName =>
        CurrentSession?.Name ?? sessionName;

    public int MaxPlayers =>
        CurrentSession?.MaxPlayers ?? maxPlayers;

    public int PlayerCount =>
        CurrentSession?.PlayerCount ?? 0;

    public string LastErrorMessage { get; private set; }

    public event Action LobbyChanged;
    public event Action SessionEnded;
    public event Action PlayerLeftSession;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    // ---------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------
    public Task<bool> CreateSessionAsync()
    {
        string playerName = "Player";

        if (ugsInitializer != null &&
            !string.IsNullOrWhiteSpace(
                ugsInitializer.CurrentPlayerName))
        {
            playerName =
                ugsInitializer.CurrentPlayerName;
        }

        return CreateSessionAsync(playerName);
    }
    public async Task<bool> CreateSessionAsync(
        string playerName)
    {
        if (!TryBeginSessionOperation(
                SessionOperationState.Creating))
        {
            return false;
        }

        try
        {
            return await CreateSessionOperationAsync(
                playerName
            );
        }
        finally
        {
            EndSessionOperation(
                SessionOperationState.Creating
            );
        }
    }
    private async Task<bool> CreateSessionOperationAsync(
        string playerName)
    {
        LastErrorMessage = string.Empty;

        if (IsInSession)
        {
            LastErrorMessage =
                "Você já está em uma partida.";

            return false;
        }

        if (!await EnsureServicesReadyAsync())
        {
            return false;
        }

        if (!await ugsInitializer.SetPlayerNameAsync(
                playerName))
        {
            LastErrorMessage =
                "Não foi possível definir o nome do jogador.";

            return false;
        }

        try
        {
            var options = new SessionOptions
            {
                Name = sessionName,
                Type = "Game",
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                IsLocked = isLocked,

                PlayerProperties = CreatePlayerProperties()
            }
            .WithRelayNetwork();

            CurrentSession =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);

            SubscribeToSession(CurrentSession);


            Debug.Log(
                $"[Network] Sessão criada. " +
                $"ID: {CurrentSession.Id} | " +
                $"Código: {CurrentSession.Code} | " +
                $"Máximo: {CurrentSession.MaxPlayers}"
            );

            NotifyLobbyChanged();

            return true;
        }
        catch (SessionException ex)
        {
            CurrentSession = null;

            Debug.LogError(
                $"[Network] Falha ao criar sessão.\n" +
                $"Error: {ex.Error}\n" +
                $"Message: {ex.Message}"
            );

            LastErrorMessage =
                "Não foi possível criar a partida.";

            return false;
        }
        catch (Exception ex)
        {
            CurrentSession = null;

            Debug.LogError(
                $"[Network] Erro inesperado ao criar sessão: {ex}"
            );

            LastErrorMessage =
                "Ocorreu um erro inesperado.";

            return false;
        }
    }

    // ---------------------------------------------------------
    // JOIN
    // ---------------------------------------------------------

    public Task<bool> JoinSessionAsync(
        string joinCode)
    {
        string playerName = "Player";

        if (ugsInitializer != null &&
            !string.IsNullOrWhiteSpace(
                ugsInitializer.CurrentPlayerName))
        {
            playerName =
                ugsInitializer.CurrentPlayerName;
        }

        return JoinSessionAsync(
            joinCode,
            playerName
        );
    }
    public async Task<bool> JoinSessionAsync(
        string joinCode,
        string playerName)
    {
        if (!TryBeginSessionOperation(
                SessionOperationState.Joining))
        {
            return false;
        }

        try
        {
            return await JoinSessionOperationAsync(
                joinCode,
                playerName
            );
        }
        finally
        {
            EndSessionOperation(
                SessionOperationState.Joining
            );
        }
    }
    private async Task<bool> JoinSessionOperationAsync(
        string joinCode,
        string playerName)
    {
        LastErrorMessage = string.Empty;

        if (IsInSession)
        {
            LastErrorMessage =
                "Você já está em uma partida.";

            return false;
        }

        joinCode =
            NormalizeJoinCode(joinCode);

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            LastErrorMessage =
                "Digite o código da partida.";

            return false;
        }

        if (!await EnsureServicesReadyAsync())
        {
            return false;
        }

        if (!await ugsInitializer.SetPlayerNameAsync(
                playerName))
        {
            LastErrorMessage =
                "Não foi possível definir o nome do jogador.";

            return false;
        }

        try
        {
            var joinOptions =
                new JoinSessionOptions
                {
                    PlayerProperties =
                        CreatePlayerProperties()
                };

            Debug.Log(
                "[LobbyDebug] Propriedades que serão enviadas no Join:"
            );

            foreach (
                var property
                in joinOptions.PlayerProperties)
            {
                Debug.Log(
                    $"[LobbyDebug] SEND | " +
                    $"Key='{property.Key}' | " +
                    $"Value='{property.Value.Value}'"
                );
            }

            LogNetworkState(
                "Antes de JoinSessionByCodeAsync"
            );
            
            LogTransportState("Antes de entrar na sessão");

            CurrentSession =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(
                        joinCode,
                        joinOptions
                    );
            
            LogNetworkState(
                "Depois de JoinSessionByCodeAsync"
            );

            SubscribeToSession(CurrentSession);

            Debug.Log(
                $"[Network] Entrou na sessão. " +
                $"ID: {CurrentSession.Id} | " +
                $"Código: {CurrentSession.Code}"
            );

            NotifyLobbyChanged();

            return true;
        }
        catch (SessionException ex)
        {
            CurrentSession = null;

            Debug.LogError(
                $"[Network] Falha ao entrar na sessão.\n" +
                $"Error: {ex.Error}\n" +
                $"Exceção completa:\n{ex}"
            );

            LogNetworkState(
                "Depois da falha em JoinSessionByCodeAsync"
            );

            LastErrorMessage =
                "Não foi possível entrar na partida. " +
                "Verifique o código e tente novamente.";

            return false;
        }
        catch (Exception ex)
        {
            CurrentSession = null;

            Debug.LogError(
                $"[Network] Erro inesperado ao entrar na sessão: {ex}"
            );

            LastErrorMessage =
                "Ocorreu um erro inesperado.";

            return false;
        }
    }

    // ---------------------------------------------------------
    // LEAVE
    // ---------------------------------------------------------

    public async Task LeaveSessionAsync()
    {
        if (!TryBeginSessionOperation(
                SessionOperationState.Leaving))
        {
            return;
        }

        try
        {
            await LeaveSessionOperationAsync();
        }
        finally
        {
            EndSessionOperation(
                SessionOperationState.Leaving
            );
        }
    }

    private async Task LeaveSessionOperationAsync()
    {
        if (CurrentSession == null)
        {
            SessionEnded?.Invoke();
            return;
        }

        ISession session = CurrentSession;

        UnsubscribeFromSession(session);

        try
        {
            await session.LeaveAsync();

            Debug.Log(
                "[Network] Sessão abandonada."
            );
        }
        catch (SessionException ex)
        {
            Debug.LogError(
                $"[Network] Falha ao sair da sessão: " +
                $"{ex.Error}"
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[Network] Erro inesperado ao sair: {ex}"
            );
        }
        finally
        {
            if (ReferenceEquals(CurrentSession, session))
            {
                FinalizeSession(session);
            }
        }
    }

    // ---------------------------------------------------------
    // LOBBY DATA
    // ---------------------------------------------------------

    public List<LobbyPlayerData> GetLobbyPlayers()
    {
        var result =
            new List<LobbyPlayerData>();

        if (CurrentSession == null)
        {
            return result;
        }

        int fallbackIndex = 1;

        foreach (var player in CurrentSession.Players)
        {
            Debug.Log(
                $"[LobbyDebug] ==============================="
            );

            Debug.Log(
                $"[LobbyDebug] PlayerId: {player.Id}"
            );

            Debug.Log(
                $"[LobbyDebug] GetPlayerName(): " +
                $"{player.GetPlayerName() ?? "<NULL>"}"
            );

            Debug.Log(
                $"[LobbyDebug] Quantidade de propriedades: " +
                $"{player.Properties.Count}"
            );

            foreach (var property in player.Properties)
            {
                Debug.Log(
                    $"[LobbyDebug] Property | " +
                    $"Key = '{property.Key}' | " +
                    $"Value = '{property.Value.Value}'"
                );
            }

            Debug.Log(
                $"[LobbyDebug] ==============================="
            );

            string playerName = "Player";

            if (player.Properties.TryGetValue(
                    "playerName",
                    out var playerNameProperty))
            {
                playerName =
                    playerNameProperty.Value;
            }

            Debug.Log(
                $"[Lobby] PlayerId={player.Id} | " +
                $"PlayerName={playerName ?? "<NULL>"}"
            );

            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName =
                    $"Player {fallbackIndex}";
            }

            bool isReady =
                IsPlayerReady(player);

            bool isHost =
                player.Id ==
                CurrentSession.Host;

            bool isLocal =
                player.Id ==
                CurrentSession.CurrentPlayer.Id;


            result.Add(
                new LobbyPlayerData
                {
                    PlayerId = player.Id,
                    DisplayName = playerName,
                    IsHost = isHost,
                    IsLocalPlayer = isLocal,
                    IsReady = isReady
                }
            );

            fallbackIndex++;
        }

        // Host primeiro.
        result.Sort(
            (a, b) =>
            {
                if (a.IsHost && !b.IsHost)
                    return -1;

                if (!a.IsHost && b.IsHost)
                    return 1;

                return string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase
                );
            }
        );

        return result;
    }

    // ---------------------------------------------------------
    // EVENTS
    // ---------------------------------------------------------

    private void SubscribeToSession(ISession session)
    {
        if (session == null)
            return;

        // A mesma sessão já está inscrita.
        if (ReferenceEquals(subscribedSession, session))
            return;

        // Remove inscrições anteriores, se existirem.
        if (subscribedSession != null)
        {
            UnsubscribeFromSession(subscribedSession);
        }

        finalizedSession = null;

        subscribedSession = session;

        // Os delegates capturam a sessão de origem.
        playerJoinedHandler =
            playerId => OnPlayerJoined(session, playerId);

        playerHasLeftHandler =
            playerId => OnPlayerHasLeft(session, playerId);

        playerPropertiesChangedHandler =
            () => OnPlayerPropertiesChanged(session);

        removedFromSessionHandler =
            () => OnRemovedFromSession(session);

        session.PlayerJoined += playerJoinedHandler;
        session.PlayerHasLeft += playerHasLeftHandler;
        session.PlayerPropertiesChanged += playerPropertiesChangedHandler;
        session.RemovedFromSession += removedFromSessionHandler;
    }

    private void UnsubscribeFromSession(ISession session)
    {
        if (session == null)
            return;

        // Só podemos remover os delegates que pertencem
        // à sessão atualmente rastreada.
        if (!ReferenceEquals(subscribedSession, session))
            return;

        session.PlayerJoined -= playerJoinedHandler;
        session.PlayerHasLeft -= playerHasLeftHandler;
        session.PlayerPropertiesChanged -= playerPropertiesChangedHandler;
        session.RemovedFromSession -= removedFromSessionHandler;

        subscribedSession = null;

        playerJoinedHandler = null;
        playerHasLeftHandler = null;
        playerPropertiesChangedHandler = null;
        removedFromSessionHandler = null;
    }

    private void OnPlayerJoined(
    ISession session,
    string playerId)
    {
        if (!IsActiveSessionEvent(session))
            return;

        Debug.Log(
            $"[Lobby] Jogador entrou: {playerId}"
        );

        NotifyLobbyChanged();
    }

    private void OnPlayerHasLeft(
    ISession session,
    string playerId)
    {
        if (!IsActiveSessionEvent(session))
            return;

        Debug.Log(
            $"[Lobby] Jogador saiu: {playerId}"
        );

        // Preserva o comportamento atual:
        // cancelar Countdown e redefinir Ready local.
        PlayerLeftSession?.Invoke();

        NotifyLobbyChanged();
    }

    private void OnPlayerPropertiesChanged(
    ISession session)
    {
        if (!IsActiveSessionEvent(session))
            return;

        if (CurrentSession != null)
        {
            foreach (var player in CurrentSession.Players)
            {
                string playerName = "<NULL>";
                string readyValue = "<NULL>";

                if (player.Properties.TryGetValue(
                        "playerName",
                        out var nameProperty))
                {
                    playerName =
                        nameProperty.Value;
                }

                if (player.Properties.TryGetValue(
                        "ready",
                        out var readyProperty))
                {
                    readyValue =
                        readyProperty.Value;
                }

                Debug.Log(
                    $"[Lobby] Player={playerName} | " +
                    $"ID={player.Id} | " +
                    $"Ready={readyValue}"
                );
            }
        }

        NotifyLobbyChanged();
    }

    private void OnRemovedFromSession(ISession session)
    {
        if (!ReferenceEquals(session, CurrentSession))
            return;

        Debug.LogWarning(
            "[Lobby] Este jogador foi removido da sessão."
        );

        FinalizeSession(session);
    }

    private void NotifyLobbyChanged()
    {
        LobbyChanged?.Invoke();
    }

    public async Task<bool> SetReadyAsync(bool ready)
    {
        if (CurrentSession == null)
        {
            LastErrorMessage =
                "Você não está em uma sessão.";

            return false;
        }

        try
        {
            CurrentSession.CurrentPlayer.SetProperty(
                "ready",
                new PlayerProperty(
                    ready.ToString().ToLowerInvariant(),
                    VisibilityPropertyOptions.Member
                )
            );

            await CurrentSession.SaveCurrentPlayerDataAsync();

            Debug.Log(
                $"[Lobby] Ready alterado para: {ready}"
            );

            return true;
        }
        catch (SessionException ex)
        {
            Debug.LogError(
                $"[Lobby] Falha ao alterar Ready.\n" +
                $"Error: {ex.Error}\n" +
                $"Message: {ex.Message}"
            );

            LastErrorMessage =
                "Não foi possível alterar o estado de pronto.";

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[Lobby] Erro inesperado ao alterar Ready: {ex}"
            );

            LastErrorMessage =
                "Ocorreu um erro inesperado.";

            return false;
        }
    }

    public bool IsPlayerReady(
    IReadOnlyPlayer player)
    {
        if (player == null)
        {
            return false;
        }

        if (!player.Properties.TryGetValue(
                "ready",
                out var readyProperty))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                readyProperty.Value))
        {
            return false;
        }

        return bool.TryParse(
            readyProperty.Value,
            out bool ready
        ) && ready;
    }
    private bool FinalizeSession(ISession session)
    {
        if (session == null)
            return false;

        // Uma sessão antiga não pode finalizar a atual.
        if (!ReferenceEquals(CurrentSession, session))
        {
            UnsubscribeFromSession(session);
            return false;
        }

        // Impede finalização duplicada.
        if (ReferenceEquals(finalizedSession, session))
            return false;

        finalizedSession = session;

        UnsubscribeFromSession(session);

        CurrentSession = null;

        LobbyChanged?.Invoke();
        SessionEnded?.Invoke();

        return true;
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private async Task<bool> EnsureServicesReadyAsync()
    {
        if (ugsInitializer == null)
        {
            LastErrorMessage =
                "UGSInitializer não está configurado.";

            Debug.LogError(
                "[Network] UGSInitializer não atribuído."
            );

            return false;
        }

        if (!ugsInitializer.IsReady)
        {
            await ugsInitializer.InitializeAsync();
        }

        if (!ugsInitializer.IsReady)
        {
            LastErrorMessage =
                "Os serviços online não estão disponíveis.";

            return false;
        }

        return true;
    }

    private string NormalizeJoinCode(
        string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return code
            .Trim()
            .Replace(" ", "")
            .ToUpperInvariant();
    }

    private Dictionary<string, PlayerProperty>
    CreatePlayerProperties()
    {
        string playerName = "Player";

        if (ugsInitializer != null)
        {
            playerName =
                ugsInitializer.CurrentPlayerName;
        }

        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player";
        }

        return new Dictionary<string, PlayerProperty>
        {
            {
                "playerName",
                new PlayerProperty(
                    playerName,
                    VisibilityPropertyOptions.Member
                )
            },
            {
                "ready",
                new PlayerProperty(
                    "false",
                    VisibilityPropertyOptions.Member
                )
            }
        };
    }
    private void LogNetworkState(string context)
    {
        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogWarning(
                $"[NetworkDiagnostic] {context} | " +
                "NetworkManager.Singleton = null"
            );

            return;
        }

        Debug.Log(
            $"[NetworkDiagnostic] {context} | " +
            $"IsListening={networkManager.IsListening} | " +
            $"IsClient={networkManager.IsClient} | " +
            $"IsServer={networkManager.IsServer} | " +
            $"IsHost={networkManager.IsHost}"
        );
    }

    private void LogTransportState(string context)
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null)
        {
            Debug.LogError(
                $"[NetworkDiagnostic] {context} | " +
                "NetworkManager.Singleton = null"
            );
            return;
        }

        Debug.Log(
            $"[NetworkDiagnostic] {context} | " +
            $"NetworkManagerId={nm.GetEntityId()} | " +
            $"IsListening={nm.IsListening} | " +
            $"IsClient={nm.IsClient} | " +
            $"IsServer={nm.IsServer} | " +
            $"IsHost={nm.IsHost} | " +
            $"Transport={nm.NetworkConfig.NetworkTransport}"
        );

        UnityTransport transport =
            nm.NetworkConfig.NetworkTransport as UnityTransport;

        Debug.Log(
            $"[NetworkDiagnostic] {context} | " +
            $"UnityTransport encontrado={transport != null}"
        );
    }
    private bool TryBeginSessionOperation(
    SessionOperationState requestedState)
    {
        if (sessionOperationState != SessionOperationState.Idle)
        {
            LastErrorMessage =
                "Já existe uma operação de sessão em andamento.";

            Debug.LogWarning(
                $"[Network] Operação recusada. " +
                $"Estado atual: {sessionOperationState} | " +
                $"Solicitado: {requestedState}"
            );

            return false;
        }

        sessionOperationState = requestedState;

        return true;
    }

    private void EndSessionOperation(
        SessionOperationState completedState)
    {
        if (sessionOperationState != completedState)
        {
            Debug.LogWarning(
                $"[Network] Tentativa de finalizar uma operação " +
                $"que não corresponde ao estado atual. " +
                $"Atual: {sessionOperationState} | " +
                $"Finalizada: {completedState}"
            );

            return;
        }

        sessionOperationState = SessionOperationState.Idle;
    }
    private bool IsActiveSessionEvent(ISession session)
    {
        return session != null &&
               ReferenceEquals(session, CurrentSession) &&
               ReferenceEquals(session, subscribedSession);
    }
}


[Serializable]
public class LobbyPlayerData
{
    public string PlayerId;
    public string DisplayName;
    public bool IsHost;
    public bool IsLocalPlayer;
    public bool IsReady;
}