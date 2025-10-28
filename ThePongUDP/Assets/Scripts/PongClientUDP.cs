using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

/// <summary>
/// Cliente UDP do Pong:
/// - Conecta no servidor e recebe um ID (1..4)
/// - Envia posição do paddle local (~30 Hz)
/// - Recebe posições dos paddles remotos e estado da bola
/// - Responde a START/RESET/SCORE/GOAL
/// </summary>
public class PongClientUDP : MonoBehaviour
{
    // --- Networking ---
    private UdpClient client;
    private Thread receiveThread;
    private IPEndPoint serverEP;

    // --- Estado de jogo ---
    [Header("Estado / Identificação")]
    public int myId = -1;                   // ID dado pelo servidor (1..4)
    public bool gameStarted = false;        // vira true após START
    public int totalPlayersConnected = 0;   // contagem do START (fallback = 2)

    [Header("Config")]
    public string serverIp = "127.0.0.1";
    public int serverPort = 8051;
    public int localPort = 0;               // 0 = porta efêmera

    [Header("Referências de Cena (arraste no Inspetor)")]
    public GameObject player1Paddle;
    public GameObject player2Paddle;
    public GameObject player3Paddle;
    public GameObject player4Paddle;
    public GameObject ball;
    public GameManager gameManager;

    // --- Buffers ---
    private readonly Dictionary<int, float> remotePlayersY = new Dictionary<int, float>();

    private Vector2 remoteBallPos = Vector2.zero;
    private Vector2 remoteBallVel = Vector2.zero;
    private bool updateBallFromNetwork = false;

    private float sendTimer = 0f;
    private const float sendInterval = 0.033f; // ~30 Hz

    private void Awake()
    {
        Application.runInBackground = true;

        // Avisos pra evitar “não vejo o outro player” por referência vazia
        if (!player1Paddle || !player2Paddle || !player3Paddle || !player4Paddle)
            Debug.LogWarning("[CLIENTE] Sete os 4 paddles no PongClientUDP em TODAS as máquinas.");
        if (!ball) Debug.LogWarning("[CLIENTE] Arraste a bola no PongClientUDP.");
        if (!gameManager) Debug.LogWarning("[CLIENTE] Arraste o GameManager no PongClientUDP.");
    }

