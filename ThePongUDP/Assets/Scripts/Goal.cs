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
        else
        {
            Debug.Log($"[GOAL] Inicializado. isLeftGoal={isLeftGoal}");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se é a bola
        if (!collision.gameObject.CompareTag("Ball"))
        {
            Debug.Log($"[GOAL] Trigger com objeto não-bola: {collision.gameObject.name}");
            return;
        }

        // Evita processar o mesmo gol múltiplas vezes
        if (goalProcessed)
        {
            Debug.Log("[GOAL] Gol já processado, ignorando...");
            return;
        }

        // APENAS o Player 1 processa gols (autoridade)
        if (networkClient == null || networkClient.myId != 1)
        {
            Debug.Log($"[GOAL] Não sou o Player 1 (myId={networkClient?.myId}), ignorando gol");
            return;
        }

        goalProcessed = true;

        // Determina qual time marcou
        // Se bola entrou no gol da ESQUERDA → Time DIREITA (2) marcou
        // Se bola entrou no gol da DIREITA → Time ESQUERDA (1) marcou
        int scoringTeam = isLeftGoal ? 2 : 1;
        
        Debug.Log($"[GOAL] *** GOL! Time {scoringTeam} marcou! (isLeftGoal={isLeftGoal}) ***");

        // Atualiza placar localmente
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            if (scoringTeam == 1)
            {
                gm.Team1Scored();
                Debug.Log("[GOAL] Team 1 scored localmente");
            }
            else
            {
                gm.Team2Scored();
                Debug.Log("[GOAL] Team 2 scored localmente");
            }
        }

        // Envia para outros clientes
        networkClient.SendGoalTeam(scoringTeam);

        // Reseta após 2 segundos
        Invoke(nameof(ResetAfterGoal), 2f);
    }

    void ResetAfterGoal()
    {
        if (networkClient != null && networkClient.myId == 1)
        {
            Debug.Log("[GOAL] Enviando RESET após gol");
            networkClient.SendReset();
        }
        
        goalProcessed = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            // Permite novo gol quando a bola sair completamente
            goalProcessed = false;
            Debug.Log("[GOAL] Bola saiu do trigger, pronto para novo gol");
        }
    }
}