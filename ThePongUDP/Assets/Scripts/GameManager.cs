using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Ball")]
    public GameObject ball;

    [Header("Team A (Left)")]
    public GameObject paddle1;
    public GameObject paddle3;
    public GameObject leftGoal; // opcional (referência na cena)

    [Header("Team B (Right)")]
    public GameObject paddle2;
    public GameObject paddle4;
    public GameObject rightGoal; // opcional (referência na cena)

    [Header("Score UI")]
    public GameObject team1Text;
    public GameObject team2Text;

    [Header("Network")]
    public PongClientUDP networkClient; // atribuir no inspetor

    private int team1Score;
    private int team2Score;
    public int maxScore = 5;

    public void Team1Scored()
    {
        team1Score++;
        if (team1Text) team1Text.GetComponent<TextMeshProUGUI>().text = team1Score.ToString();
        CheckMaxScore();
    }

    public void Team2Scored()
    {
        team2Score++;
        if (team2Text) team2Text.GetComponent<TextMeshProUGUI>().text = team2Score.ToString();
        CheckMaxScore();
    }

    private void CheckMaxScore()
    {
        if (team1Score >= maxScore || team2Score >= maxScore)
        {
            ResetAllScores();

            if (networkClient != null && networkClient.myId == 1)
            {
                networkClient.SendReset();
            }
        }
    }

    private void ResetAllScores()
    {
        team1Score = 0;
        team2Score = 0;

        if (team1Text) team1Text.GetComponent<TextMeshProUGUI>().text = "0";
        if (team2Text) team2Text.GetComponent<TextMeshProUGUI>().text = "0";

        ResetPositions();
    }

    private void ResetPositions()
    {
        if (ball) ball.GetComponent<Ball>().Reset();

        if (paddle1) paddle1.GetComponent<Player>().Reset();
        if (paddle2) paddle2.GetComponent<Player>().Reset();
        if (paddle3) paddle3.GetComponent<Player>().Reset();
        if (paddle4) paddle4.GetComponent<Player>().Reset();
    }
}
