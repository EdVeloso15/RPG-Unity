using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class NetworkPlayerController : NetworkBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerInput playerInput;
    private CharacterController characterController;

    private Camera playerCamera;

    /// <summary>
    /// Última direção de movimento desejada pelo dono deste Player.
    ///
    /// Owner:
    /// escreve.
    ///
    /// Server:
    /// lê e usa para executar o movimento.
    ///
    /// Outros clientes:
    /// não precisam ler.
    /// </summary>
    private NetworkVariable<Vector3> movementDirection =
        new NetworkVariable<Vector3>(
            Vector3.zero,
            NetworkVariableReadPermission.Owner,
            NetworkVariableWritePermission.Owner
        );

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>();
        characterController = GetComponent<CharacterController>();

        // O Player começa com Input e CharacterController desabilitados.
        //
        // Depois do spawn:
        // - o dono recebe Input;
        // - o servidor recebe CharacterController.
        playerInput.enabled = false;
        characterController.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Somente o dono deste NetworkObject recebe input.
        playerInput.enabled = IsOwner;

        // O CharacterController só precisa existir ativo
        // no processo que executa o movimento autoritativo.
        //
        // Em Host:
        // IsServer = true.
        //
        // Em Client:
        // IsServer = false.
        characterController.enabled = IsServer;

        if (IsOwner)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                Debug.LogError(
                    $"[NetworkPlayerController] " +
                    $"Câmera principal não encontrada para " +
                    $"ClientId {OwnerClientId}."
                );
            }
        }

        Debug.Log(
            $"[NetworkPlayerController] " +
            $"Spawned | ClientId = {OwnerClientId} | " +
            $"IsOwner = {IsOwner} | " +
            $"IsServer = {IsServer}"
        );
    }

    /// <summary>
    /// Recebe a intenção de movimento do Input System.
    /// </summary>
    public void SetMovementInput(Vector2 input)
    {
        // Somente o dono pode informar a intenção deste Player.
        if (!IsOwner)
            return;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
                return;
        }

        Vector3 worldDirection = Vector3.zero;

        if (input.sqrMagnitude > 0.0001f)
        {
            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraRight = playerCamera.transform.right;

            // O movimento acontece no plano X/Z.
            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            worldDirection =
                cameraForward * input.y +
                cameraRight * input.x;

            // Segurança contra movimento diagonal acima da velocidade.
            if (worldDirection.sqrMagnitude > 1f)
            {
                worldDirection.Normalize();
            }
        }

        // Aqui estamos "guardando a intenção":
        //
        // O cliente diz:
        // "Minha direção desejada agora é X."
        //
        // O servidor posteriormente lê esse valor.
        movementDirection.Value = worldDirection;
    }

    private void Update()
    {
        // Apenas o servidor executa o movimento autoritativo.
        if (!IsServer)
            return;

        if (!characterController.enabled)
            return;

        playerMovement.MoveCharacter(
            movementDirection.Value
        );
    }

    public override void OnNetworkDespawn()
    {
        playerInput.enabled = false;
        characterController.enabled = false;

        base.OnNetworkDespawn();
    }
}