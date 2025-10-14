using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

public class PongClientUDP : MonoBehaviour
{
    UdpClient client;
    Thread receiveThread;
    IPEndPoint serverEP;
    
    public int myId = -1;
    private bool gameStarted = false;
    
    [Header("Configurações do Servidor")]
    public string serverIP = "26.203.179.47";
    public int serverPort = 5001;
    
    [Header("Referências do Jogo")]
    public GameObject player1Paddle;
    public GameObject player2Paddle;
    public GameObject player3Paddle; // NOVO
    public GameObject player4Paddle; // NOVO
    public GameObject ball;
    public GameManager gameManager;
    
    // Dados recebidos da rede - agora para 4 jogadores
    private Dictionary<int, float> remotePlayersY = new Dictionary<int, float>();
    private Vector2 remoteBallPos = Vector2.zero;
    private Vector2 remoteBallVel = Vector2.zero;
    private bool updateRemoteBall = false;
    
    // Controle de envio
    private float sendRate = 0.03f; // 30 vezes por segundo
    private float lastSendTime = 0f;

    void Start()
    {
        // Garante que o dispatcher existe na main thread
        _ = UnityMainThreadDispatcher.Instance();
        
        // Inicializa posições remotas
        for (int i = 1; i <= 4; i++)
        {
            remotePlayersY[i] = 0f;
        }
        
        ConnectToServer();
    }
    
    void ConnectToServer()
    {
        try
        {
            client = new UdpClient();
            serverEP = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            client.Connect(serverEP);
            
            receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
            
            // Envia HELLO para se registrar
            SendMessage("HELLO");
            Debug.Log("[CLIENTE] Conectado ao servidor " + serverIP + ":" + serverPort);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[CLIENTE] Erro ao conectar: " + e.Message);
        }
    }

    void Update()
    {
        if (myId == -1 || !gameStarted) return;
        
        // Atualiza posição dos paddles remotos (todos exceto o meu)
        UpdateRemotePaddles();
        
        // Player 1 tem autoridade sobre a bola
        if (myId == 1)
        {
            // Envia posição da bola
            if (Time.time - lastSendTime > sendRate)
            {
                SendBallData();
                lastSendTime = Time.time;
            }
        }
        else
        {
            // Players 2, 3 e 4 recebem e aplicam posição da bola
            if (updateRemoteBall && ball != null)
            {
                ball.transform.position = Vector3.Lerp(
                    ball.transform.position,
                    remoteBallPos,
                    Time.deltaTime * 10f
                );
                
                Rigidbody2D ballRig = ball.GetComponent<Rigidbody2D>();
                if (ballRig != null)
                {
                    ballRig.linearVelocity = remoteBallVel;
                }
            }
        }
        
        // Envia posição do próprio paddle
        SendPaddleData();
    }
    
    void UpdateRemotePaddles()
    {
        // Atualiza cada paddle que não é o meu
        for (int i = 1; i <= 4; i++)
        {
            if (i == myId) continue; // Pula o meu próprio paddle
            
            GameObject paddle = GetPaddleById(i);
            if (paddle != null && remotePlayersY.ContainsKey(i))
            {
                Vector3 targetPos = paddle.transform.position;
                targetPos.y = remotePlayersY[i];
                paddle.transform.position = Vector3.Lerp(
                    paddle.transform.position,
                    targetPos,
                    Time.deltaTime * 15f
                );
            }
        }
    }
    
    GameObject GetPaddleById(int id)
    {
        switch (id)
        {
            case 1: return player1Paddle;
            case 2: return player2Paddle;
            case 3: return player3Paddle;
            case 4: return player4Paddle;
            default: return null;
        }
    }
    
    void SendPaddleData()
    {
        GameObject myPaddle = GetPaddleById(myId);
        if (myPaddle != null)
        {
            float y = myPaddle.transform.position.y;
            string msg = $"PADDLE:{y.ToString("F3", CultureInfo.InvariantCulture)}";
            SendMessage(msg);
        }
    }
    
