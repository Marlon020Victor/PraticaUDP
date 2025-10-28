using UnityEngine;
using TMPro;

/// <summary>
/// Gerencia placar e reset de partida localmente.
/// A atualização do placar deve vir da rede (GOAL/SCORE),
/// mas deixamos métodos Team1Scored/Team2Scored para quando a
/// mensagem chegar, e para testes locais.
/// </summary>
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

    private int team1Score = 0; // Esquerda
    private int team2Score = 0; // Direita
    public int maxScore = 5;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
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
            var tmp1 = team1Text.GetComponent<TextMeshProUGUI>();
            if (tmp1 != null) tmp1.text = team1Score.ToString();
        }

        if (team2Text != null)
        {
            var tmp2 = team2Text.GetComponent<TextMeshProUGUI>();
            if (tmp2 != null) tmp2.text = team2Score.ToString();
        }
    }

    // Compat com código antigo
    public void Player1Scored() => Team1Scored();
    public void Player2Scored() => Team2Scored();

    private void CheckMaxScore()
    {
        if (team1Score >= maxScore)
        {
            Debug.Log($"[GAMEMANAGER] TIME 1 (ESQUERDO) VENCEU! {team1Score} x {team2Score}");
            Invoke(nameof(ResetMatch), 3f);
        }
        else if (team2Score >= maxScore)
        {
            Debug.Log($"[GAMEMANAGER] TIME 2 (DIREITO) VENCEU! {team1Score} x {team2Score}");
            Invoke(nameof(ResetMatch), 3f);
        }
    }

    private void ResetMatch()
    {
        Debug.Log("[GAMEMANAGER] Resetando partida...");
        ResetAllScores();

        // Apenas o Player 1 pede RESET pra sincronizar a rodada
        if (networkClient != null && networkClient.myId == 1)
            networkClient.SendReset();
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
            var ballScript = ball.GetComponent<Ball>();
            if (ballScript != null)
                ballScript.ResetBall();   // <<< nome correto
        }

        if (player1Paddle != null)
        {
            var p1 = player1Paddle.GetComponent<Player>();
            if (p1 != null) p1.Reset();
        }

        if (player2Paddle != null)
        {
            var p2 = player2Paddle.GetComponent<Player>();
            if (p2 != null) p2.Reset();
        }

        if (player3Paddle != null)
        {
            var p3 = player3Paddle.GetComponent<Player>();
            if (p3 != null) p3.Reset();
        }

        if (player4Paddle != null)
        {
            var p4 = player4Paddle.GetComponent<Player>();
            if (p4 != null) p4.Reset();
        }
    }
}
