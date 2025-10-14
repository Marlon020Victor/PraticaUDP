using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] 
    private Rigidbody2D Rig;
    public float MoveSpeed = 10f;
    public Vector3 startPosition;
    
    [Header("Multiplayer")]
    public bool isLocalPlayer = true;
    public int playerNumber = 1; // 1, 2, 3 ou 4
    
    [Header("AI Settings (quando não controlado)")]
    public GameObject ball;
    public float aiSpeed = 8f;
    public float aiReactionDelay = 0.2f;
    public float aiErrorMargin = 0.5f;
    
    private PongClientUDP networkClient;
    private bool isAIControlled = false;
    private float lastAIReactionTime = 0f;
    private Vector3 aiTargetPosition;
    
    private void Start()
    {
        startPosition = transform.position;
        networkClient = FindFirstObjectByType<PongClientUDP>();
        aiTargetPosition = transform.position;
        
        // Tenta encontrar a bola automaticamente se não foi atribuída
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("Ball");
        }
    }
    
    void Update()
    {
        // Verifica se este paddle é controlado localmente, remotamente ou por IA
        CheckControl();
        
        if (isLocalPlayer)
        {
            PlayMovement();
        }
        else if (isAIControlled)
        {
            AIMovement();
        }
    }
    
    void CheckControl()
    {
        if (networkClient == null || networkClient.myId == -1)
        {
            // Sem rede ou sem ID = não faz nada ainda
            isLocalPlayer = false;
            isAIControlled = false;
            return;
        }
    
        // Só ativa IA se o jogo já começou (gameStarted) e tem pelo menos 2 jogadores
        if (!networkClient.gameStarted || networkClient.totalPlayersConnected < 2)
        {
            isLocalPlayer = false;
            isAIControlled = false;
            return;
        }
        
        // Se este é meu paddle, eu controlo
        if (networkClient.myId == playerNumber)
        {
            isLocalPlayer = true;
            isAIControlled = false;
        }
        else
        {
            // Se não é meu paddle, verificar se tem jogador conectado nele
            // Players 1-4: se o ID máximo conectado é menor que meu número, uso IA
            isLocalPlayer = false;
            
            // IA ativa apenas se ninguém mais controla este paddle
            // (a rede já controla paddles de outros jogadores conectados)
            isAIControlled = !IsPlayerConnected(playerNumber);
        }
    }
    
    bool IsPlayerConnected(int playerId)
    {
        if (networkClient == null) return false;
        
        // Se o total de jogadores conectados é menor que este ID, não tem jogador
        // Exemplo: 2 jogadores conectados = IDs 1 e 2, então 3 e 4 usam IA
        return playerId <= networkClient.totalPlayersConnected;
    }
    
    void AIMovement()
    {
        if (ball == null) return;
        
        // Atualiza posição alvo com delay de reação
        if (Time.time - lastAIReactionTime > aiReactionDelay)
        {
            lastAIReactionTime = Time.time;
            
            // Calcula posição alvo com margem de erro para parecer mais humano
            float targetY = ball.transform.position.y;
            targetY += UnityEngine.Random.Range(-aiErrorMargin, aiErrorMargin);
            
            // Limita a posição Y baseado nos limites da tela
            targetY = Mathf.Clamp(targetY, -4f, 4f);
            
            aiTargetPosition = transform.position;
            aiTargetPosition.y = targetY;
        }
        
        // Move suavemente em direção ao alvo
        transform.position = Vector3.MoveTowards(
            transform.position,
            aiTargetPosition,
            aiSpeed * Time.deltaTime
        );
    }
    
    private void PlayMovement()
    {
        bool isPressingUp = false;
        bool isPressingDown = false;
        
        // Controles para cada jogador
        switch (playerNumber)
        {
            case 1: // Player 1 - W/S (lado esquerdo superior)
                isPressingUp = Input.GetKey(KeyCode.W);
                isPressingDown = Input.GetKey(KeyCode.S);
                break;
                
            case 2: // Player 2 - Setas (lado direito superior)
                isPressingUp = Input.GetKey(KeyCode.UpArrow);
                isPressingDown = Input.GetKey(KeyCode.DownArrow);
                break;
                
            case 3: // Player 3 - T/G (lado esquerdo inferior)
                isPressingUp = Input.GetKey(KeyCode.T);
                isPressingDown = Input.GetKey(KeyCode.G);
                break;
                
            case 4: // Player 4 - I/K (lado direito inferior)
                isPressingUp = Input.GetKey(KeyCode.I);
                isPressingDown = Input.GetKey(KeyCode.K);
                break;
        }
        
        if (isPressingUp)
        {
            transform.Translate(Vector2.up * MoveSpeed * Time.deltaTime);
        }
        if (isPressingDown)
        {
            transform.Translate(Vector2.down * MoveSpeed * Time.deltaTime);
        }
    }
    
    public void Reset()
    {
        if (Rig != null)
        {
            Rig.linearVelocity = Vector2.zero;
        }
        transform.position = startPosition;
    }
}