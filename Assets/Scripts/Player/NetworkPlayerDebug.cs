using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerDebug : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log(
            $"[PlayerNetwork] Spawned | " +
            $"NetworkObjectId: {NetworkObjectId} | " +
            $"OwnerClientId: {OwnerClientId} | " +
            $"IsOwner: {IsOwner} | " +
            $"IsServer: {IsServer} | " +
            $"IsClient: {IsClient}"
        );
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log(
            $"[PlayerNetwork] Despawned | " +
            $"NetworkObjectId: {NetworkObjectId} | " +
            $"OwnerClientId: {OwnerClientId}"
        );

        base.OnNetworkDespawn();
    }
}