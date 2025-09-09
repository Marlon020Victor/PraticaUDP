using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UdpServerUnity : MonoBehaviour {

    UdpClient server;
    IPEndPoint clientEP;
    Thread receiveThread;

    void Start() {

        server = new UdpClient(5000);
        clientEP = new IPEndPoint(IPAddress.Any, 0);
        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();

        Debug.Log("Servidor iniciado na porta 5000");

    }

    void ReceiveData() {

        while (true) {

            byte[] data = server.Receive(ref clientEP);

            string msg = Encoding.UTF8.GetString(data);

            Debug.Log("Recebido: " + msg);

            byte[] echo = Encoding.UTF8.GetBytes("Eco: " + msg);

            server.Send(echo, echo.Length, clientEP);

        }

    }

    void OnApplicationQuit() {

        receiveThread.Abort();

        server.Close();

    }

}