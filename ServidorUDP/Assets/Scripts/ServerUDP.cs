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

    [Header("Config")]
    [SerializeField] int maxPlayers = 4;
    [SerializeField] int minPlayersToStart = 2; // <= maxPlayers

    [System.Serializable]
    public class PlayerData { public float y; public IPEndPoint endpoint; }

    [System.Serializable]
    public class BallData { public float x, y, vx, vy; }

    void Start()
    {
        server = new UdpClient(5001);
        anyEP = new IPEndPoint(IPAddress.Any, 0);
        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();
        Debug.Log("[SERVIDOR] Iniciado na porta 5001");
    }

    void MaybeStartGame()
    {
        if (clientIds.Count >= Mathf.Clamp(minPlayersToStart, 1, maxPlayers))
        {
            BroadcastToAll("START");
            Debug.Log("[SERVIDOR] START enviado (atingiu mínimo de jogadores).");
        }
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
                            string rejectMsg = "REJECT:Servidor cheio";
                            server.Send(Encoding.UTF8.GetBytes(rejectMsg), rejectMsg.Length, anyEP);
                            Debug.Log("[SERVIDOR] Rejeitado cliente - servidor cheio");
                            continue;
                        }

                        clientIds[key] = nextId;
                        playerPositions[nextId] = new PlayerData { y = 0, endpoint = anyEP };

                        string assignMsg = "ASSIGN:" + nextId;
                        server.Send(Encoding.UTF8.GetBytes(assignMsg), assignMsg.Length, anyEP);
                        Debug.Log($"[SERVIDOR] Cliente {nextId} conectado");

                        nextId++;

                        // Inicia assim que atingir o mínimo
                        MaybeStartGame();
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
                        // Apenas roteia o gol
                        BroadcastToAll(msg);
                        Debug.Log($"[SERVIDOR] Gol marcado! {msg}");
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
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
            server.Send(data, data.Length, ep);
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
        server?.Close();
    }
}
