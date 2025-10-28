using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D Rig;
    public float MoveSpeed = 10f;
    public Vector3 startPosition;

    [Header("Multiplayer")]
    public bool isLocalPlayer = true;
    public int playerNumber = 1; // agora pode ser 1..4

    private PongClientUDP networkClient;

    private void Start()
    {
        startPosition = transform.position;
        networkClient = FindFirstObjectByType<PongClientUDP>();
    }

    void Update()
    {
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
        // como cada cliente controla um id único, as teclas podem ser as mesmas
        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        if (up) transform.Translate(Vector2.up * MoveSpeed * Time.deltaTime);
        if (down) transform.Translate(Vector2.down * MoveSpeed * Time.deltaTime);
    }

    public void Reset()
    {
        if (Rig != null) Rig.linearVelocity = Vector2.zero;
        transform.position = startPosition;
    }
}