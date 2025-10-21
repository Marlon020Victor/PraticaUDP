using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    [Header("Ball")]
    public GameObject ball;
    
    [Header("Time 1 - Esquerdo (Players 1 & 3)")]
    public GameObject player1Paddle;
    public GameObject player3Paddle;
    public GameObject team1Goal;
    
    [Header("Time 2 - Direito (Players 2 & 4)")]
    public GameObject player2Paddle;
    public GameObject player4Paddle;
    public GameObject team2Goal;
    
    [Header("Score UI")] 
    public GameObject team1Text;
    public GameObject team2Text;
    
    [Header("Network")]
    public PongClientUDP networkClient;
    
    private int team1Score = 0; // Time Esquerdo (Players 1 + 3)
    private int team2Score = 0; // Time Direito (Players 2 + 4)
    public int maxScore = 5;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateScoreUI();
    }
    
    public void Team1Scored()
    {
        team1Score++;
        UpdateScoreUI();
        Debug.Log($"[GAMEMANAGER] Time 1 pontuou! Placar: {team1Score} x {team2Score}");
        CheckMaxScore();
    }
    
    public void Team2Scored()
    {
        team2Score++;
        UpdateScoreUI();
        Debug.Log($"[GAMEMANAGER] Time 2 pontuou! Placar: {team1Score} x {team2Score}");
        CheckMaxScore();
    }
    
    private void UpdateScoreUI()
    {
        if (team1Text != null)
        {
            TextMeshProUGUI tmp1 = team1Text.GetComponent<TextMeshProUGUI>();
            if (tmp1 != null)
            {
                tmp1.text = team1Score.ToString();
            }
        }
        
        if (team2Text != null)
        {
            TextMeshProUGUI tmp2 = team2Text.GetComponent<TextMeshProUGUI>();
            if (tmp2 != null)
            {
                tmp2.text = team2Score.ToString();
            }
        }
    }
    
    // Compatibilidade com código antigo
    public void Player1Scored()
    {
        Team1Scored();
    }
    
    public void Player2Scored()
    {
        Team2Scored();
    }
    
    private void CheckMaxScore()
    {
        if (team1Score >= maxScore)
        {
            Debug.Log($"[GAMEMANAGER] TIME 1 (ESQUERDO) VENCEU! {team1Score} x {team2Score}");
            Invoke("ResetMatch", 3f);
        }
        else if (team2Score >= maxScore)
        {
            Debug.Log($"[GAMEMANAGER] TIME 2 (DIREITO) VENCEU! {team1Score} x {team2Score}");
            Invoke("ResetMatch", 3f);
        }
    }
    
    private void ResetMatch()
    {
        Debug.Log("[GAMEMANAGER] Resetando partida...");
        ResetAllScores();
        
        // Apenas o player 1 envia comando de reset para a rede
        if (networkClient != null && networkClient.myId == 1)
        {
            networkClient.SendReset();
        }
    }
    
    private void ResetAllScores()
    {
        team1Score = 0;
        team2Score = 0;
        UpdateScoreUI();
        ResetPositions();
    }
    
    private void ResetPositions()
    {
        if (ball != null)
        {
            Ball ballScript = ball.GetComponent<Ball>();
            if (ballScript != null)
            {
                ballScript.Reset();
            }
        }
        
        if (player1Paddle != null)
        {
            Player p1 = player1Paddle.GetComponent<Player>();
            if (p1 != null) p1.Reset();
        }
        
        if (player2Paddle != null)
        {
            Player p2 = player2Paddle.GetComponent<Player>();
            if (p2 != null) p2.Reset();
        }
        
        if (player3Paddle != null)
        {
            Player p3 = player3Paddle.GetComponent<Player>();
            if (p3 != null) p3.Reset();
        }
        
        if (player4Paddle != null)
        {
            Player p4 = player4Paddle.GetComponent<Player>();
            if (p4 != null) p4.Reset();
        }
    }
}