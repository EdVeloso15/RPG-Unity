using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 15f;

    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundedForce = -2f;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Executa o movimento físico do personagem.
    /// Esta função será chamada pelo sistema de networking
    /// no lado autoritativo.
    /// </summary>
    public void MoveCharacter(Vector3 moveDirection)
    {
        if (!controller.enabled)
            return;

        // Segurança: nunca permita uma direção horizontal
        // com magnitude maior que 1.
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Rotação do personagem.
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Gravidade.
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedForce;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity =
            moveDirection * moveSpeed;

        velocity.y = verticalVelocity;

        controller.Move(
            velocity * Time.deltaTime
        );
    }
}