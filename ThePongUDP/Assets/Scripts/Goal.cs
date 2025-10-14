using UnityEngine;

public class Goal : MonoBehaviour
{
    public bool isTeam1Goal; // true = gol do Time 1 (esquerda), false = gol do Time 2 (direita)
    private PongClientUDP networkClient;
    private bool goalProcessed = false;
    
    private void Start()
    {
        networkClient = FindFirstObjectByType<PongClientUDP>();
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Ball") && !goalProcessed)
        {
            goalProcessed = true;
            
            // Apenas o player 1 tem autoridade para marcar gols
            if (networkClient != null && networkClient.myId == 1)
            {
                GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
                
                if (isTeam1Goal)
                {
                    // Bola entrou no gol do Time 1 (esquerda), Time 2 (direita) marcou
                    gm.Team2Scored();
                    networkClient.SendGoalScored(2);
                    Debug.Log("Time 2 (Players 2+4) pontuou!");
                }
                else
                {
                    // Bola entrou no gol do Time 2 (direita), Time 1 (esquerda) marcou
                    gm.Team1Scored();
                    networkClient.SendGoalScored(1);
                    Debug.Log("Time 1 (Players 1+3) pontuou!");
                }
                
                // Reset após gol
                Invoke("ResetAfterGoal", 2f);
            }
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
        {
            goalProcessed = false;
        }
    }
}