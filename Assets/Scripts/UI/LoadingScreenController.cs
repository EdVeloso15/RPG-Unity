
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance { get; private set; }

    private PanelRenderer panelRenderer;

    private VisualElement root;
    private VisualElement barFill;
    private Label statusLabel;

    private bool uiInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        panelRenderer = GetComponent<PanelRenderer>();
    }

    private void OnEnable()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponent<PanelRenderer>();

        if (panelRenderer != null)
        {
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }
        else
        {
            Debug.LogError(
                "[LoadingScreen] PanelRenderer não encontrado."
            );
        }
    }


    private void OnDisable()
    {
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        uiInitialized = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnUIReload(
    PanelRenderer renderer,
    VisualElement rootElement,
    int version)
    {
        root = rootElement;

        barFill = root.Q<VisualElement>("LoadingBarFill");
        statusLabel = root.Q<Label>("LoadingStatus");

        uiInitialized =
            root != null &&
            barFill != null &&
            statusLabel != null;

        if (!uiInitialized)
        {
            Debug.LogError(
                "[LoadingScreen] Falha ao localizar elementos. " +
                $"Root: {root != null}, " +
                $"BarFill: {barFill != null}, " +
                $"StatusLabel: {statusLabel != null}"
            );

            return;
        }

        SetProgress(0f);
        SetStatus("Preparando partida...");
        Hide();

        Debug.Log(
            $"[LoadingScreen] UI inicializada. Versão: {version}"
        );
    }

    public void Show(string message = "Preparando partida...")
    {
        if (!uiInitialized)
        {
            Debug.LogError(
                "[LoadingScreen] UI ainda não foi inicializada."
            );
            return;
        }

        root.style.display = DisplayStyle.Flex;

        SetProgress(0f);
        SetStatus(message);

        Debug.Log("[LoadingScreen] Tela exibida.");
    }

    public void Hide()
    {
        if (root == null)
            return;

        root.style.display = DisplayStyle.None;

        Debug.Log("[LoadingScreen] Tela ocultada.");
    }

    public void SetStatus(string message)
    {
        if (statusLabel == null)
            return;

        statusLabel.text = message ?? string.Empty;
    }

    public void SetProgress(float progress)
    {
        if (barFill == null)
            return;

        progress = Mathf.Clamp01(progress);

        barFill.style.width =
            Length.Percent(progress * 100f);
    }
}
