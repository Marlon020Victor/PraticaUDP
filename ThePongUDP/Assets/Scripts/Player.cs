using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private Rigidbody2D Rig;
    public float MoveSpeed = 10f;
    public Vector3 startPosition;

    [Header("Multiplayer")]
    public int playerNumber = 1; // 1..4

    private PongClientUDP networkClient;
    private float lastSendTime = 0f;
    private float sendRate = 0.05f;
    private float lastY = 0f;

    void Start()
    {
        startPosition = transform.position;
        networkClient = FindFirstObjectByType<PongClientUDP>();
        
        if (networkClient == null)
        {
            Debug.LogError($"[Player {playerNumber}] NetworkClient não encontrado!");
        }
        
        Debug.Log($"[Player {playerNumber}] Inicializado na posição {transform.position}");
    }

    void Update()
    {
        if (networkClient == null) return;

        // Verifica se este paddle pertence a este cliente
        bool isLocalPlayer = (networkClient.myId == playerNumber);

        if (isLocalPlayer)
        {
            PlayMovement();
            
            // Envia posição apenas se mudou significativamente
            if (Time.time - lastSendTime > sendRate)
            {
                if (Mathf.Abs(transform.position.y - lastY) > 0.01f)
                {
                    SendPosition();
                    lastY = transform.position.y;
                }
                lastSendTime = Time.time;
            }
        }
    }

    void PlayMovement()
    {
        bool up = false, down = false;

        // CADA PLAYER TEM CONTROLES ÚNICOS
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

        if (up)   
        {
            transform.Translate(Vector2.up * MoveSpeed * Time.deltaTime);
            Debug.Log($"[Player {playerNumber}] Movendo para cima: Y={transform.position.y:F2}");
        }
        if (down) 
        {
            transform.Translate(Vector2.down * MoveSpeed * Time.deltaTime);
            Debug.Log($"[Player {playerNumber}] Movendo para baixo: Y={transform.position.y:F2}");
        }
    }

    void SendPosition()
    {
        if (networkClient != null && networkClient.myId == playerNumber)
        {
            float y = transform.position.y;
            networkClient.SendPaddlePosition(playerNumber, y);
            Debug.Log($"[Player {playerNumber}] Enviando posição Y={y:F2} para servidor");
        }
    }

    public void Reset()
    {
        if (Rig != null) Rig.linearVelocity = Vector2.zero;
        transform.position = startPosition;
        lastY = startPosition.y;
    }
}