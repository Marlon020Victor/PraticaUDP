using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] 
    private float MoveSpeed;
    

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
    
}
