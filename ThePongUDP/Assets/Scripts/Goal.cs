using UnityEngine;

public class Goal : MonoBehaviour
{
    [Tooltip("Verdadeiro se este gol é o da ESQUERDA (protege o Time A)")]
    public bool isLeftGoal;

    private PongClientUDP networkClient;
    private bool goalProcessed = false;

    private void Start()
    {
        networkClient = FindFirstObjectByType<PongClientUDP>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Ball") || goalProcessed) return;

        goalProcessed = true;

        // Só o player 1 decide/manda a mensagem de gol (autoridade)
        if (networkClient != null && networkClient.myId == 1)
        {
            // LeftGoal sofre gol => Team B (2) pontua
            // RightGoal sofre gol => Team A (1) pontua
            int scoringTeam = isLeftGoal ? 2 : 1;

            // atualiza local
            if (scoringTeam == 1) GameObject.Find("GameManager").GetComponent<GameManager>().Team1Scored();
            else GameObject.Find("GameManager").GetComponent<GameManager>().Team2Scored();

            // notifica geral
            networkClient.SendGoalTeam(scoringTeam);

            // dá um tempinho e reseta
            Invoke(nameof(ResetAfterGoal), 2f);
        }
    }

    private void ResetAfterGoal()
    {
        if (networkClient != null && networkClient.myId == 1)
        {
            networkClient.SendReset();
        }
        goalProcessed = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
            goalProcessed = false;
    }
}