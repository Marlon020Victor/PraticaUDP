using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;

public class UdpClientEcho : MonoBehaviour {

    UdpClient client;
    Thread receiveThread;
    IPEndPoint serverEP;

    public GameObject localCube;
    public GameObject echoCube;
    Vector3 echoPos = Vector3.zero;

    void Start() {

        client = new UdpClient();

        serverEP = new IPEndPoint(IPAddress.Parse("10.57.1.151"), 5001);

        client.Connect(serverEP);

        receiveThread = new Thread(ReceiveData);

        receiveThread.Start();

    }

    void Update() {

        float h = Input.GetAxis("Horizontal");

        float v = Input.GetAxis("Vertical");

        localCube.transform.Translate(new Vector3(h, v, 0) * Time.deltaTime * 5);

        string msg = localCube.transform.position.x.ToString("F2", CultureInfo.InvariantCulture) + ";" + localCube.transform.position.y.ToString("F2", CultureInfo.InvariantCulture);

        byte[] data = Encoding.UTF8.GetBytes(msg);

        client.Send(data, data.Length);

        if (echoCube != null)

            echoCube.transform.position =
                Vector3.Lerp(echoCube.transform.position,
                    echoPos, Time.deltaTime * 5);

    }

    void ReceiveData() {

        IPEndPoint remoteEP = new
            IPEndPoint(IPAddress.Any, 0);

        while (true) {

            byte[] data = client.Receive(ref remoteEP);

            string msg = Encoding.UTF8.GetString(data);

            string[] parts = msg.Split(';');

            if (parts.Length == 2) { float x =
                    float.Parse(parts[0], CultureInfo.InvariantCulture); float y = float.Parse(parts[1],CultureInfo.InvariantCulture);
                echoPos = new Vector3(x, y, 0);

            }

        }

    }

    void OnApplicationQuit() {

        receiveThread.Abort();

        client.Close();

    }

}