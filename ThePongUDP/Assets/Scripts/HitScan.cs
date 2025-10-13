using System;
using Unity.VisualScripting;
using UnityEngine;

public class HitScan : MonoBehaviour
{
    public GameObject Game;
    public GameManager gameManager;
    private void Start()
    {
        GameManager gameManager = Game.GetComponent<GameManager>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnCollisionEnter2D(Collision2D collision) //score P1
    {
        if (collision.gameObject.tag == "Map Limit Left")
        {
            gameManager.Player1Scored();
        }
        
        if (collision.gameObject.tag == "Map Limit Right")
        {
            gameManager.Player2Scored();
        }
        
    }
    
    
}
