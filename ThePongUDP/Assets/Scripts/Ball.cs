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
        
        // Aguarda conexão para iniciar
        Invoke("CheckAndStart", 1f);
    }
    
    void CheckAndStart()
    {
        // Apenas o player 1 inicia a bola
        if (networkClient != null && networkClient.myId == 1)
        {
            BallInitialMovement();
        }
    }
    
    private void BallInitialMovement()
    {
        // Direção aleatória
        float x = Random.Range(0, 2) == 0 ? -1f : 1f;
        float y = Random.Range(-1f, 1f);
        
        Rig.linearVelocity = new Vector2(x * StartingSpeed, y * StartingSpeed);
    }
    
    public void Reset()
    {
        Rig.linearVelocity = Vector2.zero;
        transform.position = startPosition;
        
        // Aguarda um momento antes de reiniciar
        Invoke("BallInitialMovement", 1f);
    }
}