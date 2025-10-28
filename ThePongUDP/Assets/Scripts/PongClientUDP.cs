using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;

public class PongClientUDP : MonoBehaviour
{
    UdpClient client;
    Thread receiveThread;
    IPEndPoint serverEP;

    public int myId = -1;
    private bool gameStarted = false;

    [Header("Servidor")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5001;

    [Header("Referências")]
    public GameObject paddle1; // Time 1 (esq)
    public GameObject paddle2; // Time 2 (dir)
    public GameObject paddle3; // Time 1 (esq)
    public GameObject paddle4; // Time 2 (dir)
    public GameObject ball;
    public GameManager gameManager;

    // estados remotos
    private float[] remoteY = new float[5]; // 1..4
    private bool[] hasRemote = new bool[5];

    private Vector2 remoteBallPos = Vector2.zero;
    private Vector2 remoteBallVel = Vector2.zero;
    private bool updateRemoteBall = false;

    private float sendRate = 0.03f;
    private float lastSendTime = 0f;

    void Start()
    {
        _ = UnityMainThreadDispatcher.Instance();
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
        if (myId == -1 || !gameStarted) return;

        ApplyRemotePaddle(1, paddle1);
        ApplyRemotePaddle(2, paddle2);
        ApplyRemotePaddle(3, paddle3);
        ApplyRemotePaddle(4, paddle4);

        if (myId == 1)
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
                ball.transform.position = Vector3.Lerp(ball.transform.position, remoteBallPos, Time.deltaTime * 10f);
                var rig = ball.GetComponent<Rigidbody2D>();
                if (rig != null) rig.linearVelocity = remoteBallVel;
            }
        }

        SendPaddleData(); // envia sempre sua posição
    }

    void ApplyRemotePaddle(int id, GameObject paddleGO)
    {
        if (paddleGO == null) return;
        if (myId == id) return; // eu controlo o meu
        if (hasRemote[id])
        {
            Vector3 t = paddleGO.transform.position;
            t.y = remoteY[id];
            paddleGO.transform.position = Vector3.Lerp(paddleGO.transform.position, t, Time.deltaTime * 15f);
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

    GameObject GetPaddleById(int id)
    {
        switch (id)
        {
            case 1: return paddle1;
            case 2: return paddle2;
            case 3: return paddle3;
            case 4: return paddle4;
        }
        return null;
    }

    void SendBallData()
    {
        if (!ball) return;
        Vector3 pos = ball.transform.position;
        var rig = ball.GetComponent<Rigidbody2D>();
        Vector2 vel = rig ? rig.linearVelocity : Vector2.zero;

        string msg = $"BALL:{pos.x.ToString("F3", CultureInfo.InvariantCulture)};{pos.y.ToString("F3", CultureInfo.InvariantCulture)};{vel.x.ToString("F3", CultureInfo.InvariantCulture)};{vel.y.ToString("F3", CultureInfo.InvariantCulture)}";
        SendMessage(msg);
    }

    public void SendGoalTeam(int team) // 1 = esquerda (P1+P3), 2 = direita (P2+P4)
    {
        string msg = $"GOAL:{team};1";
        SendMessage(msg);
    }

    public void SendReset()
    {
        if (myId == 1) SendMessage("RESET");
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
                }
                else if (msg.StartsWith("REJECT:"))
                {
                    Debug.LogWarning("[CLIENTE] " + msg.Substring(7));
                }
                else if (msg.StartsWith("START"))
                {
                    gameStarted = true;
                    Debug.Log("[CLIENTE] Jogo iniciado!");

                    // Se eu sou o 1, eu libero a bola após o START
                    if (myId == 1 && ball != null)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            var b = ball.GetComponent<Ball>();
                            if (b != null) b.StartRoundAfter(1f);
                        });
                    }
                }
                else if (msg.StartsWith("PADDLE:"))
                {
                    string[] parts = msg.Substring(7).Split(';');
                    if (parts.Length >= 2)
                    {
                        int id = int.Parse(parts[0]);
                        float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        if (id >= 1 && id <= 4 && id != myId)
                        {
                            remoteY[id] = y;
                            hasRemote[id] = true;
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
                            remoteBallPos.x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                            remoteBallPos.y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            remoteBallVel.x = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            remoteBallVel.y = float.Parse(parts[3], CultureInfo.InvariantCulture);
                            updateRemoteBall = true;
                        }
                    }
                }
                else if (msg.StartsWith("GOAL:"))
                {
                    string[] parts = msg.Substring(5).Split(';');
                    if (parts.Length >= 2)
                    {
                        int team = int.Parse(parts[0]); // 1=esq, 2=dir
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            if (gameManager != null)
                            {
                                if (team == 1) gameManager.Team1Scored();
                                else gameManager.Team2Scored();
                            }
                        });
                    }
                }
                else if (msg.StartsWith("RESET"))
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() => { ResetGame(); });
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
        if (ball != null) ball.GetComponent<Ball>().Reset();
        if (paddle1 != null) paddle1.GetComponent<Player>().Reset();
        if (paddle2 != null) paddle2.GetComponent<Player>().Reset();
        if (paddle3 != null) paddle3.GetComponent<Player>().Reset();
        if (paddle4 != null) paddle4.GetComponent<Player>().Reset();
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
        client?.Close();
    }
}
