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

    [Header("Estado do Cliente")]
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

    [Header("Net Sync")]
    [Tooltip("Intervalo de envio em segundos")]
    public float sendRate = 0.03f;

    [Header("Debug")]
    public bool debugVerbose = true;
    public bool drawBallAuthorityGizmo = true;

    // Estado remoto
    private readonly Dictionary<int, float> remotePlayersY = new Dictionary<int, float>();
    private Vector2 remoteBallPos = Vector2.zero;
    private Vector2 remoteBallVel = Vector2.zero;
    private bool updateRemoteBall = false;

    // Timers
    private float lastSendTime = 0f;
    private float lastPaddleSendTime = 0f;

    // Métricas e debug
    private float lastBallPacketTime = -1f;
    private float lastPaddlePacketTime = -1f;
    private string lastServerMsg = "";
    private float approxPingMs = -1f;
    private double lastPingSendTime = 0;
    private const string PingToken = "PING";
    private const string PongToken = "PONG";

    void Start()
    {
        _ = UnityMainThreadDispatcher.Instance();

        for (int i = 1; i <= 4; i++)
            remotePlayersY[i] = 0f;

        ConnectToServer();
        InvokeRepeating(nameof(SendPing), 1f, 2f); // ping simples p/ estimar latência
    }

    void ConnectToServer()
    {
        try
        {
            client = new UdpClient();
            serverEP = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
            client.Connect(serverEP);

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            SendMessage("HELLO");
            Log($"Conectado ao servidor {serverIP}:{serverPort}");
        }
        catch (System.Exception e)
        {
            LogError("Erro ao conectar: " + e.Message);
        }
    }

    void Update()
    {
        if (myId == -1) return;

        UpdateRemotePaddles();

        // Autoridade da bola: somente ID 1 simula e envia estado
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
            // Clientes não-autoritativos apenas aplicam estado remoto
            if (updateRemoteBall && ball != null)
            {
                ball.transform.position = Vector3.Lerp(
                    ball.transform.position,
                    remoteBallPos,
                    Time.deltaTime * 15f
                );

                var ballRig = ball.GetComponent<Rigidbody2D>();
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
            if (!paddle) continue;

            if (remotePlayersY.TryGetValue(i, out float ry))
            {
                var player = paddle.GetComponent<Player>();
                if (player != null)
                    player.ApplyRemotePosition(ry);
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
        }
        return null;
    }

    void SendPaddleData()
    {
        GameObject myPaddle = GetPaddleById(myId);
        if (!myPaddle) return;

        float y = myPaddle.transform.position.y;
        string msg = $"PADDLE:{myId};{y.ToString("F3", CultureInfo.InvariantCulture)}";
        SendMessage(msg);
        lastPaddlePacketTime = Time.time;
    }

    void SendBallData()
    {
        if (!ball) return;

        Vector3 pos = ball.transform.position;
        var ballRig = ball.GetComponent<Rigidbody2D>();
        Vector2 vel = ballRig ? ballRig.linearVelocity : Vector2.zero;

        string msg = $"BALL:{pos.x.ToString("F3", CultureInfo.InvariantCulture)};" +
                     $"{pos.y.ToString("F3", CultureInfo.InvariantCulture)};" +
                     $"{vel.x.ToString("F3", CultureInfo.InvariantCulture)};" +
                     $"{vel.y.ToString("F3", CultureInfo.InvariantCulture)}";
        SendMessage(msg);
        lastBallPacketTime = Time.time;
    }

    public void SendGoalScored(int scoringTeam)
    {
        SendMessage($"GOAL:{scoringTeam};1");
        Log($"Enviando gol do Time {scoringTeam}");
    }

    public void SendReset()
    {
        if (myId == 1)
        {
            SendMessage("RESET");
            Log("Enviando comando de reset");
        }
    }

    void SendMessage(string message)
    {
        if (client == null) return;
        byte[] data = Encoding.UTF8.GetBytes(message);
        client.Send(data, data.Length);
        if (debugVerbose) LogNet($"=> {message}");
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
                lastServerMsg = msg;
                if (debugVerbose) LogNet($"<= {msg}");

                if (msg.StartsWith("ASSIGN:"))
                {
                    myId = int.Parse(msg.Substring(7));
                    Log($"Meu ID = {myId}");

                    // setar local player e enviar posição inicial do paddle IMEDIATAMENTE
                    GameObject myPaddle = GetPaddleById(myId);
                    if (myPaddle != null)
                    {
                        var pl = myPaddle.GetComponent<Player>();
                        if (pl) pl.isLocalPlayer = true;
                    }
                    // evita “paddle invisível” até o jogador se mexer
                    UnityMainThreadDispatcher.Instance().Enqueue(SendPaddleData);
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
                    Log($"Jogo iniciado com {totalPlayersConnected} jogadores!");

                    // dispara a bola de forma determinística ASSIM QUE o START chegar
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        if (ball != null)
                        {
                            var b = ball.GetComponent<Ball>();
                            if (b != null)
                            {
                                if (myId == 1)
                                    b.StartAsAuthoritative(); // só ID 1 lança
                                else
                                    b.MarkAsNonAuthoritativeClient(); // outros só seguem
                            }
                        }
                    });
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
                else if (msg.StartsWith(PingToken))
                {
                    // ecoa PONG (só como exemplo; seu servidor já pode estar ignorando isso)
                    SendMessage(PongToken);
                }
                else if (msg.StartsWith(PongToken))
                {
                    // se seu servidor algum dia refletir o ping, calcula latência aqui
                    approxPingMs = (float)((Time.realtimeSinceStartupAsDouble - lastPingSendTime) * 1000.0);
                }
            }
            catch (System.Exception e)
            {
                LogError("Erro ao receber: " + e.Message);
                break;
            }
        }
    }

    void ResetGame()
    {
        Log("Resetando jogo...");
        ball?.GetComponent<Ball>()?.Reset();
        player1Paddle?.GetComponent<Player>()?.Reset();
        player2Paddle?.GetComponent<Player>()?.Reset();
        player3Paddle?.GetComponent<Player>()?.Reset();
        player4Paddle?.GetComponent<Player>()?.Reset();

        // em clientes não-autoritativos, certifique-se de que continuaremos seguindo estado da bola
        if (myId != 1) updateRemoteBall = true;
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();
        client?.Close();
    }

    // ===== Debug helpers =====
    void SendPing()
    {
        lastPingSendTime = Time.realtimeSinceStartupAsDouble;
        SendMessage(PingToken);
    }

    void Log(string msg) => Debug.Log($"[CLIENTE][ID:{myId}] {msg}");
    void LogError(string msg) => Debug.LogError($"[CLIENTE][ID:{myId}] {msg}");
    void LogNet(string msg) { if (debugVerbose) Debug.Log($"[NET][ID:{myId}] {msg}"); }

    void OnGUI()
    {
        if (!debugVerbose) return;

        GUILayout.BeginArea(new Rect(10, 10, 420, 180), GUI.skin.box);
        GUILayout.Label($"ID: {myId} | Started: {gameStarted} | Total: {totalPlayersConnected}");
        GUILayout.Label($"Última msg do server: {lastServerMsg}");
        GUILayout.Label($"Último envio BALL: {(lastBallPacketTime < 0 ? "-" : (Time.time - lastBallPacketTime).ToString("F2")+"s")} | " +
                        $"Último envio PADDLE: {(lastPaddlePacketTime < 0 ? "-" : (Time.time - lastPaddlePacketTime).ToString("F2")+"s")}");
        GUILayout.Label($"Ping ~ {(approxPingMs < 0 ? "?" : approxPingMs.ToString("F0"))} ms (estimativa)");
        GUILayout.Label(myId == 1 ? "Autoridade da Bola: ESTE CLIENTE" : "Autoridade da Bola: CLIENTE 1");
        if (GUILayout.Button("Forçar RESET (apenas ID1)")) SendReset();
        GUILayout.EndArea();
    }

    void OnDrawGizmos()
    {
        if (!drawBallAuthorityGizmo || !ball) return;
        Gizmos.color = (myId == 1) ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(ball.transform.position, 0.4f);
    }
}
