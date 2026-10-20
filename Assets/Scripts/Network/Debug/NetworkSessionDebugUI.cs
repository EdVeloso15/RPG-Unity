using UnityEngine;

public class NetworkSessionDebugUI : MonoBehaviour
{
    [SerializeField]
    private NetworkSessionManager sessionManager;

    private string joinCode = string.Empty;
    private bool operationInProgress;

    private void Awake()
    {
        if (sessionManager == null)
            sessionManager = FindAnyObjectByType<NetworkSessionManager>();
    }

    private void OnGUI()
    {
        if (sessionManager == null)
            return;

        const float width = 360f;
        const float height = 230f;

        Rect panel = new Rect(20f, 20f, width, height);

        GUI.Box(panel, "Online Session - Teste");

        GUILayout.BeginArea(new Rect(
            panel.x + 15f,
            panel.y + 30f,
            panel.width - 30f,
            panel.height - 45f
        ));

        GUILayout.Label(
            sessionManager.IsInSession
                ? $"Conectado | {(sessionManager.IsHost ? "HOST" : "CLIENT")}"
                : "Desconectado"
        );

        if (!sessionManager.IsInSession)
        {
            GUI.enabled = !operationInProgress;

            if (GUILayout.Button("CRIAR PARTIDA", GUILayout.Height(35f)))
            {
                _ = CreateSessionAsync();
            }

            GUILayout.Space(10f);

            GUILayout.Label("Código da partida:");
            joinCode = GUILayout.TextField(joinCode);

            if (GUILayout.Button("ENTRAR NA PARTIDA", GUILayout.Height(35f)))
            {
                _ = JoinSessionAsync();
            }

            GUI.enabled = true;
        }
        else
        {
            GUILayout.Label($"Join Code: {sessionManager.JoinCode}");

            GUILayout.Space(10f);

            if (GUILayout.Button("SAIR DA PARTIDA", GUILayout.Height(35f)))
            {
                _ = LeaveSessionAsync();
            }
        }

        if (operationInProgress)
        {
            GUILayout.Space(10f);
            GUILayout.Label("Processando...");
        }

        GUILayout.EndArea();
    }

    private async System.Threading.Tasks.Task CreateSessionAsync()
    {
        operationInProgress = true;
        await sessionManager.CreateSessionAsync();
        operationInProgress = false;
    }

    private async System.Threading.Tasks.Task JoinSessionAsync()
    {
        operationInProgress = true;
        await sessionManager.JoinSessionAsync(joinCode);
        operationInProgress = false;
    }

    private async System.Threading.Tasks.Task LeaveSessionAsync()
    {
        operationInProgress = true;
        await sessionManager.LeaveSessionAsync();
        operationInProgress = false;
    }
}
