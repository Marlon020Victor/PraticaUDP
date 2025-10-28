using UnityEngine;

public class HitScan : MonoBehaviour
{
    public GameObject Game;
    public GameManager gameManager;
    public PongClientUDP networkClient; // atribuído no inspetor

    private void Start()
    {
        gameManager = Game.GetComponent<GameManager>();
        if (networkClient == null)
        {
            networkClient = FindFirstObjectByType<PongClientUDP>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Apenas o player 1 (ou o "dono" da bola) decide quando houve gol
        if (networkClient == null || networkClient.myId != 1)
            return;

        // Detecta gol e notifica o servidor
        if (collision.gameObject.CompareTag("Map Limit Left"))
        {
            // Gol no lado esquerdo = Time 2 (direita) marcou
            networkClient.SendGoalTeam(2);
            Debug.Log("[HitScan] Gol do Time 2!");
        }
        else if (collision.gameObject.CompareTag("Map Limit Right"))
        {
            // Gol no lado direito = Time 1 (esquerda) marcou
            networkClient.SendGoalTeam(1);
            Debug.Log("[HitScan] Gol do Time 1!");
        }
    }
}