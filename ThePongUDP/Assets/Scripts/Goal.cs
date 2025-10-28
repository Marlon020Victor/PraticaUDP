using UnityEngine;

public class Goal : MonoBehaviour
{
    [Tooltip("Verdadeiro se este é o gol da ESQUERDA (defende o Time 1)")]
    public bool isLeftGoal;

    private PongClientUDP networkClient;
    private bool goalProcessed = false;

    void Start()
    {
        networkClient = FindFirstObjectByType<PongClientUDP>();
        
        if (networkClient == null)
        {
            Debug.LogError("[GOAL] NetworkClient NÃO encontrado!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;
        if (goalProcessed) return;

        // APENAS o Player 1 processa gols
        if (networkClient == null || networkClient.myId != 1) return;

        goalProcessed = true;

        // Determina qual time marcou
        int scoringTeam = isLeftGoal ? 2 : 1;
        
        Debug.Log($"[GOAL Player1] GOL! Time {scoringTeam} marcou!");

        // IMPORTANTE: NÃO atualiza placar localmente aqui
        // Apenas envia para o servidor, que fará broadcast para TODOS (inclusive este cliente)
        networkClient.SendGoalTeam(scoringTeam);

        // Reseta após 2 segundos
        Invoke(nameof(ResetAfterGoal), 2f);
    }

    void ResetAfterGoal()
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
        {
            goalProcessed = false;
        }
    }
}