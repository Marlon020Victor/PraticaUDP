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
    public bool gameStarted = false;
    public int totalPlayersConnected = 0;

    [Header("Configurações do Servidor")]
    public string serverIP = "26.203.179.47";
    public int serverPort = 5001;

    [Header("Referências do Jogo")]
    public GameObject player1Paddle;
    public GameObject player2Paddle;
    public GameObject player3Paddle;
    public GameObject player4Paddle;
    public GameObject ball;
    public GameManager gameManager;

    // Dados recebidos da rede
    private Dictionary<int, float> remotePlayersY = new Dictionary<int, float>();
    private Vector2 remoteBallPos = Vector2.zero;
    private Vector2 remoteBallVel = Vector2.zero;
    private bool updateRemoteBall = false;

    // Controle de envio
    private float sendRate = 0.03f;
    private float lastSendTime = 0f;
    private float lastPaddleSendTime = 0f;

    void Start()
    {
        _ = UnityMainThreadDispatcher.Instance();

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
        if (myId == -1) return;

        // Atualiza paddles remotos
        UpdateRemotePaddles();

        // Player 1 tem autoridade sobre a bola
        if (myId == 1 && gameStarted)
        {
            if (Time.time - lastSendTime > sendRate)
            {
                SendBallData();
                lastSendTime = Time.time;
            }
        }
        else
        {
            // Outros players recebem posição da bola
            if (updateRemoteBall && ball != null)
            {
                ball.transform.position = Vector3.Lerp(
                    ball.transform.position,
                    remoteBallPos,
                    Time.deltaTime * 20f
                );

                Rigidbody2D ballRig = ball.GetComponent<Rigidbody2D>();
                if (ballRig != null)
                {
                    ballRig.linearVelocity = remoteBallVel;
                }
            }
        }

        // Envia posição do próprio paddle
        if (Time.time - lastPaddleSendTime > sendRate)
        {
            SendPaddleData();
            lastPaddleSendTime = Time.time;
        }
    }

    void UpdateRemotePaddles()
    {
        for (int i = 1; i <= 4; i++)
        {
            if (i == myId) continue;

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
            string msg = $"PADDLE:{myId};{y.ToString("F3", CultureInfo.InvariantCulture)}";
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
        string msg = $"GOAL:{scoringTeam};1";
        SendMessage(msg);
        Debug.Log($"[CLIENTE] Enviando gol do Time {scoringTeam}");
    }

    public void SendReset()
    {
        if (myId == 1)
        {
            SendMessage("RESET");
            Debug.Log("[CLIENTE] Enviando comando de reset");
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

                if (msg.StartsWith("ASSIGN:"))
                {
                    myId = int.Parse(msg.Substring(7));
                    Debug.Log($"[CLIENTE] Meu ID = {myId}");

                    string team = (myId == 1 || myId == 3) ? "ESQUERDO (1+3)" : "DIREITO (2+4)";
                    Debug.Log($"[CLIENTE] Você está no TIME {team}");
                }
                else if (msg.StartsWith("REJECT:"))
                {
                    Debug.LogWarning("[CLIENTE] " + msg.Substring(7));
                }
                else if (msg.StartsWith("START"))
                {
                    int parsedTotal = -1;
                    if (msg.Contains(":"))
                    {
                        string[] parts = msg.Split(':');
                        if (parts.Length > 1)
                        {
                            int.TryParse(parts[1], out parsedTotal);
                        }
                    }

                    if (parsedTotal >= 2)
                    {
                        totalPlayersConnected = parsedTotal;
                        gameStarted = true;
                        Debug.Log($"[CLIENTE] Jogo iniciado com {totalPlayersConnected} jogadores!");
                    }
                    else
                    {
                        totalPlayersConnected = parsedTotal > 0 ? parsedTotal : 2;
                        gameStarted = true;
                        Debug.Log($"[CLIENTE] Jogo iniciado!");
                    }
                }
                else if (msg.StartsWith("PADDLE:"))
                {
                    string payload = msg.Substring(7);
                    string[] parts = payload.Split(';');
                    if (parts.Length >= 2)
                    {
                        int id;
                        float y;
                        if (int.TryParse(parts[0], out id) && 
                            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                        {
                            if (id != myId)
                            {
                                remotePlayersY[id] = y;
                            }
                        }
                    }
                }
                else if (msg.StartsWith("BALL:"))
                {
                    if (myId != 1)
                    {
                        string[] parts = msg.Substring(5).Split(';');
                        if (parts.Length >= 4)
                        {
                            float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                            float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            float vx = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            float vy = float.Parse(parts[3], CultureInfo.InvariantCulture);

                            remoteBallPos.x = x;
                            remoteBallPos.y = y;
                            remoteBallVel.x = vx;
                            remoteBallVel.y = vy;
                            updateRemoteBall = true;
                        }
                    }
                }
                else if (msg.StartsWith("GOAL:"))
                {
                    string[] parts = msg.Substring(5).Split(';');
                    if (parts.Length >= 2)
                    {
                        int scoringTeam = int.Parse(parts[0]);
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            if (gameManager != null)
                            {
                                if (scoringTeam == 1)
                                {
                                    gameManager.Team1Scored();
                                }
                                else if (scoringTeam == 2)
                                {
                                    gameManager.Team2Scored();
                                }
                            }
                        });
                    }
                }
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
        Debug.Log("[CLIENTE] Resetando jogo...");
        
        if (ball != null)
        {
            ball.GetComponent<Ball>()?.Reset();
        }
        if (player1Paddle != null)
        {
            player1Paddle.GetComponent<Player>()?.Reset();
        }
        if (player2Paddle != null)
        {
            player2Paddle.GetComponent<Player>()?.Reset();
        }
        if (player3Paddle != null)
        {
            player3Paddle.GetComponent<Player>()?.Reset();
        }
        if (player4Paddle != null)
        {
            player4Paddle.GetComponent<Player>()?.Reset();
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