    private void Start()
    {
        try
        {
            serverEP = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            client = new UdpClient(localPort);

            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();

            Send("HELLO");
            Debug.Log("[CLIENTE] UDP iniciado. Aguardando ID...");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CLIENTE] Falha ao iniciar cliente UDP: " + ex.Message);
        }
    }

    private void OnDestroy()
    {
        try { receiveThread?.Abort(); } catch { }
        try { client?.Close(); } catch { }
    }

    private void Update()
    {
        // envia paddle local periodicamente
        sendTimer += Time.deltaTime;
        if (sendTimer >= sendInterval)
        {
            sendTimer = 0f;
            SendPaddleData();
        }

        // interpola paddles remotos
        UpdateRemotePaddles();

        // (opcional) aplicar estado de bola vindo da rede
        if (updateBallFromNetwork && ball != null)
        {
            ball.transform.position = Vector3.Lerp(ball.transform.position, remoteBallPos, 0.5f);
            var rb = ball.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.velocity = Vector2.Lerp(rb.velocity, remoteBallVel, 0.5f);
        }
    }

    // =========================
    // ========= ENVIO =========
    // =========================

    private void SendPaddleData()
    {
        if (myId <= 0) return;
        var myPaddle = GetPaddleById(myId);
        if (myPaddle == null) return;

        float y = myPaddle.transform.position.y;
        string msg = $"PADDLE:{myId};{y.ToString(CultureInfo.InvariantCulture)}";
        Send(msg);
        // Debug.Log($"[CLIENTE] Enviado {msg}");
    }

    private void Send(string text)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            client.Send(data, data.Length, serverEP);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CLIENTE] Erro ao enviar: " + ex.Message);
        }
    }

    /// <summary>
    /// Chame isso quando DETECTAR gol no cliente 'dono' (id==1).
    /// Envia para o servidor notificar a todos.
    /// Mando GOAL e também SCORE por compatibilidade (caso seu Server escute um dos dois).
    /// </summary>
    public void SendGoalScored(int team)
    {
        if (myId != 1) return; // só o "host" decide
        if (team != 1 && team != 2) return;

        Send($"GOAL:{team}");
        Send($"SCORE:{team}"); // compat: se o servidor só entender SCORE
        Debug.Log($"[CLIENTE] GOAL enviado (team {team}).");
    }

    /// <summary>
    /// Solicita reset de rodada ao servidor (após gol).
    /// </summary>
    public void SendReset()
    {
        if (myId != 1) return; // só o "host"
        Send("RESET");
        Debug.Log("[CLIENTE] RESET enviado.");
    }

    // =========================
    // ======== RECEPÇÃO =======
    // =========================

    private void ReceiveLoop()
    {
        IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                byte[] data = client.Receive(ref any);
                string msg = Encoding.UTF8.GetString(data);
                UnityMainThreadDispatcher.Enqueue(() => HandleMessage(msg));
            }
            catch (SocketException) { }
            catch (ThreadAbortException) { break; }
            catch (System.Exception ex)
            {
                Debug.LogError("[CLIENTE] ReceiveLoop erro: " + ex.Message);
            }
        }
    }

    private void HandleMessage(string msg)
    {
        if (msg.StartsWith("ID:"))
        {
            if (int.TryParse(msg.Substring(3), out int id))
            {
                myId = id;
                Debug.Log($"[CLIENTE] Meu ID é {myId}");
            }
            return;
        }

        if (msg.StartsWith("START"))
        {
            // START ou START:N
            totalPlayersConnected = 2; // fallback
            var parts = msg.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int count) && count >= 2)
                totalPlayersConnected = count;

            gameStarted = true;
            Debug.Log($"[CLIENTE] Jogo iniciado (players={totalPlayersConnected}).");
            return;
        }

        if (msg.StartsWith("PADDLE:"))
        {
            var body = msg.Substring(7);
            var p = body.Split(';');
            if (p.Length == 2 &&
                int.TryParse(p[0], out int id) &&
                float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                remotePlayersY[id] = y;
            }
            return;
        }

        if (msg.StartsWith("BALLPOS:"))
        {
            var body = msg.Substring(8);
            var p = body.Split(';');
            if (p.Length == 2 &&
                float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                remoteBallPos = new Vector2(x, y);
                updateBallFromNetwork = true;
            }
            return;
        }

        if (msg.StartsWith("BALLVEL:"))
        {
            var body = msg.Substring(8);
            var p = body.Split(';');
            if (p.Length == 2 &&
                float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float vx) &&
                float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float vy))
            {
                remoteBallVel = new Vector2(vx, vy);
                updateBallFromNetwork = true;
            }
            return;
        }

        if (msg.StartsWith("RESET"))
        {
            if (ball != null)
            {
                var b = ball.GetComponent<Ball>();
                if (b != null) b.ResetBall();
            }
            Debug.Log("[CLIENTE] RESET recebido -> bola reposicionada.");
            return;
        }

        // Aceita tanto GOAL quanto SCORE, trata igual e atualiza UI local
        if (msg.StartsWith("GOAL:") || msg.StartsWith("SCORE:"))
        {
            var body = msg.Substring(msg.IndexOf(':') + 1);
            if (int.TryParse(body, out int team))
            {
                if (gameManager != null)
                {
                    if (team == 1) gameManager.Team1Scored();
                    else if (team == 2) gameManager.Team2Scored();
                }
                Debug.Log($"[CLIENTE] Placar atualizado (team {team}).");
            }
            return;
        }
    }

    // =========================
    // ===== APLICAÇÕES ========
    // =========================

    private void UpdateRemotePaddles()
    {
        foreach (var kv in remotePlayersY)
        {
            int id = kv.Key;
            float y = kv.Value;
            var paddle = GetPaddleById(id);
            if (paddle == null) continue;
            if (id == myId) continue; // o meu eu movo local

            var p = paddle.transform.position;
            p.y = Mathf.Lerp(p.y, y, 0.5f);
            paddle.transform.position = p;
        }
    }

    private GameObject GetPaddleById(int id)
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
}
