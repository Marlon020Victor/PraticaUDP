using UnityEngine;

public class DebugHelper : MonoBehaviour
{
    public PongClientUDP networkClient;
    public GameObject paddle1, paddle2, paddle3, paddle4;
    
    private float debugInterval = 2f;
    private float lastDebugTime = 0f;

    void Start()
    {
        if (networkClient == null)
            networkClient = FindFirstObjectByType<PongClientUDP>();
    }

    void Update()
    {
        if (Time.time - lastDebugTime > debugInterval)
        {
            PrintDebugInfo();
            lastDebugTime = Time.time;
        }

        // Mostra controles na tela
        if (networkClient != null && networkClient.myId > 0)
        {
            string controls = GetControlsForPlayer(networkClient.myId);
            Debug.Log($"[DEBUG] Você é o Player {networkClient.myId}. Controles: {controls}");
        }
    }

    void PrintDebugInfo()
    {
        if (networkClient == null)
        {
            Debug.LogError("[DEBUG] NetworkClient é NULL!");
            return;
        }

        Debug.Log("=== DEBUG INFO ===");
        Debug.Log($"Meu ID: {networkClient.myId}");
        Debug.Log($"Jogo iniciado: {(networkClient.myId != -1 ? "SIM" : "NÃO")}");
        
        if (paddle1) Debug.Log($"Paddle 1 Y: {paddle1.transform.position.y:F2}");
        if (paddle2) Debug.Log($"Paddle 2 Y: {paddle2.transform.position.y:F2}");
        if (paddle3) Debug.Log($"Paddle 3 Y: {paddle3.transform.position.y:F2}");
        if (paddle4) Debug.Log($"Paddle 4 Y: {paddle4.transform.position.y:F2}");
        
        Debug.Log("==================");
    }

    string GetControlsForPlayer(int playerId)
    {
        switch (playerId)
        {
            case 1: return "↑ ↓ (Setas)";
            case 2: return "W S";
            case 3: return "T G";
            case 4: return "I K";
            default: return "???";
        }
    }

    void OnGUI()
    {
        if (networkClient == null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;

        string info = $"Player ID: {networkClient.myId}\n";
        
        if (networkClient.myId > 0)
        {
            info += $"Controles: {GetControlsForPlayer(networkClient.myId)}\n";
            info += $"Seu Paddle: {networkClient.myId}";
        }

        GUI.Label(new Rect(10, 10, 300, 100), info, style);
    }
}