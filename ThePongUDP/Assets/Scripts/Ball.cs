using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D Rig;

    [SerializeField] 
    private Vector3 startPosition;

    [SerializeField]
    private float StartingSpeed = 8f;

    private PongClientUDP networkClient;
    private bool hasStarted = false;

    void Start()
    {
        startPosition = transform.position;
        networkClient = FindFirstObjectByType<PongClientUDP>();

        if (networkClient == null)
        {
            Debug.LogError("[BALL] PongClientUDP não encontrado!");
        }

        // Aguarda o jogo começar
        InvokeRepeating("CheckAndStart", 1f, 0.5f);
    }

    void CheckAndStart()
    {
        if (hasStarted) return;
        
        // Só inicia se:
        // 1. Eu sou o player 1 (autoridade)
        // 2. O jogo começou (START recebido)
        // 3. Tem pelo menos 2 jogadores
        if (networkClient != null && 
            networkClient.myId == 1 && 
            networkClient.gameStarted &&
            networkClient.totalPlayersConnected >= 2)
        {
            BallInitialMovement();
            hasStarted = true;
            CancelInvoke("CheckAndStart");
            Debug.Log("[BALL] Bola iniciada!");
        }
    }

    private void BallInitialMovement()
    {
        // Direção aleatória
        float x = Random.Range(0, 2) == 0 ? -1f : 1f;
        float y = Random.Range(-1f, 1f);

        if (Rig != null)
        {
            Rig.linearVelocity = Vector2.zero;
            Rig.angularVelocity = 0f;
            Rig.linearVelocity = new Vector2(x * StartingSpeed, y * StartingSpeed);
        }
        
        Debug.Log($"[BALL] Velocidade inicial: ({x * StartingSpeed}, {y * StartingSpeed})");
    }

    public void Reset()
    {
        Debug.Log("[BALL] Reset chamado");
        
        if (Rig != null)
        {
            Rig.linearVelocity = Vector2.zero;
            Rig.angularVelocity = 0f;
        }
        
        transform.position = startPosition;
        hasStarted = false;

        // Reinicia após 1 segundo
        CancelInvoke("CheckAndStart");
        InvokeRepeating("CheckAndStart", 1f, 0.5f);
    }
}