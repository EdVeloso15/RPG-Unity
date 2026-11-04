
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkSessionEventTestProbe : MonoBehaviour
{
    [SerializeField]
    private NetworkSessionManager sessionManager;

    private int lobbyChangedCount;
    private int sessionEndedCount;
    private int playerLeftSessionCount;

    private bool isSubscribed;

    private void Start()
    {
        if (sessionManager == null)
        {
            sessionManager =
                NetworkSessionManager.Instance;
        }

        if (sessionManager == null)
        {
            Debug.LogError(
                "[SessionTestProbe] NetworkSessionManager não encontrado."
            );

            enabled = false;
            return;
        }

        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed || sessionManager == null)
            return;

        sessionManager.LobbyChanged +=
            OnLobbyChanged;

        sessionManager.SessionEnded +=
            OnSessionEnded;

        sessionManager.PlayerLeftSession +=
            OnPlayerLeftSession;

        isSubscribed = true;

        Debug.Log(
            "[SessionTestProbe] Inscrito nos eventos públicos."
        );
    }

    private void UnsubscribeFromEvents()
    {
        if (!isSubscribed || sessionManager == null)
            return;

        sessionManager.LobbyChanged -=
            OnLobbyChanged;

        sessionManager.SessionEnded -=
            OnSessionEnded;

        sessionManager.PlayerLeftSession -=
            OnPlayerLeftSession;

        isSubscribed = false;

        Debug.Log(
            "[SessionTestProbe] Inscrições removidas."
        );
    }

    private void OnLobbyChanged()
    {
        lobbyChangedCount++;

        Debug.Log(
            $"[SessionTestProbe] LobbyChanged recebido. " +
            $"Total: {lobbyChangedCount}"
        );
    }

    private void OnSessionEnded()
    {
        sessionEndedCount++;

        Debug.Log(
            $"[SessionTestProbe] SessionEnded recebido. " +
            $"Total: {sessionEndedCount}"
        );
    }

    private void OnPlayerLeftSession()
    {
        playerLeftSessionCount++;

        Debug.Log(
            $"[SessionTestProbe] PlayerLeftSession recebido. " +
            $"Total: {playerLeftSessionCount}"
        );
    }

    private void OnGUI()
    {
        if (sessionManager == null)
            return;

        const float width = 300f;
        const float height = 190f;

        GUILayout.BeginArea(
            new Rect(20f, 270f, width, height),
            GUI.skin.box
        );

        GUILayout.Label("SESSION EVENT TEST PROBE");

        GUILayout.Label(
            $"Sessão ativa: {sessionManager.IsInSession}"
        );

        GUILayout.Label(
            $"Papel: " +
            $"{(sessionManager.IsHost ? "HOST" : "CLIENT / NONE")}"
        );

        GUILayout.Space(5f);

        GUILayout.Label(
            $"LobbyChanged: {lobbyChangedCount}"
        );

        GUILayout.Label(
            $"SessionEnded: {sessionEndedCount}"
        );

        GUILayout.Label(
            $"PlayerLeftSession: {playerLeftSessionCount}"
        );

        GUILayout.Space(5f);

        if (GUILayout.Button("ZERAR CONTADORES"))
        {
            ResetCounters();
        }

        GUILayout.EndArea();
    }

    private void ResetCounters()
    {
        lobbyChangedCount = 0;
        sessionEndedCount = 0;
        playerLeftSessionCount = 0;

        Debug.Log(
            "[SessionTestProbe] Contadores zerados."
        );
    }
}
