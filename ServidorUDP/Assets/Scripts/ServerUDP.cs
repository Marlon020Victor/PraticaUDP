using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;

public class ServerUDP : MonoBehaviour
{
    UdpClient server;
    IPEndPoint anyEP;
    Thread receiveThread;
    
    Dictionary<string, int> clientIds = new Dictionary<string, int>();
    Dictionary<int, PlayerData> playerPositions = new Dictionary<int, PlayerData>();
    BallData ballData = new BallData();
    
    int nextId = 1;
    
    [Header("Configurações")]
    public int maxPlayers = 4;
    public int minPlayers = 2; // NOVO: mínimo de jogadores para iniciar
    public float waitTimeBeforeStart = 10f; // NOVO: tempo de espera antes de iniciar com menos jogadores
    
    private float firstPlayerConnectTime = -1f;
    private bool gameStarted = false;
    public GameObject threadDispatcher;
    
    [System.Serializable]
    public class PlayerData
    {
        public float y;
        public IPEndPoint endpoint;
    }
    
    [System.Serializable]
    public class BallData
    {
        public float x;
        public float y;
        public float vx;
        public float vy;
    }

    void Start()
    {
        _ = UnityMainThreadDispatcher.Instance();
        server = new UdpClient(5001);
        anyEP = new IPEndPoint(IPAddress.Any, 0);
        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();
        
        Debug.Log($"[SERVIDOR] Iniciado na porta 5001 - Esperando {minPlayers}-{maxPlayers} jogadores");
    }
    
    void Update()
    {
        // Verifica se deve iniciar o jogo com menos jogadores
        if (!gameStarted && firstPlayerConnectTime > 0)
        {
            float waitedTime = Time.time - firstPlayerConnectTime;
            
            if (clientIds.Count >= minPlayers && waitedTime >= waitTimeBeforeStart)
            {
                StartGame();
            }
        }
    }
    
    void StartGame()
    {
        if (gameStarted) return;
        
        gameStarted = true;
        string startMsg = $"START:{clientIds.Count}";
        BroadcastToAll(startMsg);
        Debug.Log($"[SERVIDOR] Jogo iniciado com {clientIds.Count} jogadores!");
    }

    void ReceiveData()
    {
        while (true)
        {
            try
            {
                byte[] data = server.Receive(ref anyEP);
                string msg = Encoding.UTF8.GetString(data);
                string key = anyEP.Address + ":" + anyEP.Port;

                if (msg.StartsWith("HELLO"))
                {
                    if (!clientIds.ContainsKey(key))
                    {
                        if (clientIds.Count >= maxPlayers)
                        {
                            string rejectMsg = $"REJECT:Servidor cheio ({maxPlayers}/{maxPlayers} jogadores)";
                            server.Send(Encoding.UTF8.GetBytes(rejectMsg), rejectMsg.Length, anyEP);
                            Debug.Log("[SERVIDOR] Rejeitado cliente - servidor cheio");
                            continue;
                        }
                        
                        clientIds[key] = nextId;
                        playerPositions[nextId] = new PlayerData { y = 0, endpoint = anyEP };
                        
                        string assignMsg = "ASSIGN:" + nextId;
                        server.Send(Encoding.UTF8.GetBytes(assignMsg), assignMsg.Length, anyEP);
                        
                        Debug.Log($"[SERVIDOR] Cliente {nextId} conectado ({clientIds.Count}/{maxPlayers})");
                        
                        // Marca o tempo do primeiro jogador
                        if (clientIds.Count == 1)
                        {
                            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                                firstPlayerConnectTime = Time.time;
                            });
                        }
                        
                        // Inicia imediatamente se atingir o máximo
                        if (clientIds.Count == maxPlayers)
                        {
                            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                                StartGame();
                            });
                        }
                        
                        nextId++;
                    }
                }
                else if (msg.StartsWith("PADDLE:"))
                {
                    if (clientIds.ContainsKey(key))
                    {
                        int id = clientIds[key];
                        string[] parts = msg.Substring(7).Split(';');
                        
                        if (parts.Length >= 1)
                        {
                            float y = float.Parse(parts[0], CultureInfo.InvariantCulture);
                            playerPositions[id].y = y;
                            
                            string broadcast = $"PADDLE:{id};{y.ToString("F3", CultureInfo.InvariantCulture)}";
                            BroadcastToAll(broadcast);
                        }
                    }
                }
                else if (msg.StartsWith("BALL:"))
                {
                    if (clientIds.ContainsKey(key) && clientIds[key] == 1)
                    {
                        string[] parts = msg.Substring(5).Split(';');
                        
                        if (parts.Length >= 4)
                        {
                            ballData.x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                            ballData.y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            ballData.vx = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            ballData.vy = float.Parse(parts[3], CultureInfo.InvariantCulture);
                            
                            BroadcastToAll(msg);
                        }
                    }
                }
                else if (msg.StartsWith("GOAL:"))
                {
                    if (clientIds.ContainsKey(key))
                    {
                        string[] parts = msg.Substring(5).Split(';');
                        if (parts.Length >= 2)
                        {
                            BroadcastToAll(msg);
                            Debug.Log($"[SERVIDOR] Gol marcado! {msg}");
                        }
                    }
                }
                else if (msg.StartsWith("RESET"))
                {
                    if (clientIds.ContainsKey(key) && clientIds[key] == 1)
                    {
                        BroadcastToAll("RESET");
                        Debug.Log("[SERVIDOR] Jogo resetado");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[SERVIDOR] Erro: " + e.Message);
            }
        }
    }
    
    void BroadcastToAll(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        
        foreach (var kvp in clientIds)
        {
            var parts = kvp.Key.Split(':');
            IPEndPoint ep = new IPEndPoint(
                IPAddress.Parse(parts[0]),
                int.Parse(parts[1])
            );
            
            server.Send(data, data.Length, ep);
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        
        if (server != null)
        {
            server.Close();
        }
    }
}