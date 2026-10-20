using Unity.Netcode;
using UnityEngine;

public class IsometricCameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField]
    private Vector3 followOffset = new Vector3(10f, 10f, -10f);

    [SerializeField]
    private float followSmoothTime = 0.12f;

    private Transform target;
    private Vector3 currentVelocity;

    private void Start()
    {
        ConfigureIsometricRotation();
    }

    private void LateUpdate()
    {
        TryFindLocalPlayer();

        if (target == null)
            return;

        Vector3 desiredPosition =
            target.position +
            followOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            followSmoothTime
        );

        // Se o Player local for destruído/desconectado,
        // permitimos que o sistema procure novamente.
        if (!target.gameObject.activeInHierarchy)
        {
            target = null;
        }
    }

    private void TryFindLocalPlayer()
    {
        if (target != null)
            return;

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient)
            return;

        NetworkObject localPlayer =
            NetworkManager.Singleton
                .SpawnManager
                .GetLocalPlayerObject();

        if (localPlayer == null)
            return;

        target = localPlayer.transform;

        Debug.Log(
            $"[Camera] Player local encontrado. " +
            $"NetworkObjectId = {localPlayer.NetworkObjectId} | " +
            $"OwnerClientId = {localPlayer.OwnerClientId}"
        );
    }

    private void ConfigureIsometricRotation()
    {
        Vector3 lookDirection =
            -followOffset.normalized;

        transform.rotation =
            Quaternion.LookRotation(
                lookDirection,
                Vector3.up
            );
    }
}