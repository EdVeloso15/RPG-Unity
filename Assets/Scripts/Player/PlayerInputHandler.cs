using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkPlayerController))]
public class PlayerInputHandler : MonoBehaviour
{
    private NetworkPlayerController networkPlayerController;

    private void Awake()
    {
        networkPlayerController = GetComponent<NetworkPlayerController>();
    }

    // Recebe WASD ou analógico como Vector2.
    // Aqui apenas informamos a intenção de movimento.
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();

        networkPlayerController.SetMovementInput(input);
    }
}