using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    [Header("Ball")]
    public GameObject ball;
    
    [Header("Team 1 - Left Side (Players 1 & 3)")]
    public GameObject player1Paddle;
    public GameObject player3Paddle; // NOVO
    public GameObject team1Goal;
    
    [Header("Team 2 - Right Side (Players 2 & 4)")]
    public GameObject player2Paddle;
    public GameObject player4Paddle; // NOVO
    public GameObject team2Goal;
    
    [Header("Score UI")] 
    public GameObject team1Text;
    public GameObject team2Text;
    
    [Header("Network")]
    public PongClientUDP networkClient;
    
    private int team1Score; // Time Esquerdo (Players 1 + 3)
    private int team2Score; // Time Direito (Players 2 + 4)
    public int maxScore = 5; // pontuação máxima antes do reset
    
    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    
    public void Team1Scored()
    {
        team1Score++;
        team1Text.GetComponent<TextMeshProUGUI>().text = team1Score.ToString();
        Debug.Log($"Time 1 (Esquerdo) pontuou! Placar: {team1Score} x {team2Score}");
        CheckMaxScore();
    }
    
    public void Team2Scored()
    {
        team2Score++;
        team2Text.GetComponent<TextMeshProUGUI>().text = team2Score.ToString();
        Debug.Log($"Time 2 (Direito) pontuou! Placar: {team1Score} x {team2Score}");
        CheckMaxScore();
    }
    
    // Mantém compatibilidade com código antigo
    public void Player1Scored()
    {
        Team1Scored();
    }
    
    public void Player2Scored()
    {
        Team2Scored();
    }
    
    // Checa se algum time atingiu a pontuação máxima
    private void CheckMaxScore()
    {
        if (team1Score >= maxScore || team2Score >= maxScore)
        {
            string winner = team1Score >= maxScore ? "Time 1 (Esquerdo)" : "Time 2 (Direito)";
            Debug.Log($"{winner} venceu a partida!");
            
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
        team1Score = 0;
        team2Score = 0;
        team1Text.GetComponent<TextMeshProUGUI>().text = "0";
        team2Text.GetComponent<TextMeshProUGUI>().text = "0";
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
        if (player3Paddle != null)
            player3Paddle.GetComponent<Player>().Reset();
        if (player4Paddle != null)
            player4Paddle.GetComponent<Player>().Reset();
    }
}