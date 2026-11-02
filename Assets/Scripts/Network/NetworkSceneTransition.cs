
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(LoadingScreenController))]
public class NetworkSceneTransition : MonoBehaviour
{
    public static NetworkSceneTransition Instance { get; private set; }

    [Header("Scene Transition")]
    [SerializeField]
    private string gameSceneName = "GameWorld";

    [Header("Dependencies")]
    [SerializeField]
    private NetworkSessionManager sessionManager;

    private LoadingScreenController loadingScreen;
    private NetworkManager networkManager;

    private Coroutine loadingCoroutine;
    private Coroutine hideCoroutine;

    private bool transitionStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        loadingScreen = GetComponent<LoadingScreenController>();

        if (sessionManager == null)
        {
            sessionManager =
                FindAnyObjectByType<NetworkSessionManager>();
        }
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError(
                "[NetworkSceneTransition] NetworkManager não encontrado."
            );

            return;
        }

        if (networkManager.SceneManager == null)
        {
            Debug.LogError(
                "[NetworkSceneTransition] SceneManager do NGO indisponível."
            );

            return;
        }

        networkManager.SceneManager.OnSceneEvent += OnSceneEvent;

        Debug.Log(
            "[NetworkSceneTransition] Inscrito em OnSceneEvent. " +
            $"NetworkManager ativo: {networkManager.IsListening}"
        );

        Debug.Log(
            "[NetworkSceneTransition] Eventos de cena registrados."
        );
    }

    private void OnDestroy()
    {
        if (networkManager != null &&
            networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
        }

        if (Instance == this)
            Instance = null;
    }

    // ---------------------------------------------------------
    // START GAME
    // ---------------------------------------------------------

    public bool StartGameTransition()
    {
        if (transitionStarted)
        {
            Debug.LogWarning(
                "[NetworkSceneTransition] Transição já iniciada."
            );

            return false;
        }

        if (networkManager == null ||
            !networkManager.IsListening)
        {
            Debug.LogError(
                "[NetworkSceneTransition] NetworkManager não está ativo."
            );

            return false;
        }

        if (!networkManager.IsServer)
        {
            Debug.LogWarning(
                "[NetworkSceneTransition] Apenas o Host pode iniciar a transição."
            );

            return false;
        }

        if (sessionManager == null ||
            !sessionManager.IsHost)
        {
            Debug.LogError(
                "[NetworkSceneTransition] O jogador não é o Host da sessão."
            );

            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError(
                $"[NetworkSceneTransition] Cena '{gameSceneName}' " +
                "não encontrada no Build Profile."
            );

            return false;
        }

        transitionStarted = true;

        loadingScreen.Show("Preparando partida...");

        Debug.Log(
            $"[NetworkSceneTransition] Iniciando transição para {gameSceneName}."
        );

        Debug.Log(
            $"[NetworkSceneTransition] IsListening: {networkManager.IsListening} | " +
            $"IsServer: {networkManager.IsServer} | " +
            $"IsHost: {networkManager.IsHost} | " +
            $"SceneManager: {networkManager.SceneManager != null}"
        );

        SceneEventProgressStatus status =
            networkManager.SceneManager.LoadScene(
                gameSceneName,
                LoadSceneMode.Single
            );

        Debug.Log(
            $"[NetworkSceneTransition] Resultado de LoadScene: {status}"
        );

        if (status != SceneEventProgressStatus.Started)
        {
            transitionStarted = false;

            loadingScreen.Hide();

            Debug.LogError(
                $"[NetworkSceneTransition] Falha ao iniciar carregamento. " +
                $"Status: {status}"
            );

            return false;
        }
        Debug.Log("[NetworkSceneTransition] LoadScene iniciado com sucesso.");
        return true;
    }

    // ---------------------------------------------------------
    // NETWORK SCENE EVENTS
    // ---------------------------------------------------------

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        Debug.Log(
        $"[NetworkSceneTransition] Scene Event: {sceneEvent.SceneEventType} | " +
        $"ClientId: {sceneEvent.ClientId}"
        );
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                BeginLoading(sceneEvent);
                break;

            case SceneEventType.LoadComplete:
                OnLocalLoadComplete();
                break;

            case SceneEventType.LoadEventCompleted:
                OnAllClientsLoadComplete();
                break;

            case SceneEventType.Synchronize:
                if (sceneEvent.ClientId == networkManager.LocalClientId)
                {
                    BeginSynchronization();
                }
                break;

            case SceneEventType.SynchronizeComplete:
                if (sceneEvent.ClientId == networkManager.LocalClientId)
                {
                    OnSynchronizationComplete();
                }
                break;
        }
    }

    // ---------------------------------------------------------
    // LOADING
    // ---------------------------------------------------------

    private void BeginLoading(SceneEvent sceneEvent)
    {
        if (loadingScreen == null)
            return;

        loadingScreen.Show("Carregando mundo...");

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        if (sceneEvent.AsyncOperation != null)
        {
            loadingCoroutine = StartCoroutine(
                TrackLoadingProgress(sceneEvent.AsyncOperation)
            );
        }

        Debug.Log(
            "[NetworkSceneTransition] Carregamento local iniciado."
        );
    }

    public void ShowWaitingForHost()
    {
        if (loadingScreen == null)
        {
            Debug.LogError("[NetworkSceneTransition] LoadingScreen não encontrado.");
            return;
        }

        loadingScreen.Show("Preparando partida...");
        loadingScreen.SetProgress(0f);
    }

    private IEnumerator TrackLoadingProgress(AsyncOperation operation)
    {
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress);

            loadingScreen.SetProgress(progress);

            int percentage = Mathf.RoundToInt(progress * 100f);

            loadingScreen.SetStatus(
                $"Carregando mundo... {percentage}%"
            );

            yield return null;
        }

        loadingScreen.SetProgress(1f);

        loadingScreen.SetStatus(
            "Sincronizando jogadores..."
        );

        loadingCoroutine = null;
    }

    private void OnLocalLoadComplete()
    {
        Debug.Log(
            "[NetworkSceneTransition] Carregamento local concluído."
        );
    }

    private void OnAllClientsLoadComplete()
    {
        Debug.Log(
            "[NetworkSceneTransition] Todos os clientes terminaram de carregar."
        );

        loadingScreen.SetProgress(1f);
        loadingScreen.SetStatus("Partida pronta!");

        ScheduleHide();
    }

    // ---------------------------------------------------------
    // LATE JOIN
    // ---------------------------------------------------------

    private void BeginSynchronization()
    {
        loadingScreen.Show("Entrando no mundo...");

        Debug.Log(
            "[NetworkSceneTransition] Sincronização de novo cliente iniciada."
        );
    }

    private void OnSynchronizationComplete()
    {
        Debug.Log(
            "[NetworkSceneTransition] Sincronização do cliente concluída."
        );

        loadingScreen.SetProgress(1f);
        loadingScreen.SetStatus("Bem-vindo!");

        ScheduleHide();
    }

    // ---------------------------------------------------------
    // HIDE
    // ---------------------------------------------------------

    private void ScheduleHide()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        loadingScreen.Hide();

        hideCoroutine = null;
    }
}
