using UnityEngine;
using System.Threading.Tasks;

public class SessionOperationTest : MonoBehaviour
{
    [SerializeField]
    private NetworkSessionManager sessionManager;

    [SerializeField]
    private string testPlayerName = "TestPlayer";

    [SerializeField]
    private string testJoinCode = "INVALID";

    [ContextMenu("Testar concorrência Create + Join")]
    private async void TestCreateAndJoinConcurrency()
    {
        if (sessionManager == null)
        {
            Debug.LogError("[TEST] NetworkSessionManager não atribuído.");
            return;
        }

        Debug.Log("[TEST] Iniciando Create...");

        Task<bool> createTask =
            sessionManager.CreateSessionAsync(testPlayerName);

        Debug.Log("[TEST] Solicitando Join enquanto Create está em andamento...");

        Task<bool> joinTask =
            sessionManager.JoinSessionAsync(
                testJoinCode,
                testPlayerName
            );

        bool joinResult = await joinTask;

        Debug.Log(
            $"[TEST] Resultado Join: {joinResult} (esperado: false)"
        );

        bool createResult = await createTask;

        Debug.Log(
            $"[TEST] Resultado Create: {createResult}"
        );

        Debug.Log(
            $"[TEST] LastErrorMessage: " +
            $"{sessionManager.LastErrorMessage}"
        );

        Debug.Log(
            $"[TEST] IsInSession: " +
            $"{sessionManager.IsInSession}"
        );
    }
}