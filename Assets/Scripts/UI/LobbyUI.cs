using System.Collections.Generic;
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

    private bool isPlayerReady = false;

    // ---------------------------------------------------------
    // Player Slots
    // ---------------------------------------------------------

    private LobbyPlayerSlotUI[] playerSlots;

    // ---------------------------------------------------------
    // State
    // ---------------------------------------------------------

    private bool operationInProgress;

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
        panelRenderer.UnregisterUIReloadCallback(
            OnUIReload
        );

        UnsubscribeFromNetwork();
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

        bool success =
            await sessionManager
                .JoinSessionAsync(
                    code,
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
            SetLobbyStatus(
                "Você está pronto."
            );
        }
        else
        {
            readyButton.text = "PRONTO";
            SetLobbyStatus(
                "Aguardando você ficar pronto."
            );
        }
    }

    // ---------------------------------------------------------
    // Leave
    // ---------------------------------------------------------

    private async void OnLeaveClicked()
    {
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

        operationInProgress =
            false;

        if (leaveButton != null)
        {
            leaveButton.SetEnabled(
                true
            );
        }

        ShowMainMenu();

        SetMainStatus(
            "Você saiu da partida."
        );
    }

    private void OnSessionEnded()
    {
        operationInProgress =
            false;

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

    private void SetMainButtonsEnabled(
        bool enabled)
    {
        if (createButton != null)
        {
            createButton.SetEnabled(
                enabled
            );
        }

        if (joinButton != null)
        {
            joinButton.SetEnabled(
                enabled
            );
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
}