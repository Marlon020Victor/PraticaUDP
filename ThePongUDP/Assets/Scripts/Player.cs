using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] 
    private Rigidbody2D Rig;
    public float MoveSpeed = 10f;
    public Vector3 startPosition;
    
    [Header("Multiplayer")]
    public bool isLocalPlayer = true; // Define se esse paddle é controlado localmente
    public int playerNumber = 1; // 1 ou 2
    
    private PongClientUDP networkClient;

    private void Start()
    {
        startPosition = transform.position;
        networkClient = FindFirstObjectByType<PongClientUDP>();
    }

    void Update()
    {
        // Apenas o jogador local controla seu paddle
        if (networkClient != null)
        {
            // Player 1 controla paddle 1, Player 2 controla paddle 2
            isLocalPlayer = (networkClient.myId == playerNumber);
        }
        
        if (isLocalPlayer)
        {
            PlayMovement();
        }
    }

    private void PlayMovement()
    {
        bool isPressingUp = false;
        bool isPressingDown = false;
        
        // Controles diferentes para cada jogador
        if (playerNumber == 1)
        {
            isPressingUp = Input.GetKey(KeyCode.W);
            isPressingDown = Input.GetKey(KeyCode.S);
        }
        else if (playerNumber == 2)
        {
            isPressingUp = Input.GetKey(KeyCode.UpArrow);
            isPressingDown = Input.GetKey(KeyCode.DownArrow);
        }

        if (isPressingUp)
        {
            transform.Translate(Vector2.up * MoveSpeed * Time.deltaTime);
        }

        if (isPressingDown)
        {
            transform.Translate(Vector2.down * MoveSpeed * Time.deltaTime);
        }
    }

    public void Reset()
    {
        if (Rig != null)
        {
            Rig.linearVelocity = Vector2.zero;
        }
        transform.position = startPosition;
    }
}