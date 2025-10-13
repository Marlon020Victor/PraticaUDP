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

    [Header("Network")]
    public PongClientUDP networkClient; // atribuir no inspetor

    private int player1Score;
    private int player2Score;
    public int maxScore; // pontuação máxima antes do reset

    public void Player1Scored()
    {
        if (player1Paddle)
            player1Score++;

        player1Text.GetComponent<TextMeshProUGUI>().text = player1Score.ToString();
        Debug.Log("O score mudou!");

        CheckMaxScore();
    }

    public void Player2Scored()
    {
        if (player2Paddle)
            player2Score++;

        player2Text.GetComponent<TextMeshProUGUI>().text = player2Score.ToString();
        Debug.Log("O score mudou!");

        CheckMaxScore();
    }

    // Checa se algum jogador atingiu a pontuação máxima
    private void CheckMaxScore()
    {
        if (player1Score >= maxScore || player2Score >= maxScore)
        {
            ResetAllScores();

            // Apenas o player 1 envia o comando de reset para todos via rede
            if (networkClient != null && networkClient.myId == 1)
            {
                networkClient.SendReset();
            }
        }
    }

    // Reseta as pontuações
    private void ResetAllScores()
    {
        player1Score = 0;
        player2Score = 0;

        player1Text.GetComponent<TextMeshProUGUI>().text = "0";
        player2Text.GetComponent<TextMeshProUGUI>().text = "0";

        ResetPosition();
    }

    // Reseta a posição da bola e paddles
    private void ResetPosition()
    {
        if (ball != null)
            ball.GetComponent<Ball>().Reset();

        if (player1Paddle != null)
            player1Paddle.GetComponent<Player>().Reset();

        if (player2Paddle != null)
            player2Paddle.GetComponent<Player>().Reset();
    }
}
