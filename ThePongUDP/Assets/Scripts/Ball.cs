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

    void Start()
    {
        startPosition = transform.position;
        networkClient = FindFirstObjectByType<PongClientUDP>();

        // Aguarda conexão / START para iniciar (verifica periodicamente)
        Invoke("CheckAndStart", 1f);
    }

    void CheckAndStart()
    {
        // Só inicia a bola se eu for o player 1 (autoridade) e o jogo realmente começou
        if (networkClient != null && networkClient.myId == 1 && networkClient.gameStarted)
        {
            BallInitialMovement();
        }
        else
        {
            // tenta novamente enquanto não tiver autorização para iniciar
            Invoke("CheckAndStart", 1f);
        }
    }

    private void BallInitialMovement()
    {
        // Direção aleatória
        float x = Random.Range(0, 2) == 0 ? -1f : 1f;
        float y = Random.Range(-1f, 1f);

        if (Rig != null)
        {
            Rig.linearVelocity = Vector2.zero; // garante
            Rig.linearVelocity = new Vector2(x * StartingSpeed, y * StartingSpeed);
        }
        else
        {
            transform.position += new Vector3(x * StartingSpeed * 0.01f, y * StartingSpeed * 0.01f, 0f);
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

        // Reinicia depois que o jogo mandar, aqui faz apenas espera curta e tenta iniciar (CheckAndStart fará verificação)
        Invoke("CheckAndStart", 1f);
    }
}