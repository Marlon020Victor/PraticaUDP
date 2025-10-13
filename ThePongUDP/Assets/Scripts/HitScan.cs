using UnityEngine;

public class HitScan : MonoBehaviour
{
    public GameObject Game;
    public GameManager gameManager;
    public PongClientUDP networkClient; // atribuído no inspetor

    private void Start()
    {
        gameManager = Game.GetComponent<GameManager>();
        // Evita qualquer Find() pra não dar NullRef em tempo de rede
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Apenas o player 1 (ou o "dono" da bola) decide quando houve gol
        if (networkClient == null || networkClient.myId != 1)
            return;

        // Detecta gol e notifica o servidor, mas NÃO atualiza o placar local
        if (collision.gameObject.CompareTag("Map Limit Left"))
        {
            networkClient.SendGoalScored(2); // Player 2 marcou
            networkClient.SendReset();       // Reinicia bola
        }
        else if (collision.gameObject.CompareTag("Map Limit Right"))
        {
            networkClient.SendGoalScored(1); // Player 1 marcou
            networkClient.SendReset();       // Reinicia bola
        }
    }
}