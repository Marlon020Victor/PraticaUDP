using UnityEngine;

/// <summary>
/// Controla kickoff e reset da bola no CLIENTE.
/// Somente o cliente de ID 1 inicia o movimento após START e >=2 jogadores.
/// </summary>
public class Ball : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Rigidbody2D Rig;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private float StartingSpeed = 8f;

    private PongClientUDP networkClient;
    private bool hasStarted = false;

    private void Awake()
    {
        networkClient = FindObjectOfType<PongClientUDP>();
        if (Rig == null)
        {
            Rig = GetComponent<Rigidbody2D>();
            if (Rig == null)
                Debug.LogError("[BALL] Rigidbody2D não encontrado. Arraste no campo 'Rig'.");
        }
        if (startPosition == Vector3.zero)
            startPosition = transform.position;
    }

    private void OnEnable()
    {
        CancelInvoke(nameof(CheckAndStart));
        InvokeRepeating(nameof(CheckAndStart), 0.75f, 0.5f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(CheckAndStart));
    }

    private void CheckAndStart()
    {
        if (hasStarted) return;
        if (networkClient == null) return;
        if (!networkClient.gameStarted) return;
        if (networkClient.myId != 1) return;
        if (networkClient.totalPlayersConnected < 2) return;

        Debug.Log($"[BALL] Kickoff autorizado (myId={networkClient.myId}, players={networkClient.totalPlayersConnected}).");
        BallInitialMovement();
        hasStarted = true;
    }

    private void BallInitialMovement()
    {
        if (Rig == null) return;

        Rig.linearVelocity = Vector2.zero;   // usar velocity (compat)
        Rig.angularVelocity = 0f;
        Rig.WakeUp();

        int x = Random.value < 0.5f ? -1 : 1;
        float y = Random.Range(-0.7f, 0.7f);
        Vector2 dir = new Vector2(x, y).normalized;

        Rig.linearVelocity = dir * StartingSpeed;

        Debug.Log($"[BALL] Bola iniciada! Vel={Rig.linearVelocity}, pos={transform.position}");
    }

    public void ResetBall()
    {
        if (Rig != null)
        {
            Rig.linearVelocity = Vector2.zero;
            Rig.angularVelocity = 0f;
            Rig.WakeUp();
        }

        transform.position = startPosition;
        hasStarted = false;

        CancelInvoke(nameof(CheckAndStart));
        InvokeRepeating(nameof(CheckAndStart), 1f, 0.5f);

        Debug.Log("[BALL] Reset efetuado; aguardando kickoff.");
    }
}
