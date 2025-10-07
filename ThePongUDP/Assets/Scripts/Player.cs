using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] 
    private Rigidbody2D Rig;
    private float MoveSpeed;
    public Vector3 startPosition;


    private void Start()
    {
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        PlayMovement();
    }

    private void PlayMovement()
    {
        bool isPressingUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool isPressingDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        if (isPressingUp)
        {
            transform.Translate(Vector2.up * MoveSpeed * Time.deltaTime);
        }

        if (isPressingDown)
        {
            transform.Translate(Vector2.down * MoveSpeed * Time.deltaTime);
        }
    }

    public void Reset()
    {
        Rig.velocity = Vector2.zero;
        transform.position = startPosition;
    }
    
}
