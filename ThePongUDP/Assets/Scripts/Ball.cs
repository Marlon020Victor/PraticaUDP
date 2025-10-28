using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private Rigidbody2D Rig;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private float StartingSpeed = 8f;
    
    [Header("Física")]
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float speedIncrease = 1.05f; // Aumenta 5% a cada colisão
    [SerializeField] private float minSpeed = 5f; // Velocidade mínima

    void Start()
    {
        startPosition = transform.position;
        
        if (Rig == null)
            Rig = GetComponent<Rigidbody2D>();
            
        // Configurações CRÍTICAS do Rigidbody2D
        if (Rig != null)
        {
            Rig.gravityScale = 0; // Sem gravidade
            Rig.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // CRÍTICO: Evita atravessar
            Rig.interpolation = RigidbodyInterpolation2D.Interpolate; // Suaviza
            Rig.constraints = RigidbodyConstraints2D.FreezeRotation; // Não roda
            Rig.sleepMode = RigidbodySleepMode2D.NeverSleep; // Nunca dorme
        }
    }

    public void StartRoundAfter(float delaySec)
    {
        CancelInvoke();
        Invoke(nameof(BallInitialMovement), Mathf.Max(0f, delaySec));
    }

    void BallInitialMovement()
    {
        float x = Random.Range(0, 2) == 0 ? -1f : 1f;
        float y = Random.Range(-1f, 1f);
        Rig.linearVelocity = new Vector2(x * StartingSpeed, y * StartingSpeed);
    }

    // Chamado quando colide com algo
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Aumenta velocidade gradualmente
        Vector2 vel = Rig.linearVelocity;
        vel *= speedIncrease;
        
        // Limita velocidade máxima
        if (vel.magnitude > maxSpeed)
        {
            vel = vel.normalized * maxSpeed;
        }
        
        Rig.linearVelocity = vel;
        
        Debug.Log($"[BALL] Colidiu com {collision.gameObject.name}, velocidade: {vel.magnitude:F2}");
    }

    public void Reset()
    {
        Rig.linearVelocity = Vector2.zero;
        transform.position = startPosition;
        CancelInvoke();
    }
}