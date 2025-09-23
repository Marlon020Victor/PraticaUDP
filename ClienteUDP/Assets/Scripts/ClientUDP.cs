using UnityEngine;

using System.Net.Sockets;

using System.Net;

using System.Text;

using System.Threading;

using System.Globalization;

public class UdpClientTwoClients : MonoBehaviour {

UdpClient client;

Thread receiveThread;

IPEndPoint serverEP;

int myId = -1;

Vector3 remotePos = Vector3.zero;

public GameObject localCube;

public GameObject remoteCube;

void Start() {

client = new UdpClient();

serverEP = new
IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001);

client.Connect(serverEP);

receiveThread = new Thread(ReceiveData);

receiveThread.Start();

client.Send(Encoding.UTF8.GetBytes("HELLO"), 5);

}
void ReceiveData() {

    IPEndPoint remoteEP = new
        IPEndPoint(IPAddress.Any, 0);

    while (true) {

        byte[] data = client.Receive(ref remoteEP);

        string msg = Encoding.UTF8.GetString(data);

        if (msg.StartsWith("ASSIGN:")) {

            myId = int.Parse(msg.Substring(7));

            Debug.Log("[Cliente] Meu ID = " + myId);

        }

        else if (msg.StartsWith("POS:")) {

            string[] parts = msg.Substring(4).Split(';');

            if (parts.Length == 3) {

                int id = int.Parse(parts[0]);

                if (id != myId) {

                    float x = float.Parse(parts[1],
                        CultureInfo.InvariantCulture);

                    float y = float.Parse(parts[2],
                        CultureInfo.InvariantCulture);

                    remotePos = new Vector3(x, y, 0);

                }

            }

        }

    }

}

void OnApplicationQuit() {

    receiveThread.Abort();

    client.Close();

}

}