using UnityEngine;

public class Goal : MonoBehaviour
{
    [Tooltip("Verdadeiro se este é o gol da ESQUERDA (defende o Team 1)")]
    public bool isLeftGoal;

    private PongClientUDP networkClient;
    private bool goalProcessed = false;

    void Start()
    {
        networkClient = FindFirstObjectByType<PongClientUDP>();
    }

    private void OnColliderEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Ball") || goalProcessed) return;
        goalProcessed = true;

        if (networkClient != null && networkClient.myId == 1)
        {
            int scoringTeam = isLeftGoal ? 2 : 1; // entrou no gol da esquerda → ponto do time da direita
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                if (scoringTeam == 1) gm.Team1Scored();
                else gm.Team2Scored();
            }

            networkClient.SendGoalTeam(scoringTeam);
            Invoke(nameof(ResetAfterGoal), 2f);
        }
    }

    void ResetAfterGoal()
    {
        if (networkClient != null && networkClient.myId == 1)
            networkClient.SendReset();
        goalProcessed = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Ball")) goalProcessed = false;
    }
}