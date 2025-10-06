using UnityEngine;

public class Ball : MonoBehaviour
{
    
    [SerializeField]
    private Rigidbody2D Rig;

    [SerializeField] 
    private float StartingSpeed;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       BallInitialMovement();
    }
    
    private void BallInitialMovement()
    {
        bool isRight = UnityEngine.Random.value >= 1f;

        float xVelocity = -1f;

        if (isRight == true)
        {
            xVelocity = 1f;
        }
        
        float yVelocity = UnityEngine.Random.Range(-1f, 1f);
        
        Rig.velocity = new Vector2(xVelocity * StartingSpeed, yVelocity * StartingSpeed);
    }
    
    private void BallInitialMovement2()
    {
       float x = Random.Range(0,2) == 0 ? -1 : 1;
       float y = Random.Range(0,2) == 0 ? -1 : 1;
       Rig.velocity = new Vector2(x * StartingSpeed, y * StartingSpeed);
    }
    
}
