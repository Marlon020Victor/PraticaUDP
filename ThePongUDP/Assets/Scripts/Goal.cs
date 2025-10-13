using UnityEngine;

public class Goal : MonoBehaviour
{
    public bool isPlayer1Goal;
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
                
                if (!isPlayer1Goal)
                {
                    // Bola entrou no gol do player 1, player 2 marcou
                    gm.Player2Scored();
                    networkClient.SendGoalScored(2);
                    Debug.Log("Player 2 pontuou!");
                }
                else
                {
                    // Bola entrou no gol do player 2, player 1 marcou
                    gm.Player1Scored();
                    networkClient.SendGoalScored(1);
                    Debug.Log("Player 1 pontuou!");
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