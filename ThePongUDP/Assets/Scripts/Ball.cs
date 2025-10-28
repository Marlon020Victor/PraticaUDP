using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private Rigidbody2D Rig;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private float StartingSpeed = 8f;

    void Start()
    {
        startPosition = transform.position;
        // Não inicia aqui — quem chama é o cliente (ID1) após START
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

    public void Reset()
    {
        Rig.linearVelocity = Vector2.zero;
        transform.position = startPosition;
        // Round será reiniciado pelo cliente (ID1) após RESET broadcast
    }
}