using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] 
    private Rigidbody2D Rig;
    public float MoveSpeed = 10f;
    public Vector3 startPosition;
    
    [Header("Multiplayer")]
    public bool isLocalPlayer = true;
    public int playerNumber = 1; // 1, 2, 3 ou 4
    
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
        
        // Controles para cada jogador
        switch (playerNumber)
        {
            case 1: // Player 1 - W/S (lado esquerdo superior)
                isPressingUp = Input.GetKey(KeyCode.W);
                isPressingDown = Input.GetKey(KeyCode.S);
                break;
                
            case 2: // Player 2 - Setas (lado direito superior)
                isPressingUp = Input.GetKey(KeyCode.UpArrow);
                isPressingDown = Input.GetKey(KeyCode.DownArrow);
                break;
                
            case 3: // Player 3 - T/G (lado esquerdo inferior)
                isPressingUp = Input.GetKey(KeyCode.T);
                isPressingDown = Input.GetKey(KeyCode.G);
                break;
                
            case 4: // Player 4 - I/K (lado direito inferior)
                isPressingUp = Input.GetKey(KeyCode.I);
                isPressingDown = Input.GetKey(KeyCode.K);
                break;
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