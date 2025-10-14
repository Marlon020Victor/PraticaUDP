using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D Rig;
    public float MoveSpeed = 10f;
    public Vector3 startPosition;

    [Header("Multiplayer")]
    public bool isLocalPlayer = false;
    public int playerNumber = 1; // 1, 2, 3 ou 4

    private PongClientUDP networkClient;

    private void Start()
    {
        startPosition = transform.position;
        networkClient = FindFirstObjectByType<PongClientUDP>();

        // Por padrão não ser local até receber ASSIGN
        isLocalPlayer = false;
    }

    void Update()
    {
        CheckControl();

        if (isLocalPlayer)
        {
            PlayMovement();
        }
    }

    void CheckControl()
    {
        // Se não temos client ou ainda não recebemos ID, não permitimos controle local
        if (networkClient == null || networkClient.myId == -1)
        {
            isLocalPlayer = false;
            return;
        }

        // Definimos controle local se este paddle pertence a este cliente (myId == playerNumber)
        isLocalPlayer = (networkClient.myId == playerNumber);

        // Note: mesmo que gameStarted seja false, o jogador com ID pode mover seu paddle
        // (útil para posicionamento antes do START). A bola e scoring só ocorrem após gameStarted.
    }

    private void PlayMovement()
    {
        bool isPressingUp = false;
        bool isPressingDown = false;

        // Controles para cada jogador
        switch (playerNumber)
        {
            case 1: // Player 1 - W/S
                isPressingUp = Input.GetKey(KeyCode.W);
                isPressingDown = Input.GetKey(KeyCode.S);
                break;

            case 2: // Player 2 - Setas
                isPressingUp = Input.GetKey(KeyCode.UpArrow);
                isPressingDown = Input.GetKey(KeyCode.DownArrow);
                break;

            case 3: // Player 3 - T/G
                isPressingUp = Input.GetKey(KeyCode.T);
                isPressingDown = Input.GetKey(KeyCode.G);
                break;

            case 4: // Player 4 - I/K
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
            Rig.velocity = Vector2.zero;
            Rig.angularVelocity = 0f;
        }
        transform.position = startPosition;
    }
}
