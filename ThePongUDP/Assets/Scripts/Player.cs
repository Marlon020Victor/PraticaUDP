using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private Rigidbody2D Rig;
    private Vector2 move;
    public bool isLocalPlayer = false;

    void Awake()
    {
        Rig = GetComponent<Rigidbody2D>();
        Rig.gravityScale = 0;
        Rig.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        move.y = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            Rig.linearVelocity = move * speed;
        }
    }

    public void ApplyRemotePosition(float targetY)
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * 15f);
        transform.position = pos;
    }

    public void Reset()
    {
        Rig.linearVelocity = Vector2.zero;
        Rig.angularVelocity = 0f;
    }
}