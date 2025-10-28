using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private Rigidbody2D Rig;
    public float MoveSpeed = 10f;
    public Vector3 startPosition;

    [Header("Multiplayer")]
    public bool isLocalPlayer = true;
    public int playerNumber = 1; // 1..4

    private PongClientUDP networkClient;
    private float lastSendTime = 0f;
    private float sendRate = 0.05f; // Envia posição 20x por segundo

    void Start()
    {
        startPosition = transform.position;
        networkClient = FindFirstObjectByType<PongClientUDP>();
    }

    void Update()
    {
        if (networkClient != null)
            isLocalPlayer = (networkClient.myId == playerNumber);

        if (isLocalPlayer)
        {
            PlayMovement();
            
            // Envia a posição para a rede
            if (Time.time - lastSendTime > sendRate)
            {
                SendPosition();
                lastSendTime = Time.time;
            }
        }
    }

    void PlayMovement()
    {
        bool up = false, down = false;

        switch (playerNumber)
        {
            case 1: 
                up = Input.GetKey(KeyCode.UpArrow);   
                down = Input.GetKey(KeyCode.DownArrow); 
                break;
            case 2: 
                up = Input.GetKey(KeyCode.W);         
                down = Input.GetKey(KeyCode.S);         
                break;
            case 3: 
                up = Input.GetKey(KeyCode.T);         
                down = Input.GetKey(KeyCode.G);         
                break;
            case 4: 
                up = Input.GetKey(KeyCode.I);         
                down = Input.GetKey(KeyCode.K);         
                break;
        }

        if (up)   transform.Translate(Vector2.up * MoveSpeed * Time.deltaTime);
        if (down) transform.Translate(Vector2.down * MoveSpeed * Time.deltaTime);
    }

    void SendPosition()
    {
        if (networkClient != null && networkClient.myId == playerNumber)
        {
            networkClient.SendPaddlePosition(playerNumber, transform.position.y);
        }
    }

    public void Reset()
    {
        if (Rig != null) Rig.linearVelocity = Vector2.zero;
        transform.position = startPosition;
    }
}