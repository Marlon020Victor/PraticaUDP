using UnityEngine;

public class HitScan : MonoBehaviour
{
    public GameObject Game;
    public GameManager gameManager;
    public PongClientUDP networkClient; // agora configurado pelo inspetor

    private void Start()
    {
        gameManager = Game.GetComponent<GameManager>();
        // NÃO usar FindAnyObjectByType() aqui!
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Só o player 1 (dono da bola) decide o gol
        if (networkClient == null || networkClient.myId != 1)
            return;

        if (collision.gameObject.CompareTag("Map Limit Left"))
        {
            gameManager.Player2Scored();
            networkClient.SendGoalScored(2);
            networkClient.SendReset();
        }
        else if (collision.gameObject.CompareTag("Map Limit Right"))
        {
            gameManager.Player1Scored();
            networkClient.SendGoalScored(1);
            networkClient.SendReset();
        }
    }
}