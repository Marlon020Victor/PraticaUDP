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

        if (networkClient == null)
        {
            Debug.LogError($"[PLAYER {playerNumber}] PongClientUDP não encontrado!");
        }

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
        if (networkClient == null || networkClient.myId == -1)
        {
            isLocalPlayer = false;
            return;
        }

        // Este paddle é controlado localmente se o myId corresponde ao playerNumber
        isLocalPlayer = (networkClient.myId == playerNumber);
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

        float vertical = 0f;
        if (isPressingUp) vertical = 1f;
        if (isPressingDown) vertical = -1f;

        if (vertical != 0f)
        {
            transform.Translate(Vector2.up * vertical * MoveSpeed * Time.deltaTime);
        }
    }

    public void Reset()
    {
        if (Rig != null)
        {
            Rig.linearVelocity = Vector2.zero;
            Rig.angularVelocity = 0f;
        }
        
        transform.position = startPosition;
        Debug.Log($"[PLAYER {playerNumber}] Reset para posição inicial");
    }
}