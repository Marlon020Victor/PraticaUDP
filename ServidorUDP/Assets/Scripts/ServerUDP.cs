using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UdpServerEcho : MonoBehaviour {

    UdpClient server;
    IPEndPoint anyEP;
    Thread receiveThread;

    void Start() {

        server = new UdpClient(5001);
        anyEP = new IPEndPoint(IPAddress.Any, 0);
        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();

        Debug.Log("Servidor iniciado na porta 5001");

    }

    void ReceiveData() {

        while (true) {

            byte[] data = server.Receive(ref anyEP);
            string msg = Encoding.UTF8.GetString(data);

            Debug.Log("Posição recebida: " + msg);
            server.Send(data, data.Length, anyEP);

        }

    }

    void OnApplicationQuit() {

        receiveThread.Abort();

        server.Close();

    }

}