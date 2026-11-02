using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class LobbyUI : MonoBehaviour
{
    [Header("Network")]
    [SerializeField]
    private NetworkSessionManager sessionManager;

    // ---------------------------------------------------------
    // Panel Renderer
    // ---------------------------------------------------------

    private PanelRenderer panelRenderer;

    private int uiVersion = -1;

    // ---------------------------------------------------------
    // Root
    // ---------------------------------------------------------

    private VisualElement root;

    // ---------------------------------------------------------
    // Main Menu
    // ---------------------------------------------------------

    private VisualElement mainMenuPanel;

    private TextField playerNameInput;
    private TextField joinCodeInput;

    private Button createButton;
    private Button joinButton;

    private Label mainStatus;

    // ---------------------------------------------------------
    // Lobby
    // ---------------------------------------------------------

    private VisualElement lobbyPanel;

    private Label sessionName;
    private Label joinCode;
    private Label playerCount;
    private Label lobbyStatus;

    private Button leaveButton;
    private Button readyButton;
    private Button copyCodeButton;
    private bool isPlayerReady = false;

    // ---------------------------------------------------------
    // Player Slots
    // ---------------------------------------------------------

    private LobbyPlayerSlotUI[] playerSlots;

    // ---------------------------------------------------------
    // State
    // ---------------------------------------------------------

    private bool operationInProgress;
    private bool resettingReadyAfterDeparture;

    // ---------------------------------------------------------
    // Countdown
    // ---------------------------------------------------------

    private Coroutine countdownCoroutine;

    private bool countdownRunning;

    // ---------------------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------------------

    private void Awake()
    {
        panelRenderer =
            GetComponent<PanelRenderer>();

        if (sessionManager == null)
        {
            sessionManager =
                FindAnyObjectByType<
                    NetworkSessionManager>();
        }

        if (sessionManager == null)
        {
            Debug.LogError(
                "[LobbyUI] " +
                "NetworkSessionManager não encontrado."
            );
        }
    }

    private void OnEnable()
    {
        panelRenderer.RegisterUIReloadCallback(
            OnUIReload
        );

        SubscribeToNetwork();
    }

    private void OnDisable()
    {
        CancelCountdown();

        UnregisterUIEvents();

        panelRenderer.UnregisterUIReloadCallback(
            OnUIReload
        );

        UnsubscribeFromNetwork();

        ClearUIReferences();
    }

    // ---------------------------------------------------------
    // UI Reload
    // ---------------------------------------------------------

    private void OnUIReload(
        PanelRenderer renderer,
        VisualElement rootElement,
        int version)
    {
        // Impede que o mesmo Visual Tree seja configurado
        // novamente sem necessidade.
        if (uiVersion == version)
        {
            return;
        }

        uiVersion = version;

        UnregisterUIEvents();

        root =
            rootElement;

        CacheElements();

        RegisterUIEvents();

        ShowMainMenu();

        ClearLobbySlots();

        Debug.Log(
            $"[LobbyUI] UI carregada. " +
            $"Versão: {version}"
        );
    }

    // ---------------------------------------------------------
    // Cache Elements
    // ---------------------------------------------------------

    private void CacheElements()
    {
        // Main Menu

        mainMenuPanel =
            root.Q<VisualElement>(
                "MainMenuPanel"
            );

        playerNameInput =
            root.Q<TextField>(
                "PlayerNameInput"
            );

        joinCodeInput =
            root.Q<TextField>(
                "JoinCodeInput"
            );

        createButton =
            root.Q<Button>(
                "CreateButton"
            );

        joinButton =
            root.Q<Button>(
                "JoinButton"
            );

        mainStatus =
            root.Q<Label>(
                "MainStatus"
            );

        // Lobby

        lobbyPanel =
            root.Q<VisualElement>(
                "LobbyPanel"
            );

        sessionName =
            root.Q<Label>(
                "SessionName"
            );

        joinCode =
            root.Q<Label>(
                "JoinCode"
            );
        
        copyCodeButton =
            root.Q<Button>(
                "CopyJoinCodeButton"
            );

        playerCount =
            root.Q<Label>(
                "PlayerCount"
            );

        lobbyStatus =
            root.Q<Label>(
                "LobbyStatus"
            );

        readyButton =
            root.Q<Button>(
                "ReadyButton"
            );

        leaveButton =
            root.Q<Button>(
                "LeaveButton"
            );

        // Player Slots

        playerSlots = new LobbyPlayerSlotUI[4];

        playerSlots[0] =
            CreatePlayerSlot(
                "PlayerSlot_1"
            );

        playerSlots[1] =
            CreatePlayerSlot(
                "PlayerSlot_2"
            );

        playerSlots[2] =
            CreatePlayerSlot(
                "PlayerSlot_3"
            );

        playerSlots[3] =
            CreatePlayerSlot(
                "PlayerSlot_4"
            );

        ValidateUIReferences();
    }

    private LobbyPlayerSlotUI CreatePlayerSlot(
        string slotName)
    {
        VisualElement slot =
            root.Q<VisualElement>(
                slotName
            );

        if (slot == null)
        {
            Debug.LogError(
                $"[LobbyUI] " +
                $"Player Slot '{slotName}' " +
                $"não encontrado."
            );

            return null;
        }

        return new LobbyPlayerSlotUI(
            slot
        );
    }
    private void ClearUIReferences()
    {
        root = null;

        mainMenuPanel = null;
        playerNameInput = null;
        joinCodeInput = null;
        createButton = null;
        joinButton = null;
        mainStatus = null;

        lobbyPanel = null;
        sessionName = null;
        joinCode = null;
        playerCount = null;
        lobbyStatus = null;

        leaveButton = null;
        readyButton = null;
        copyCodeButton = null;

        playerSlots = null;

        uiVersion = -1;
    }

    // ---------------------------------------------------------
    // UI Events
    // ---------------------------------------------------------

    private void RegisterUIEvents()
    {
        if (createButton != null)
        {
            createButton.clicked +=
                OnCreateClicked;
        }

        if (joinButton != null)
        {
            joinButton.clicked +=
                OnJoinClicked;
        }

        if (readyButton != null)
        {
            readyButton.clicked +=
                OnReadyClicked;
        }

        if (leaveButton != null)
        {
            leaveButton.clicked +=
                OnLeaveClicked;
        }

        if (copyCodeButton != null)
        {
            copyCodeButton.clicked +=
                OnCopyCodeClicked;
        }
    }

    private void UnregisterUIEvents()
    {
        if (createButton != null)
        {
            createButton.clicked -=
                OnCreateClicked;
        }

        if (joinButton != null)
        {
            joinButton.clicked -=
                OnJoinClicked;
        }

        if (readyButton != null)
        {
            readyButton.clicked -=
                OnReadyClicked;
        }

        if (leaveButton != null)
        {
            leaveButton.clicked -=
                OnLeaveClicked;
        }

        if (copyCodeButton != null)
        {
            copyCodeButton.clicked -=
                OnCopyCodeClicked;
        }
    }

    // ---------------------------------------------------------
    // Network Events
    // ---------------------------------------------------------

    private void SubscribeToNetwork()
    {
        if (sessionManager == null)
        {
            return;
        }

        sessionManager.LobbyChanged +=
            RefreshLobby;

        sessionManager.SessionEnded +=
            OnSessionEnded;
        
        sessionManager.PlayerLeftSession +=
            OnPlayerLeftSession;
    }

    private void UnsubscribeFromNetwork()
    {
        if (sessionManager == null)
        {
            return;
        }

        sessionManager.LobbyChanged -=
            RefreshLobby;

        sessionManager.SessionEnded -=
            OnSessionEnded;
        
        sessionManager.PlayerLeftSession -=
            OnPlayerLeftSession;
    }

    // ---------------------------------------------------------
    // Create Session
    // ---------------------------------------------------------

    private async void OnCreateClicked()
    {
        if (operationInProgress)
        {
            return;
        }

        operationInProgress =
            true;

        SetMainButtonsEnabled(
            false
        );

        SetMainStatus(
            "Criando partida..."
        );

        string playerName =
            playerNameInput?.value;

        if (string.IsNullOrWhiteSpace(
                playerName))
        {
            playerName = "Player";
        }

        bool success =
            await sessionManager
                .CreateSessionAsync(
                    playerName
                );

        operationInProgress =
            false;

        SetMainButtonsEnabled(
            true
        );

        if (!success)
        {
            SetMainStatus(
                sessionManager
                    .LastErrorMessage
            );

            return;
        }

        ShowLobby();

        SetLobbyStatus(
            "Partida criada. " +
            "Aguardando jogadores..."
        );

        RefreshLobby();
    }

    // ---------------------------------------------------------
    // Join Session
    // ---------------------------------------------------------

    private async void OnJoinClicked()
    {
        if (operationInProgress)
        {
            return;
        }

        operationInProgress =
            true;

        SetMainButtonsEnabled(
            false
        );

        SetMainStatus(
            "Entrando na partida..."
        );

        string playerName =
            playerNameInput?.value;

        if (string.IsNullOrWhiteSpace(
                playerName))
        {
            playerName = "Player";
        }

        string code =
            joinCodeInput?.value;

        int requestUIVersion = uiVersion;

        bool success =
            await sessionManager
                .JoinSessionAsync(
                    code,
                    playerName
                );

        // A cena pode ter sido descarregada durante o await.
        if (this == null)
        {
            return;
        }

        operationInProgress = false;

        if (!IsUIAvailable(requestUIVersion))
        {
            return;
        }

        SetMainButtonsEnabled(true);

        if (!success)
        {
            SetMainStatus(
                sessionManager.LastErrorMessage
            );

            return;
        }

        ShowLobby();

        SetLobbyStatus(
            "Você entrou na partida."
        );

        RefreshLobby();
    }

    // ---------------------------------------------------------
    // Lobby
    // ---------------------------------------------------------

    private void ShowLobby()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.style.display =
                DisplayStyle.None;
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.style.display =
                DisplayStyle.Flex;
        }

        isPlayerReady = false;
        if (readyButton != null)
        {
            readyButton.RemoveFromClassList("is-toggled");
            readyButton.text = "PRONTO";
        }

        RefreshLobby();
    }

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.style.display =
                DisplayStyle.Flex;
        }

        if (lobbyPanel != null)
        {
            lobbyPanel.style.display =
                DisplayStyle.None;
        }
    }

    private void RefreshLobby()
    {
        if (sessionManager == null)
        {
            return;
        }

        if (!sessionManager.IsInSession)
        {
            ShowMainMenu();
            return;
        }

        if (sessionName != null)
        {
            sessionName.text =
                sessionManager.SessionName;
        }

        if (joinCode != null)
        {
            joinCode.text =
                $"Código: {sessionManager.JoinCode}";
        }

        if (playerCount != null)
        {
            playerCount.text =
                $"{sessionManager.PlayerCount}/" +
                $"{sessionManager.MaxPlayers}";
        }

        List<LobbyPlayerData> players =
            sessionManager
                .GetLobbyPlayers();

        for (int i = 0;
             i < playerSlots.Length;
             i++)
        {
            if (playerSlots[i] == null)
            {
                continue;
            }

            if (i < players.Count)
            {
                playerSlots[i]
                    .SetPlayer(
                        players[i]
                    );
            }
            else
            {
                playerSlots[i]
                    .SetEmpty();
            }
        }
        UpdateCountdownState();
    }

    // ---------------------------------------------------------
    // Pronto
    // ---------------------------------------------------------

    private async void OnReadyClicked()
    {
        if (readyButton == null)
            return;

        if (sessionManager == null)
        {
            Debug.LogError(
                "[LobbyUI] NetworkSessionManager não encontrado."
            );

            return;
        }

        // Inverte o estado local
        bool newReadyState = !isPlayerReady;

        Debug.Log(
            $"[LobbyUI] Jogador clicou no Ready. " +
            $"Estado solicitado: {newReadyState}"
        );

        bool success =
            await sessionManager.SetReadyAsync(
                newReadyState
            );

        if (!success)
        {
            Debug.LogError(
                "[LobbyUI] Não foi possível atualizar o Ready na sessão."
            );

            return;
        }

        // Só atualizamos o estado local depois que
        // o NetworkSessionManager confirmou o envio.
        isPlayerReady = newReadyState;

        readyButton.ToggleInClassList(
            "is-toggled"
        );

        if (isPlayerReady)
        {
            readyButton.text = "CANCELAR";
        }
        else
        {
            readyButton.text = "PRONTO";
        }
    }
    private bool AreAllPlayersReady()
    {
        if (sessionManager == null ||
            !sessionManager.IsInSession)
        {
            return false;
        }

        List<LobbyPlayerData> players =
            sessionManager.GetLobbyPlayers();

        if (players.Count == 0)
        {
            return false;
        }

        foreach (LobbyPlayerData player in players)
        {
            if (!player.IsReady)
            {
                return false;
            }
        }

        return true;
    }

    // ---------------------------------------------------------
    // Leave
    // ---------------------------------------------------------

    private async void OnLeaveClicked()
    {
        CancelCountdown();

        if (operationInProgress)
        {
            return;
        }

        operationInProgress =
            true;
        if (leaveButton != null)
        {
            leaveButton.SetEnabled(
                false
            );
        }

        SetLobbyStatus(
            "Saindo da partida..."
        );

        await sessionManager
            .LeaveSessionAsync();

        operationInProgress = false;

        if (leaveButton != null)
        {
            leaveButton.SetEnabled(true);
        }

        ShowMainMenu();

        SetMainStatus(
            "Você saiu da partida."
        );
    }

    private async void OnPlayerLeftSession()
    {
        if (sessionManager == null ||
            !sessionManager.IsInSession)
        {
            return;
        }

        if (resettingReadyAfterDeparture)
        {
            return;
        }

        resettingReadyAfterDeparture = true;

        Debug.Log(
            "[LobbyUI] Um jogador saiu. " +
            "Redefinindo o estado Ready local."
        );

        // 1. Interrompe a contagem regressiva.
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        countdownRunning = false;

        // 2. Atualiza imediatamente o estado visual local.
        isPlayerReady = false;

        if (readyButton != null)
        {
            readyButton.RemoveFromClassList("is-toggled");
            readyButton.text = "PRONTO";
            readyButton.SetEnabled(false);
        }

        SetLobbyStatus(
            "Um jogador saiu. " +
            "Todos precisam ficar prontos novamente."
        );

        // 3. Salva Ready=false para o jogador local.
        bool success = await sessionManager.SetReadyAsync(false);

        // 4. Libera o botão após a operação.
        if (readyButton != null)
        {
            readyButton.SetEnabled(true);
        }

        resettingReadyAfterDeparture = false;

        if (!success)
        {
            Debug.LogError(
                "[LobbyUI] Não foi possível redefinir " +
                "o estado Ready após a saída de um jogador."
            );

            SetLobbyStatus(
                "Falha ao redefinir o estado de pronto. " +
                "Tente novamente."
            );

            return;
        }

        // 5. Atualiza os jogadores e seus indicadores.
        RefreshLobby();

        SetLobbyStatus(
            "Um jogador saiu. " +
            "Todos precisam ficar prontos novamente."
        );
    }

    private void OnSessionEnded()
    {
        CancelCountdown();

        operationInProgress = false;

        ShowMainMenu();

        SetMainStatus(
            "A sessão foi encerrada."
        );
    }

    // ---------------------------------------------------------
    // Lobby Slots
    // ---------------------------------------------------------

    private void ClearLobbySlots()
    {
        if (playerSlots == null)
        {
            return;
        }

        for (int i = 0;
             i < playerSlots.Length;
             i++)
        {
            playerSlots[i]?.SetEmpty();
        }
    }

    // ---------------------------------------------------------
    // Status
    // ---------------------------------------------------------

    private void SetMainStatus(
        string message)
    {
        if (mainStatus != null)
        {
            mainStatus.text =
                message ?? string.Empty;
        }
    }

    private void SetLobbyStatus(
        string message)
    {
        if (lobbyStatus != null)
        {
            lobbyStatus.text =
                message ?? string.Empty;
        }
    }

    private void SetMainButtonsEnabled(bool enabled)
    {
        if (!isActiveAndEnabled ||
            root == null ||
            root.panel == null)
        {
            return;
        }

        if (createButton != null)
        {
            createButton.SetEnabled(enabled);
        }

        if (joinButton != null)
        {
            joinButton.SetEnabled(enabled);
        }
    }

    // ---------------------------------------------------------
    // Validation
    // ---------------------------------------------------------

    private void ValidateUIReferences()
    {
        if (root == null)
        {
            Debug.LogError(
                "[LobbyUI] Root da UI não encontrado."
            );
        }

        if (mainMenuPanel == null)
        {
            Debug.LogError(
                "[LobbyUI] " +
                "MainMenuPanel não encontrado."
            );
        }

        if (lobbyPanel == null)
        {
            Debug.LogError(
                "[LobbyUI] " +
                "LobbyPanel não encontrado."
            );
        }

        if (playerNameInput == null)
        {
            Debug.LogError(
                "[LobbyUI] " +
                "PlayerNameInput não encontrado."
            );
        }

        if (joinCodeInput == null)
        {
            Debug.LogError(
                "[LobbyUI] " +
                "JoinCodeInput não encontrado."
            );
        }

        if (createButton == null)
        {
            Debug.LogError(
                "[LobbyUI] " +
                "CreateButton não encontrado."
            );
        }

        if (joinButton == null)
        {
            Debug.LogError(
                "[LobbyUI] " +
                "JoinButton não encontrado."
            );
        }

        if (readyButton == null)
        {
            Debug.LogError(
                "[LobbyUI] " +
                "ReadyButton não encontrado."
            );
        }

        if (leaveButton == null)
        {
            Debug.LogError(
                "[LobbyUI] " +
                "LeaveButton não encontrado."
            );
        }
    }
    private bool IsUIAvailable(int expectedVersion)
    {
        // Verifica primeiro se o próprio MonoBehaviour existe.
        if (this == null)
        {
            return false;
        }

        return isActiveAndEnabled &&
               uiVersion == expectedVersion &&
               root != null &&
               root.panel != null;
    }
    // ---------------------------------------------------------
    // Countdown
    // ---------------------------------------------------------
    private void UpdateCountdownState()
    {
        if (sessionManager == null ||
            !sessionManager.IsInSession)
        {
            CancelCountdown();
            return;
        }

        if (!AreAllPlayersReady())
        {
            CancelCountdown();

            SetLobbyStatus(
                "Aguardando todos os jogadores ficarem prontos..."
            );

            return;
        }

        if (countdownRunning)
        {
            return;
        }

        countdownCoroutine =
            StartCoroutine(CountdownRoutine());

        countdownRunning = true;
    }
    private IEnumerator CountdownRoutine()
    {
        for (int seconds = 5; seconds >= 1; seconds--)
        {
            if (!AreAllPlayersReady())
            {
                countdownCoroutine = null;
                countdownRunning = false;

                SetLobbyStatus(
                    "Aguardando todos os jogadores ficarem prontos..."
                );

                yield break;
            }

            SetLobbyStatus(
                $"Partida iniciando em {seconds}..."
            );

            yield return new WaitForSeconds(1f);
        }

        // Confere novamente antes de concluir a contagem.
        if (!AreAllPlayersReady())
        {
            countdownCoroutine = null;
            countdownRunning = false;

            SetLobbyStatus(
                "Aguardando todos os jogadores ficarem prontos..."
            );

            yield break;
        }

        countdownCoroutine = null;
        countdownRunning = false;

        SetLobbyStatus("Iniciando partida...");
        Debug.Log("[LobbyUI] Contagem regressiva concluída.");

        NetworkSceneTransition transition = NetworkSceneTransition.Instance;

        if (transition == null)
        {
            Debug.LogError("[LobbyUI] NetworkSceneTransition não encontrado.");
            SetLobbyStatus("Erro ao iniciar a partida.");
            yield break;
        }

        // Todos os jogadores exibem o Loading.
        transition.ShowWaitingForHost();

        // Somente o Host inicia a transição de cena.
        if (!sessionManager.IsHost)
        {
            SetLobbyStatus("Aguardando o anfitrião iniciar a partida...");
            yield break;
        }

        bool success = transition.StartGameTransition();

        if (!success)
        {
            SetLobbyStatus("Não foi possível iniciar a partida.");
        }
    }
    private void CancelCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        countdownRunning = false;
    }
    // ---------------------------------------------------------
    // Copy Join Code
    // ---------------------------------------------------------
    private void OnCopyCodeClicked()
    {
        if (sessionManager == null ||
            !sessionManager.IsInSession)
        {
            SetLobbyStatus(
                "Você não está em uma partida."
            );

            return;
        }

        string code = sessionManager.JoinCode;

        if (string.IsNullOrWhiteSpace(code))
        {
            SetLobbyStatus(
                "Código da sala indisponível."
            );

            return;
        }

        GUIUtility.systemCopyBuffer = code;

        SetLobbyStatus(
            "Código copiado! Compartilhe com seus amigos."
        );

        Debug.Log("[LobbyUI] Código da sala copiado.");
    }
}