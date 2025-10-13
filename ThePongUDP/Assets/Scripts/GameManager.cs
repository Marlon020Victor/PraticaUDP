using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
   
    [Header("Ball")]
    public GameObject ball;
    
    [Header("Player 1")]
    public GameObject player1Paddle;
    public GameObject player1Goal;
    
    [Header("Player 2")]
    public GameObject player2Paddle;
    public GameObject player2Goal;

    [Header("Score UI")] 
    public GameObject player1Text;
    public GameObject player2Text;
    
    private int player1Score;
    private int player2Score;
    
   
    public void Player1Scored()
    {
        if (player1Paddle)
        player1Score++;
        player1Text.GetComponent<TextMeshProUGUI>().text = player1Score.ToString();
        Debug.Log("O score mudou!");
    }
    
    public void Player2Scored()
    {
        if (player2Paddle)
        player2Score++;
        player2Text.GetComponent<TextMeshProUGUI>().text = player2Score.ToString();
        Debug.Log("O score mudou!");
    }

    private void ResetPosition()
    {
        ball.GetComponent<Ball>().Reset();
        player1Paddle.GetComponent<Player>().Reset();
        player2Paddle.GetComponent<Player>().Reset();
    }
    
}
