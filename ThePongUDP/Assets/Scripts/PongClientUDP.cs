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
    public int totalPlayersConnected = 0; // total de jogadores conectados

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
        // precisa ter um ID para operar
        if (myId == -1) return;

        // Atualiza posição dos paddles remotos (todos exceto o meu)
        UpdateRemotePaddles();

        // Player 1 tem autoridade sobre a bola — só envia quando o jogo começou
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
            // Players 2, 3 e 4 recebem e aplicam posição da bola quando houver updateRemoteBall
            if (updateRemoteBall && ball != null)
            {
                ball.transform.position = Vector3.MoveTowards(
                    ball.transform.position,
                    remoteBallPos,
                    Time.deltaTime * 20f
                );

                Rigidbody2D ballRig = ball.GetComponent<Rigidbody2D>();
                if (ballRig != null)
                {
                    ballRig.velocity = remoteBallVel;
                }
            }
        }

        // Envia posição do próprio paddle (enviamos sempre que temos myId, mesmo antes do START)
        if (Time.time - lastSendTime > (sendRate / 2f)) // throttle leve para paddles
        {
            SendPaddleData();
            lastSendTime = Time.time;
        }
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
            // Envia o id também para que o servidor/cliente saiba a quem pertence
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
            Vector2 vel = ballRig != null ? ballRig.velocity : Vector2.zero;

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
                // Jogo começou (pode vir com contagem de players após ':')
                else if (msg.StartsWith("START"))
                {
                    // Extrai quantos jogadores conectaram (se enviado)
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
                    else if (parsedTotal > 0)
                    {
                        // recebeu START mas com menos de 2 — aguarda
                        totalPlayersConnected = parsedTotal;
                        gameStarted = false;
                        Debug.Log($"[CLIENTE] START recebido, mas apenas {totalPlayersConnected} jogadores conectados — aguardando 2+ jogadores.");
                    }
                    else
                    {
                        // compatibilidade: se não vier quantidade, assume 4 (ou mantém false até o servidor realmente mandar conteudo)
                        totalPlayersConnected = 4;
                        gameStarted = true;
                        Debug.Log($"[CLIENTE] Jogo iniciado (sem contagem enviada).");
                    }
                }
                // Posição do paddle de outro jogador: PADDLE:<id>;<y>
                else if (msg.StartsWith("PADDLE:"))
                {
                    string payload = msg.Substring(7);
                    string[] parts = payload.Split(';');
                    if (parts.Length >= 2)
                    {
                        int id;
                        float y;
                        if (int.TryParse(parts[0], out id) && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                        {
                            if (id != myId)
                            {
                                remotePlayersY[id] = y;
                            }
                        }
                    }
                }
                // Posição da bola
                else if (msg.StartsWith("BALL:"))
                {
                    if (myId != 1) // Apenas clients não-autorizados recebem e aplicam
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
                // Gol marcado
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
                                    gameManager.Team1Scored();
                                else
                                    gameManager.Team2Scored();
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