    void SendBallData()
    {
        if (ball != null)
        {
            Vector3 pos = ball.transform.position;
            Rigidbody2D ballRig = ball.GetComponent<Rigidbody2D>();
            Vector2 vel = ballRig != null ? ballRig.linearVelocity : Vector2.zero;
            
            string msg = $"BALL:{pos.x.ToString("F3", CultureInfo.InvariantCulture)};" +
                        $"{pos.y.ToString("F3", CultureInfo.InvariantCulture)};" +
                        $"{vel.x.ToString("F3", CultureInfo.InvariantCulture)};" +
                        $"{vel.y.ToString("F3", CultureInfo.InvariantCulture)}";
            SendMessage(msg);
        }
    }
    
    public void SendGoalScored(int scoringTeam)
    {
        // scoringTeam: 1 para Time Esquerdo (Players 1+3), 2 para Time Direito (Players 2+4)
        string msg = $"GOAL:{scoringTeam};1";
        SendMessage(msg);
    }
    
    public void SendReset()
    {
        if (myId == 1)
        {
            SendMessage("RESET");
        }
    }
    
    void SendMessage(string message)
    {
        if (client != null)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length);
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        
        while (true)
        {
            try
            {
                byte[] data = client.Receive(ref remoteEP);
                string msg = Encoding.UTF8.GetString(data);
                
                // ID atribuído pelo servidor
                if (msg.StartsWith("ASSIGN:"))
                {
                    myId = int.Parse(msg.Substring(7));
                    Debug.Log($"[CLIENTE] Meu ID = {myId}");
                    
                    // Mostra qual time o jogador pertence
                    string team = (myId == 1 || myId == 3) ? "ESQUERDO" : "DIREITO";
                    Debug.Log($"[CLIENTE] Você está no TIME {team}");
                }
                // Servidor rejeitou conexão
                else if (msg.StartsWith("REJECT:"))
                {
                    Debug.LogWarning("[CLIENTE] " + msg.Substring(7));
                }
                // Jogo começou
                else if (msg.StartsWith("START"))
                {
                    gameStarted = true;
                    Debug.Log("[CLIENTE] Jogo iniciado com 4 jogadores!");
                }
                // Posição do paddle de outro jogador
                else if (msg.StartsWith("PADDLE:"))
                {
                    string[] parts = msg.Substring(7).Split(';');
                    if (parts.Length >= 2)
                    {
                        int id = int.Parse(parts[0]);
                        if (id != myId)
                        {
                            float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            remotePlayersY[id] = y;
                        }
                    }
                }
                // Posição da bola
                else if (msg.StartsWith("BALL:"))
                {
                    if (myId != 1) // Apenas players 2, 3 e 4 recebem
                    {
                        string[] parts = msg.Substring(5).Split(';');
                        if (parts.Length >= 4)
                        {
                            remoteBallPos.x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                            remoteBallPos.y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            remoteBallVel.x = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            remoteBallVel.y = float.Parse(parts[3], CultureInfo.InvariantCulture);
                            updateRemoteBall = true;
                        }
                    }
                }
                // Gol marcado
                else if (msg.StartsWith("GOAL:"))
                {
                    string[] parts = msg.Substring(5).Split(';');
                    if (parts.Length >= 2)
                    {
                        int scoringTeam = int.Parse(parts[0]);
                        // Processa no main thread via flag
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            if (gameManager != null)
                            {
                                if (scoringTeam == 1)
                                    gameManager.Team1Scored(); // Time Esquerdo (Players 1+3)
                                else
                                    gameManager.Team2Scored(); // Time Direito (Players 2+4)
                            }
                        });
                    }
                }
                // Reset do jogo
                else if (msg.StartsWith("RESET"))
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        ResetGame();
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CLIENTE] Erro ao receber: " + e.Message);
                break;
            }
        }
    }
    
    void ResetGame()
    {
        if (ball != null)
        {
            ball.GetComponent<Ball>().Reset();
        }
        if (player1Paddle != null)
        {
            player1Paddle.GetComponent<Player>().Reset();
        }
        if (player2Paddle != null)
        {
            player2Paddle.GetComponent<Player>().Reset();
        }
        if (player3Paddle != null)
        {
            player3Paddle.GetComponent<Player>().Reset();
        }
        if (player4Paddle != null)
        {
            player4Paddle.GetComponent<Player>().Reset();
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        
        if (client != null)
        {
            client.Close();
        }
    }
}