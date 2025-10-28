using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Ball : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody2D Rig;

    [Header("Config")]
    [SerializeField] private float StartingSpeed = 8f;

    [Header("Debug")]
    public bool debugVerbose = true;

    private Vector3 startPosition;
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
        // Mantemos a verificação periódica como fallback
        InvokeRepeating(nameof(CheckAndStart), 0.2f, 0.2f);
    }

    void CheckAndStart()
    {
        if (hasStarted || networkClient == null) return;

        if (networkClient.gameStarted && networkClient.totalPlayersConnected >= 2)
        {
            if (networkClient.myId == 1)
            {
                BallInitialMovement();
            }
            hasStarted = true;
            CancelInvoke(nameof(CheckAndStart));
        }
    }

    public void StartAsAuthoritative()
    {
        // Chamado pelo cliente ID 1 assim que recebe START
        if (networkClient == null) networkClient = FindFirstObjectByType<PongClientUDP>();
        if (networkClient != null && networkClient.myId == 1)
        {
            if (debugVerbose) Debug.Log("[BALL] StartAsAuthoritative()");
            BallInitialMovement();
            hasStarted = true;
            CancelInvoke(nameof(CheckAndStart));
        }
    }

    public void MarkAsNonAuthoritativeClient()
    {
        // Chamado pelos clientes != 1 assim que recebem START
        if (debugVerbose) Debug.Log("[BALL] MarkAsNonAuthoritativeClient()");
        hasStarted = true; // para não tentar lançar localmente
        // mantemos o Invoke cancelado — vamos só seguir estado remoto
        CancelInvoke(nameof(CheckAndStart));
    }

    private void BallInitialMovement()
    {
        float x = Random.Range(0, 2) == 0 ? -1f : 1f;
        float y = Random.Range(-1f, 1f);

        Rig.linearVelocity = Vector2.zero;
        Rig.angularVelocity = 0f;
        Rig.WakeUp();
        Rig.linearVelocity = new Vector2(x * StartingSpeed, y * StartingSpeed);

        if (debugVerbose)
            Debug.Log($"[BALL] Lançada com velocidade ({Rig.linearVelocity.x:F2}, {Rig.linearVelocity.y:F2})");
    }

    public void Reset()
    {
        hasStarted = false;
        transform.position = startPosition;
        Rig.linearVelocity = Vector2.zero;
        Rig.angularVelocity = 0f;
        Rig.WakeUp();

        // recomeça a verificação — START pode vir de novo depois de gols/reset
        CancelInvoke(nameof(CheckAndStart));
        InvokeRepeating(nameof(CheckAndStart), 0.2f, 0.2f);

        if (debugVerbose) Debug.Log("[BALL] Reset()");
    }
}
