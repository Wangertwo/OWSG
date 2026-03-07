using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
  
    public float speed = 12f;
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;
  
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
  
    Vector3 velocity;
    private bool isMoving;
  
    bool isGrounded;
  
    void Update()
    {
        if (MenuManager.Instance != null && MenuManager.Instance.isMenuOpen)
        {
            return;
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
  
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
  
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
  
        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        bool currentlyMoving = isGrounded && move.sqrMagnitude > 0.001f;

        if (currentlyMoving && !isMoving)
        {
            isMoving = true;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayGrassWalkSound();
            }
        }
        else if (!currentlyMoving && isMoving)
        {
            isMoving = false;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopGrassWalkSound();
            }
        }
  
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
  
        velocity.y += gravity * Time.deltaTime;
  
        controller.Move(velocity * Time.deltaTime);
    }
}
