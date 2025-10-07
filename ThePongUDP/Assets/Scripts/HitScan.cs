using Unity.VisualScripting;
using UnityEngine;

public class HitScan : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnCollisionEnter2D(Collision2D collision) //score P1
    {
        if (collision.gameObject.tag == "Map Limit Left")
        {
            GameManager.instance.Player1Scored();
        }
        
        if (collision.gameObject.tag == "Map Limit Right")
        {
            GameManager.instance.Player2Scored();
        }
        
    }
    
    
}
