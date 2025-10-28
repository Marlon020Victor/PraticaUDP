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
            PlayMovement();
    }

    void PlayMovement()
    {
        bool up = false, down = false;

        switch (playerNumber)
        {
            case 1: up = Input.GetKey(KeyCode.UpArrow);   down = Input.GetKey(KeyCode.DownArrow); break;
            case 2: up = Input.GetKey(KeyCode.W);         down = Input.GetKey(KeyCode.S);         break;
            case 3: up = Input.GetKey(KeyCode.T);         down = Input.GetKey(KeyCode.G);         break;
            case 4: up = Input.GetKey(KeyCode.Y);         down = Input.GetKey(KeyCode.H);         break;
        }

        if (up)   transform.Translate(Vector2.up * MoveSpeed * Time.deltaTime);
        if (down) transform.Translate(Vector2.down * MoveSpeed * Time.deltaTime);
    }

    public void Reset()
    {
        if (Rig != null) Rig.linearVelocity = Vector2.zero;
        transform.position = startPosition;
    }
}