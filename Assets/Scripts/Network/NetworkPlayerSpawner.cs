using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField]
    private Transform[] spawnPoints;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "[NetworkPlayerSpawner] NetworkManager não encontrado no Start."
            );

            return;
        }

        NetworkManager.Singleton.ConnectionApprovalCallback =
            ApprovalCheck;

        Debug.Log(
            "[NetworkPlayerSpawner] ConnectionApprovalCallback configurado."
        );
    }

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        ulong clientId = request.ClientNetworkId;

        response.Approved = true;
        response.CreatePlayerObject = true;

        response.PlayerPrefabHash = null;

        int spawnIndex = GetSpawnIndex(clientId);

        if (spawnIndex >= 0)
        {
            response.Position = spawnPoints[spawnIndex].position;
            response.Rotation = spawnPoints[spawnIndex].rotation;
        }
        else
        {
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;

            Debug.LogWarning(
                $"[NetworkPlayerSpawner] Não existe Spawn Point " +
                $"disponível para ClientId {clientId}."
            );
        }

        Debug.Log(
            $"[NetworkPlayerSpawner] ClientId {clientId} aprovado. " +
            $"SpawnIndex: {spawnIndex}"
        );
    }

    private int GetSpawnIndex(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return -1;

        int index = (int)clientId;

        if (index >= spawnPoints.Length)
            return -1;

        return index;
    }
}