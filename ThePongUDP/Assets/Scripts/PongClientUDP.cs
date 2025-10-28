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

    private Dictionary<int, float> remotePlayersY = new Dictionary<int, float>();
    private Vector2 remoteBallPos = Vector2.zero;
    private Vector2 remoteBallVel = Vector2.zero;
    private bool updateRemoteBall = false;

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

        UpdateRemotePaddles();

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
            if (updateRemoteBall && ball != null)
            {
                ball.transform.position = Vector3.Lerp(
                    ball.transform.position,
                    remoteBallPos,
                    Time.deltaTime * 15f
                );

                Rigidbody2D ballRig = ball.GetComponent<Rigidbody2D>();
                if (ballRig != null)
                {
                    ballRig.linearVelocity = remoteBallVel;
                    ballRig.WakeUp();
                }
            }
        }

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
                var player = paddle.GetComponent<Player>();
                if (player != null)
                    player.ApplyRemotePosition(remotePlayersY[i]);
            }
        }
    }

    GameObject GetPaddleById(int id)
    {
        return id switch
        {
            1 => player1Paddle,
            2 => player2Paddle,
            3 => player3Paddle,
            4 => player4Paddle,
            _ => null
        };
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
        SendMessage($"GOAL:{scoringTeam};1");
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
        if (client == null) return;
        byte[] data = Encoding.UTF8.GetBytes(message);
        client.Send(data, data.Length);
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

                    GameObject myPaddle = GetPaddleById(myId);
                    if (myPaddle != null)
                    {
                        var pl = myPaddle.GetComponent<Player>();
                        if (pl) pl.isLocalPlayer = true;
                    }
                }
                else if (msg.StartsWith("START"))
                {
                    int parsedTotal = -1;
                    if (msg.Contains(":"))
                    {
                        string[] parts = msg.Split(':');
                        if (parts.Length > 1) int.TryParse(parts[1], out parsedTotal);
                    }

                    totalPlayersConnected = parsedTotal > 0 ? parsedTotal : 2;
                    gameStarted = true;
                    Debug.Log($"[CLIENTE] Jogo iniciado com {totalPlayersConnected} jogadores!");
                }
                else if (msg.StartsWith("PADDLE:"))
                {
                    string[] parts = msg.Substring(7).Split(';');
                    if (parts.Length >= 2 && int.TryParse(parts[0], out int id))
                    {
                        if (id != myId && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                        {
                            remotePlayersY[id] = y;
                        }
                    }
                }
                else if (msg.StartsWith("BALL:") && myId != 1)
                {
                    string[] parts = msg.Substring(5).Split(';');
                    if (parts.Length >= 4)
                    {
                        remoteBallPos = new Vector2(
                            float.Parse(parts[0], CultureInfo.InvariantCulture),
                            float.Parse(parts[1], CultureInfo.InvariantCulture)
                        );
                        remoteBallVel = new Vector2(
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)
                        );
                        updateRemoteBall = true;
                    }
                }
                else if (msg.StartsWith("RESET"))
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() => ResetGame());
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
        ball?.GetComponent<Ball>()?.Reset();
        player1Paddle?.GetComponent<Player>()?.Reset();
        player2Paddle?.GetComponent<Player>()?.Reset();
        player3Paddle?.GetComponent<Player>()?.Reset();
        player4Paddle?.GetComponent<Player>()?.Reset();
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();

        client?.Close();
    }
}
