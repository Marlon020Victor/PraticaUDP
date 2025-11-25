using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class ServerTCP : MonoBehaviour
{
    private TcpListener server;
    private Thread listenerThread;
    private Dictionary<int, ClientConnection> clients = new Dictionary<int, ClientConnection>();
    private int nextId = 1;
    private int maxPlayers = 4;
    private object lockObj = new object();

    [Header("Config")]
    [SerializeField] private int port = 5000;
    [SerializeField] private int minPlayersToStart = 2;

    private class ClientConnection
    {
        public TcpClient client;
        public NetworkStream stream;
        public Thread thread;
        public bool isReady;
        public string playerName;
        public IPEndPoint endpoint;
    }

    void Start()
    {
        StartServer();
    }

    void StartServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            listenerThread = new Thread(ListenForClients);
            listenerThread.Start();

            Debug.Log($"[SERVER TCP] Servidor iniciado na porta {port}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SERVER TCP] Erro ao iniciar: {e.Message}");
        }
    }

    void ListenForClients()
    {
        while (true)
        {
            try
            {
                TcpClient client = server.AcceptTcpClient();
                
                lock (lockObj)
                {
                    if (clients.Count >= maxPlayers)
                    {
                        // Servidor cheio
                        NetworkStream stream = client.GetStream();
                        string rejectMsg = "REJECT:Servidor cheio\n";
                        byte[] data = Encoding.UTF8.GetBytes(rejectMsg);
                        stream.Write(data, 0, data.Length);
                        client.Close();
                        Debug.Log("[SERVER TCP] Cliente rejeitado - servidor cheio");
                        continue;
                    }

                    int playerId = nextId++;
                    ClientConnection conn = new ClientConnection
                    {
                        client = client,
                        stream = client.GetStream(),
                        isReady = false,
                        playerName = $"Player {playerId}",
                        endpoint = (IPEndPoint)client.Client.RemoteEndPoint
                    };

                    clients[playerId] = conn;

                    // Envia ID para o cliente
                    string assignMsg = $"ASSIGN:{playerId}\n";
                    byte[] assignData = Encoding.UTF8.GetBytes(assignMsg);
                    conn.stream.Write(assignData, 0, assignData.Length);

                    Debug.Log($"[SERVER TCP] {conn.playerName} conectado (ID: {playerId})");

                    // Envia estado atual para o novo cliente
                    SendCurrentStateToClient(playerId);

                    // Notifica todos sobre o novo jogador
                    BroadcastPlayerJoined(playerId, conn.playerName);

                    // Thread para receber mensagens deste cliente
                    conn.thread = new Thread(() => HandleClient(playerId));
                    conn.thread.Start();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SERVER TCP] Erro ao aceitar cliente: {e.Message}");
                break;
            }
        }
    }

    void SendCurrentStateToClient(int newPlayerId)
    {
        lock (lockObj)
        {
            var newClient = clients[newPlayerId];
            
            // Envia informações de todos os outros jogadores
            foreach (var kvp in clients)
            {
                if (kvp.Key != newPlayerId)
                {
                    string playerMsg = $"PLAYER_JOINED:{kvp.Key};{kvp.Value.playerName}\n";
                    byte[] data = Encoding.UTF8.GetBytes(playerMsg);
                    newClient.stream.Write(data, 0, data.Length);

                    if (kvp.Value.isReady)
                    {
                        string readyMsg = $"READY_STATUS:{kvp.Key};1\n";
                        byte[] readyData = Encoding.UTF8.GetBytes(readyMsg);
                        newClient.stream.Write(readyData, 0, readyData.Length);
                    }
                }
            }
        }
    }

    void HandleClient(int playerId)
    {
        ClientConnection conn = clients[playerId];
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = conn.stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // Cliente desconectou

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                Debug.Log($"[SERVER TCP] Recebeu de {conn.playerName}: {message}");

                ProcessMessage(playerId, message);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SERVER TCP] Erro com {conn.playerName}: {e.Message}");
        }
        finally
        {
            RemoveClient(playerId);
        }
    }

    void ProcessMessage(int playerId, string message)
    {
        if (message.StartsWith("CHAT:"))
        {
            // Formato: CHAT:mensagem
            string chatMsg = message.Substring(5);
            BroadcastChat(playerId, chatMsg);
        }
        else if (message.StartsWith("READY:"))
        {
            // Formato: READY:1 ou READY:0
            string[] parts = message.Split(':');
            if (parts.Length >= 2)
            {
                bool isReady = parts[1] == "1";
                SetPlayerReady(playerId, isReady);
            }
        }
    }

    void BroadcastChat(int senderId, string message)
    {
        lock (lockObj)
        {
            string senderName = clients.ContainsKey(senderId) ? clients[senderId].playerName : "Unknown";
            string broadcastMsg = $"CHAT:{senderId};{senderName};{message}\n";
            BroadcastToAll(broadcastMsg);
            Debug.Log($"[SERVER TCP] Chat de {senderName}: {message}");
        }
    }

    void SetPlayerReady(int playerId, bool isReady)
    {
        lock (lockObj)
        {
            if (clients.ContainsKey(playerId))
            {
                clients[playerId].isReady = isReady;
                string readyMsg = $"READY_STATUS:{playerId};{(isReady ? "1" : "0")}\n";
                BroadcastToAll(readyMsg);
                Debug.Log($"[SERVER TCP] {clients[playerId].playerName} status: {(isReady ? "PRONTO" : "NÃO PRONTO")}");

                // Verifica se pode iniciar o jogo
                CheckStartGame();
            }
        }
    }

    void CheckStartGame()
    {
        int readyCount = 0;
        foreach (var kvp in clients)
        {
            if (kvp.Value.isReady) readyCount++;
        }

        Debug.Log($"[SERVER TCP] Jogadores prontos: {readyCount}/{clients.Count}");

        if (readyCount >= minPlayersToStart && clients.Count >= minPlayersToStart)
        {
            Debug.Log("[SERVER TCP] Iniciando jogo!");
            BroadcastToAll("START_GAME\n");
        }
    }

    void BroadcastPlayerJoined(int playerId, string playerName)
    {
        string msg = $"PLAYER_JOINED:{playerId};{playerName}\n";
        BroadcastToAll(msg);
    }

    void BroadcastToAll(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        lock (lockObj)
        {
            List<int> disconnected = new List<int>();
            
            foreach (var kvp in clients)
            {
                try
                {
                    kvp.Value.stream.Write(data, 0, data.Length);
                }
                catch
                {
                    disconnected.Add(kvp.Key);
                }
            }

            // Remove clientes desconectados
            foreach (int id in disconnected)
            {
                RemoveClient(id);
            }
        }
    }

    void RemoveClient(int playerId)
    {
        lock (lockObj)
        {
            if (clients.ContainsKey(playerId))
            {
                ClientConnection conn = clients[playerId];
                Debug.Log($"[SERVER TCP] {conn.playerName} desconectou");

                try
                {
                    conn.stream?.Close();
                    conn.client?.Close();
                    conn.thread?.Abort();
                }
                catch { }

                string playerName = conn.playerName;
                clients.Remove(playerId);

                // Notifica outros jogadores
                string msg = $"PLAYER_LEFT:{playerId};{playerName}\n";
                BroadcastToAll(msg);
            }
        }
    }

    void OnApplicationQuit()
    {
        lock (lockObj)
        {
            foreach (var kvp in clients)
            {
                try
                {
                    kvp.Value.stream?.Close();
                    kvp.Value.client?.Close();
                    kvp.Value.thread?.Abort();
                }
                catch { }
            }
            clients.Clear();
        }

        try
        {
            listenerThread?.Abort();
            server?.Stop();
        }
        catch { }

        Debug.Log("[SERVER TCP] Servidor encerrado");
    }
}