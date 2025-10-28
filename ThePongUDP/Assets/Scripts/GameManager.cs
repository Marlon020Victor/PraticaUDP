using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Ball")]
    public GameObject ball;

    [Header("Team 1 (Left)")]
    public GameObject paddle1;
    public GameObject paddle3;

    [Header("Team 2 (Right)")]
    public GameObject paddle2;
    public GameObject paddle4;

    [Header("Score UI")]
    public TextMeshProUGUI team1Text;
    public TextMeshProUGUI team2Text;

    [Header("Network")]
    public PongClientUDP networkClient;

    private int team1Score;
    private int team2Score;
    public int maxScore = 5;

    public void Team1Scored()
    {
        team1Score++;
        if (team1Text) team1Text.text = team1Score.ToString();
        CheckMaxScore();
    }

    public void Team2Scored()
    {
        team2Score++;
        if (team2Text) team2Text.text = team2Score.ToString();
        CheckMaxScore();
    }

    void CheckMaxScore()
    {
        if (team1Score >= maxScore || team2Score >= maxScore)
        {
            ResetAllScores();
            if (networkClient != null && networkClient.myId == 1)
                networkClient.SendReset();
        }
    }

    void ResetAllScores()
    {
        team1Score = 0; team2Score = 0;
        if (team1Text) team1Text.text = "0";
        if (team2Text) team2Text.text = "0";
        ResetPositions();
    }

    void ResetPositions()
    {
        if (ball) ball.GetComponent<Ball>().Reset();
        if (paddle1) paddle1.GetComponent<Player>().Reset();
        if (paddle2) paddle2.GetComponent<Player>().Reset();
        if (paddle3) paddle3.GetComponent<Player>().Reset();
        if (paddle4) paddle4.GetComponent<Player>().Reset();
    }
}