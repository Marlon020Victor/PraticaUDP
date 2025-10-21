using UnityEngine;

public class Goal : MonoBehaviour
{
    public bool isTeam1Goal; // true = gol na área do Time 1 (esquerda), false = gol na área do Time 2 (direita)
    private PongClientUDP networkClient;
    private bool goalProcessed = false;
    
    private void Start()
    {
        networkClient = FindFirstObjectByType<PongClientUDP>();
        
        if (networkClient == null)
        {
            Debug.LogError("[GOAL] PongClientUDP não encontrado!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;
        if (goalProcessed) return;
        
        // Apenas o player 1 tem autoridade para registrar gols
        if (networkClient == null || networkClient.myId != 1) return;
        if (!networkClient.gameStarted) return;
        
        goalProcessed = true;
        
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("[GOAL] GameManager não encontrado!");
            goalProcessed = false;
            return;
        }
        
        if (isTeam1Goal)
        {
            // Bola entrou no gol do Time 1 (esquerda)
            // Time 2 (direita - Players 2+4) marcou
            Debug.Log("[GOAL] Time 2 (Players 2+4) MARCOU!");
            gm.Team2Scored();
            networkClient.SendGoalScored(2);
        }
        else
        {
            // Bola entrou no gol do Time 2 (direita)
            // Time 1 (esquerda - Players 1+3) marcou
            Debug.Log("[GOAL] Time 1 (Players 1+3) MARCOU!");
            gm.Team1Scored();
            networkClient.SendGoalScored(1);
        }
        
        // Reset após 2 segundos
        Invoke("ResetAfterGoal", 2f);
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
        {
            CancelInvoke("ResetAfterGoal");
            goalProcessed = false;
        }
    }
}