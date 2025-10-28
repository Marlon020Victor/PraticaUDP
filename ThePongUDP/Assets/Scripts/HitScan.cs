using UnityEngine;

/// <summary>
/// Detecta quando a bola colide com os limites laterais (gol) e,
/// se este cliente for o ID 1 (autoridade), manda o evento ao servidor.
/// NÃO mexe no placar local diretamente; quem atualiza é a mensagem da rede.
/// </summary>
public class HitScan : MonoBehaviour
{
    [Header("Referências")]
    public GameObject Game;
    public GameManager gameManager;        // opcional
    public PongClientUDP networkClient;    // arraste no inspetor

    private void Start()
    {
        if (Game != null && gameManager == null)
            gameManager = Game.GetComponent<GameManager>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Só o "host" (ID 1) decide gol e reinício
        if (networkClient == null || networkClient.myId != 1)
            return;

        // Use CompareTag com as mesmas tags dos seus colliders laterais
        if (collision.gameObject.CompareTag("Map Limit Left"))
        {
            // bola passou pela esquerda -> ponto do Time 2 (direita)
            networkClient.SendGoalScored(2);
            networkClient.SendReset();
        }
        else if (collision.gameObject.CompareTag("Map Limit Right"))
        {
            // bola passou pela direita -> ponto do Time 1 (esquerda)
            networkClient.SendGoalScored(1);
            networkClient.SendReset();
        }
    }
}