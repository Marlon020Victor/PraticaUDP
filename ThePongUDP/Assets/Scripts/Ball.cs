using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Ball : MonoBehaviour
{
    [SerializeField] private Rigidbody2D Rig;
    [SerializeField] private float StartingSpeed = 8f;
    [SerializeField] private Vector3 startPosition;

    private PongClientUDP networkClient;
    private bool hasStarted = false;

    void Awake()
    {
        if (!Rig) Rig = GetComponent<Rigidbody2D>();
        Rig.bodyType = RigidbodyType2D.Dynamic;
        Rig.gravityScale = 0f;
        Rig.simulated = true;
        Rig.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Rig.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    void Start()
    {
        startPosition = transform.position;
        networkClient = FindFirstObjectByType<PongClientUDP>();
        InvokeRepeating(nameof(CheckAndStart), 0.2f, 0.2f);
    }

    void CheckAndStart()
    {
        if (hasStarted || networkClient == null) return;

        if (networkClient.gameStarted && networkClient.totalPlayersConnected >= 2)
        {
            // Apenas player 1 lança a bola
            if (networkClient.myId == 1)
            {
                BallInitialMovement();
            }
            hasStarted = true;
            CancelInvoke(nameof(CheckAndStart));
        }
    }

    private void BallInitialMovement()
    {
        float x = Random.Range(0, 2) == 0 ? -1f : 1f;
        float y = Random.Range(-1f, 1f);

        Rig.linearVelocity = Vector2.zero;
        Rig.angularVelocity = 0f;
        Rig.WakeUp();
        Rig.linearVelocity = new Vector2(x * StartingSpeed, y * StartingSpeed);
        Debug.Log($"[BALL] Lançada com velocidade ({Rig.linearVelocity.x:F2}, {Rig.linearVelocity.y:F2})");
    }

    public void Reset()
    {
        hasStarted = false;
        transform.position = startPosition;
        Rig.linearVelocity = Vector2.zero;
        Rig.angularVelocity = 0f;
        Rig.WakeUp();

        CancelInvoke(nameof(CheckAndStart));
        InvokeRepeating(nameof(CheckAndStart), 0.2f, 0.2f);
    }
}
