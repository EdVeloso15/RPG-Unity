
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayerSpawner : MonoBehaviour
{
    private static NetworkPlayerSpawner Instance;

    private NetworkManager networkManager;

    // ClientId -> índice do SpawnPoint reservado.
    private readonly Dictionary<ulong, int> assignedSpawnIndices =
        new Dictionary<ulong, int>();

    // ---------------------------------------------------------
    // INITIALIZATION
    // ---------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError(
                "[NetworkPlayerSpawner] NetworkManager não encontrado."
            );

            return;
        }

        networkManager.ConnectionApprovalCallback =
            ApprovalCheck;

        networkManager.OnClientConnectedCallback +=
            OnClientConnected;

        networkManager.OnClientDisconnectCallback +=
            OnClientDisconnected;

        Debug.Log(
            "[NetworkPlayerSpawner] Sistema de Spawn inicializado."
        );
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -=
                OnClientConnected;

            networkManager.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }

        Instance = null;
    }

    // ---------------------------------------------------------
    // CONNECTION APPROVAL
    // ---------------------------------------------------------

    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        ulong clientId = request.ClientNetworkId;

        response.Approved = false;
        response.CreatePlayerObject = false;

        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;

        response.PlayerPrefabHash = null;

        SpawnRegion region = GetActiveSpawnRegion();

        if (region == null)
        {
            RejectConnection(
                response,
                clientId,
                "Nenhuma região de Spawn disponível."
            );

            return;
        }

        region.RefreshSpawnPoints();

        if (!region.HasExpectedSpawnPointCount)
        {
            RejectConnection(
                response,
                clientId,
                $"Região '{region.name}' possui quantidade inválida de SpawnPoints."
            );

            return;
        }

        int spawnIndex = GetOrAssignSpawnIndex(
            clientId,
            region.SpawnPointCount
        );

        if (spawnIndex < 0)
        {
            RejectConnection(
                response,
                clientId,
                "Não existem posições de Spawn disponíveis."
            );

            return;
        }

        Transform spawnPoint =
            region.GetSpawnPoint(spawnIndex);

        if (spawnPoint == null)
        {
            assignedSpawnIndices.Remove(clientId);

            RejectConnection(
                response,
                clientId,
                "O SpawnPoint selecionado não foi encontrado."
            );

            return;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;

        response.Position = spawnPoint.position;
        response.Rotation = spawnPoint.rotation;

        Debug.Log(
            $"[NetworkPlayerSpawner] Jogador aprovado. " +
            $"ClientId: {clientId} | " +
            $"Região: {region.name} | " +
            $"SpawnIndex: {spawnIndex}"
        );
    }

    private void RejectConnection(
        NetworkManager.ConnectionApprovalResponse response,
        ulong clientId,
        string reason)
    {
        response.Approved = false;
        response.CreatePlayerObject = false;
        response.Reason = reason;

        Debug.LogWarning(
            $"[NetworkPlayerSpawner] Conexão recusada. " +
            $"ClientId: {clientId} | Motivo: {reason}"
        );
    }

    // ---------------------------------------------------------
    // SPAWN REGION DISCOVERY
    // ---------------------------------------------------------

    private SpawnRegion GetActiveSpawnRegion()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        SpawnRegion selectedRegion = null;

        GameObject[] sceneRoots =
            activeScene.GetRootGameObjects();

        foreach (GameObject root in sceneRoots)
        {
            SpawnRegion[] regions =
                root.GetComponentsInChildren<SpawnRegion>(true);

            foreach (SpawnRegion region in regions)
            {
                if (!region.IsEnabledForSpawning)
                    continue;

                if (selectedRegion != null)
                {
                    Debug.LogWarning(
                        "[NetworkPlayerSpawner] Mais de uma região " +
                        "está habilitada. Será utilizada a primeira encontrada."
                    );

                    continue;
                }

                selectedRegion = region;
            }
        }

        return selectedRegion;
    }

    // ---------------------------------------------------------
    // SPAWN ASSIGNMENT
    // ---------------------------------------------------------

    private int GetOrAssignSpawnIndex(
        ulong clientId,
        int spawnPointCount)
    {
        // Mantém a posição já reservada para o jogador.
        if (assignedSpawnIndices.TryGetValue(
                clientId,
                out int assignedIndex))
        {
            if (assignedIndex < spawnPointCount)
                return assignedIndex;

            assignedSpawnIndices.Remove(clientId);
        }

        // Reserva uma posição para o Host, caso ainda não tenha
        // sido registrado pelo evento OnClientConnectedCallback.
        if (networkManager.IsHost &&
            clientId != NetworkManager.ServerClientId &&
            networkManager.ConnectedClientsIds.Contains(
                NetworkManager.ServerClientId
            ) &&
            !assignedSpawnIndices.ContainsKey(
                NetworkManager.ServerClientId
            ))
        {
            int hostIndex = FindFreeSpawnIndex(spawnPointCount);

            if (hostIndex >= 0)
            {
                assignedSpawnIndices.Add(
                    NetworkManager.ServerClientId,
                    hostIndex
                );
            }
        }

        int freeIndex = FindFreeSpawnIndex(spawnPointCount);

        if (freeIndex < 0)
            return -1;

        assignedSpawnIndices.Add(
            clientId,
            freeIndex
        );

        return freeIndex;
    }

    private int FindFreeSpawnIndex(int spawnPointCount)
    {
        for (int i = 0; i < spawnPointCount; i++)
        {
            if (!assignedSpawnIndices.ContainsValue(i))
                return i;
        }

        return -1;
    }

    // ---------------------------------------------------------
    // NETWORK EVENTS
    // ---------------------------------------------------------

    private void OnClientConnected(ulong clientId)
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        if (assignedSpawnIndices.ContainsKey(clientId))
            return;

        SpawnRegion region = GetActiveSpawnRegion();

        if (region == null)
            return;

        region.RefreshSpawnPoints();

        int spawnIndex = FindFreeSpawnIndex(
            region.SpawnPointCount
        );

        if (spawnIndex < 0)
            return;

        assignedSpawnIndices.Add(
            clientId,
            spawnIndex
        );

        Debug.Log(
            $"[NetworkPlayerSpawner] Posição registrada. " +
            $"ClientId: {clientId} | SpawnIndex: {spawnIndex}"
        );
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (assignedSpawnIndices.Remove(clientId))
        {
            Debug.Log(
                $"[NetworkPlayerSpawner] Posição liberada. " +
                $"ClientId: {clientId}"
            );
        }
    }
    public void LockAllPlayers(bool locked)
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        foreach (var client in networkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            NetworkPlayerController controller =
                client.PlayerObject.GetComponent<NetworkPlayerController>();

            if (controller == null)
            {
                Debug.LogWarning(
                    $"[NetworkPlayerSpawner] NetworkPlayerController " +
                    $"não encontrado para ClientId {client.ClientId}."
                );

                continue;
            }

            controller.SetMovementLocked(locked);
        }
    }
    public bool RepositionAllPlayers()
    {
        if (networkManager == null || !networkManager.IsServer)
        {
            Debug.LogError(
                "[NetworkPlayerSpawner] Reposicionamento permitido apenas no servidor."
            );

            return false;
        }

        SpawnRegion region = GetActiveSpawnRegion();

        if (region == null)
        {
            Debug.LogError(
                "[NetworkPlayerSpawner] Nenhuma SpawnRegion encontrada."
            );

            return false;
        }

        region.RefreshSpawnPoints();

        if (!region.HasExpectedSpawnPointCount)
        {
            Debug.LogError(
                $"[NetworkPlayerSpawner] Quantidade inválida de SpawnPoints " +
                $"na região {region.name}."
            );

            return false;
        }

        foreach (var client in networkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                Debug.LogError(
                    $"[NetworkPlayerSpawner] PlayerObject ausente para " +
                    $"ClientId {client.ClientId}."
                );

                return false;
            }

            if (!assignedSpawnIndices.TryGetValue(
                client.ClientId,
                out int spawnIndex))
            {
                spawnIndex = FindFreeSpawnIndex(region.SpawnPointCount);

                if (spawnIndex < 0)
                {
                    Debug.LogError(
                        $"[NetworkPlayerSpawner] Não há SpawnPoint disponível " +
                        $"para ClientId {client.ClientId}."
                    );

                    return false;
                }

                assignedSpawnIndices.Add(client.ClientId, spawnIndex);
            }

            Transform spawnPoint = region.GetSpawnPoint(spawnIndex);

            if (spawnPoint == null)
                return false;

            CharacterController characterController =
                client.PlayerObject.GetComponent<CharacterController>();

            if (characterController != null)
                characterController.enabled = false;

            client.PlayerObject.transform.SetPositionAndRotation(
                spawnPoint.position,
                spawnPoint.rotation
            );

            if (characterController != null)
                characterController.enabled = true;

            NetworkPlayerController playerController =
                client.PlayerObject.GetComponent<NetworkPlayerController>();

            Debug.Log(
                $"[NetworkPlayerSpawner] Jogador reposicionado. " +
                $"ClientId: {client.ClientId} | " +
                $"SpawnIndex: {spawnIndex} | " +
                $"Position: {spawnPoint.position}"
            );
        }
        
        LockAllPlayers(false);
        
        Debug.Log(
            "[NetworkPlayerSpawner] Todos os jogadores foram reposicionados."
        );

        return true;
    }
}
