using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine.SceneManagement;

public class LobbyClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    [Header("Servidor")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;

    [Header("Referências")]
    public LobbyManager lobbyManager;

    public int myId = -1;
    private bool isConnected = false;

    void Start()
    {
        if (lobbyManager == null)
            lobbyManager = FindFirstObjectByType<LobbyManager>();

        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            client.Connect(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;

            receiveThread = new Thread(ReceiveData);
            receiveThread.Start();

            Debug.Log($"[LOBBY CLIENT] Conectado ao servidor {serverIP}:{serverPort}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LOBBY CLIENT] Erro ao conectar: {e.Message}");
        }
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[2048];

        try
        {
            while (isConnected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                // Processa múltiplas mensagens se chegarem juntas
                string[] messages = message.Split('\n');
                foreach (string msg in messages)
                {
                    if (!string.IsNullOrEmpty(msg.Trim()))
                    {
                        string messageCopy = msg.Trim(); // Cópia para evitar problemas de closure
                        UnityMainThreadDispatcher.Instance().Enqueue(() => ProcessMessage(messageCopy));
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                Debug.LogError($"[LOBBY CLIENT] Erro ao receber: {e.Message}");
            });
        }
        finally
        {
            isConnected = false;
        }
    }

    void ProcessMessage(string message)
    {
        Debug.Log($"[LOBBY CLIENT] Recebeu: {message}");

        if (message.StartsWith("ASSIGN:"))
        {
            myId = int.Parse(message.Substring(7));
            Debug.Log($"[LOBBY CLIENT] Meu ID atribuído: {myId}");
            
            if (lobbyManager != null)
                lobbyManager.SetMyPlayerId(myId);
        }
        else if (message.StartsWith("REJECT:"))
        {
            string reason = message.Substring(7);
            Debug.LogWarning($"[LOBBY CLIENT] Rejeitado: {reason}");
            Debug.LogError($"Não foi possível entrar: {reason}");
        }
        else if (message.StartsWith("PLAYER_JOINED:"))
        {
            string[] parts = message.Substring(14).Split(';');
            if (parts.Length >= 2)
            {
                int playerId = int.Parse(parts[0]);
                string playerName = parts[1];
                
                if (lobbyManager != null)
                    lobbyManager.OnPlayerJoined(playerId, playerName);
            }
        }
        else if (message.StartsWith("PLAYER_LEFT:"))
        {
            string[] parts = message.Substring(12).Split(';');
            if (parts.Length >= 1)
            {
                int playerId = int.Parse(parts[0]);
                
                if (lobbyManager != null)
                    lobbyManager.OnPlayerLeft(playerId);
            }
        }
        else if (message.StartsWith("READY_STATUS:"))
        {
            string[] parts = message.Substring(13).Split(';');
            if (parts.Length >= 2)
            {
                int playerId = int.Parse(parts[0]);
                bool isReady = parts[1] == "1";
                
                if (lobbyManager != null)
                    lobbyManager.OnPlayerReadyChanged(playerId, isReady);
            }
        }
        else if (message.StartsWith("CHAT:"))
        {
            string[] parts = message.Substring(5).Split(new char[] { ';' }, 3);
            if (parts.Length >= 3)
            {
                int senderId = int.Parse(parts[0]);
                string senderName = parts[1];
                string chatMessage = parts[2];
                
                if (lobbyManager != null)
                    lobbyManager.OnChatMessage(senderName, chatMessage);
            }
        }
        else if (message.StartsWith("START_GAME"))
        {
            Debug.Log("[LOBBY CLIENT] Iniciando jogo!");
            StartGame();
        }
    }

    public void SendChatMessage(string message)
    {
        if (!isConnected || stream == null) return;

        try
        {
            string msg = $"CHAT:{message}\n";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);
            Debug.Log($"[LOBBY CLIENT] Enviou chat: {message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LOBBY CLIENT] Erro ao enviar chat: {e.Message}");
        }
    }

    public void SendReadyStatus(bool isReady)
    {
        if (!isConnected || stream == null) return;

        try
        {
            string msg = $"READY:{(isReady ? "1" : "0")}\n";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);
            Debug.Log($"[LOBBY CLIENT] Enviou status: {(isReady ? "PRONTO" : "NÃO PRONTO")}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LOBBY CLIENT] Erro ao enviar status: {e.Message}");
        }
    }

    void StartGame()
    {
        // Desconecta do servidor TCP (o jogo usará UDP)
        isConnected = false;
        
        try
        {
            stream?.Close();
            client?.Close();
        }
        catch { }

        // Carrega a cena do jogo
        // IMPORTANTE: Substitua "GameScene" pelo nome exato da sua cena de jogo
        SceneManager.LoadScene("GameScene");
    }

    void OnApplicationQuit()
    {
        isConnected = false;
        
        try
        {
            receiveThread?.Abort();
            stream?.Close();
            client?.Close();
        }
        catch { }
    }

    void OnDestroy()
    {
        isConnected = false;
        
        try
        {
            receiveThread?.Abort();
            stream?.Close();
            client?.Close();
        }
        catch { }
    }
}