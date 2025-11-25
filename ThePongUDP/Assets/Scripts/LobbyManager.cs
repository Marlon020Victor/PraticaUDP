using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [Header("Player Slots (UI Images nos cantos)")]
    public Image player1Slot; // Canto superior esquerdo
    public Image player2Slot; // Canto superior direito
    public Image player3Slot; // Canto inferior esquerdo
    public Image player4Slot; // Canto inferior direito

    [Header("Player Names (TextMeshPro dentro dos slots)")]
    public TextMeshProUGUI player1Text;
    public TextMeshProUGUI player2Text;
    public TextMeshProUGUI player3Text;
    public TextMeshProUGUI player4Text;

    [Header("Cores dos Players")]
    public Color notReadyColor = Color.gray;
    public Color player1ReadyColor = Color.yellow;
    public Color player2ReadyColor = Color.green;
    public Color player3ReadyColor = Color.blue;
    public Color player4ReadyColor = new Color(0.4f, 0f, 0.9f); // Roxo

    [Header("Chat UI (parte inferior central)")]
    public ScrollRect chatScrollRect;
    public TextMeshProUGUI chatText;
    public TMP_InputField chatInputField;
    public Button sendButton;

    [Header("Ready Dropdown (parte superior central)")]
    public TMP_Dropdown readyDropdown;

    [Header("Referências")]
    public LobbyClient lobbyClient;

    private int myPlayerId = -1;
    private Dictionary<int, PlayerSlotData> playerSlots = new Dictionary<int, PlayerSlotData>();
    private List<string> chatMessages = new List<string>();

    private class PlayerSlotData
    {
        public Image slotImage;
        public TextMeshProUGUI slotText;
        public Color readyColor;
        public bool isReady;
        public string playerName;
    }

    void Start()
    {
        InitializePlayerSlots();
        InitializeChat();
        InitializeDropdown();

        if (lobbyClient == null)
            lobbyClient = FindFirstObjectByType<LobbyClient>();
    }

    void InitializePlayerSlots()
    {
        // Configura os slots dos players
        playerSlots[1] = new PlayerSlotData 
        { 
            slotImage = player1Slot, 
            slotText = player1Text,
            readyColor = player1ReadyColor,
            playerName = "",
            isReady = false
        };
        
        playerSlots[2] = new PlayerSlotData 
        { 
            slotImage = player2Slot, 
            slotText = player2Text,
            readyColor = player2ReadyColor,
            playerName = "",
            isReady = false
        };
        
        playerSlots[3] = new PlayerSlotData 
        { 
            slotImage = player3Slot, 
            slotText = player3Text,
            readyColor = player3ReadyColor,
            playerName = "",
            isReady = false
        };
        
        playerSlots[4] = new PlayerSlotData 
        { 
            slotImage = player4Slot, 
            slotText = player4Text,
            readyColor = player4ReadyColor,
            playerName = "",
            isReady = false
        };

        // Inicializa todos como cinza e vazios
        foreach (var kvp in playerSlots)
        {
            kvp.Value.slotImage.color = notReadyColor;
            kvp.Value.slotText.text = "";
        }
    }

    void InitializeChat()
    {
        chatText.text = "";
        chatInputField.text = "";

        // Botão de enviar
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendChatMessage);
        }

        // Enter para enviar mensagem
        chatInputField.onSubmit.AddListener((string text) => 
        {
            SendChatMessage();
        });
    }

    void InitializeDropdown()
    {
        if (readyDropdown != null)
        {
            readyDropdown.ClearOptions();
            readyDropdown.AddOptions(new List<string> { "NÃO PRONTO", "PRONTO" });
            readyDropdown.value = 0; // NÃO PRONTO por padrão
            
            readyDropdown.onValueChanged.AddListener(OnReadyDropdownChanged);
        }
    }

    public void SetMyPlayerId(int playerId)
    {
        myPlayerId = playerId;
        Debug.Log($"[LOBBY MANAGER] Meu player ID: {myPlayerId}");
        
        // Adiciona mensagem de boas-vindas no chat
        AddChatMessage("Sistema", $"Você entrou como Player {playerId}");
    }

    public void OnPlayerJoined(int playerId, string playerName)
    {
        Debug.Log($"[LOBBY MANAGER] Player {playerId} entrou: {playerName}");
        
        if (playerSlots.ContainsKey(playerId))
        {
            playerSlots[playerId].playerName = playerName;
            playerSlots[playerId].slotText.text = playerName;
            
            // Mantém a cor atual (não pronto por padrão)
            if (!playerSlots[playerId].isReady)
            {
                playerSlots[playerId].slotImage.color = notReadyColor;
            }
        }

        // Mensagem no chat (apenas se não for eu mesmo)
        if (playerId != myPlayerId)
        {
            AddChatMessage("Sistema", $"{playerName} entrou na sala");
        }
    }

    public void OnPlayerLeft(int playerId)
    {
        Debug.Log($"[LOBBY MANAGER] Player {playerId} saiu");
        
        if (playerSlots.ContainsKey(playerId))
        {
            string playerName = playerSlots[playerId].playerName;
            playerSlots[playerId].slotImage.color = notReadyColor;
            playerSlots[playerId].slotText.text = "";
            playerSlots[playerId].playerName = "";
            playerSlots[playerId].isReady = false;

            if (!string.IsNullOrEmpty(playerName))
            {
                AddChatMessage("Sistema", $"{playerName} saiu da sala");
            }
        }
    }

    public void OnPlayerReadyChanged(int playerId, bool isReady)
    {
        Debug.Log($"[LOBBY MANAGER] Player {playerId} status: {(isReady ? "PRONTO" : "NÃO PRONTO")}");
        
        if (playerSlots.ContainsKey(playerId))
        {
            playerSlots[playerId].isReady = isReady;
            
            if (isReady)
            {
                playerSlots[playerId].slotImage.color = playerSlots[playerId].readyColor;
            }
            else
            {
                playerSlots[playerId].slotImage.color = notReadyColor;
            }
        }
    }

    public void OnChatMessage(string senderName, string message)
    {
        AddChatMessage(senderName, message);
    }

    void AddChatMessage(string sender, string message)
    {
        string formattedMessage = $"<b>{sender}:</b> {message}";
        chatMessages.Add(formattedMessage);
        
        // Atualiza o texto do chat
        chatText.text = string.Join("\n", chatMessages);
        
        // Auto-scroll para baixo
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void SendChatMessage()
    {
        string message = chatInputField.text.Trim();
        
        if (string.IsNullOrEmpty(message)) return;
        if (lobbyClient == null) return;

        lobbyClient.SendChatMessage(message);
        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    void OnReadyDropdownChanged(int index)
    {
        if (lobbyClient == null) return;

        bool isReady = (index == 1); // 0 = NÃO PRONTO, 1 = PRONTO
        lobbyClient.SendReadyStatus(isReady);
    }
}