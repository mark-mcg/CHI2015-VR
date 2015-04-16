using UnityEngine;
using System.Collections;
using Assets.Scripts.Keyboard;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class NetworkTextDisplay : MonoBehaviour, TextInput.TextReceiver
{
    // State object for receiving data from remote device.
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    private const int port = 8001;

    // ManualResetEvent instances signal completion.
    private static ManualResetEvent connectDone =
        new ManualResetEvent(false);
    private static ManualResetEvent sendDone =
        new ManualResetEvent(false);
    private static ManualResetEvent receiveDone =
        new ManualResetEvent(false);

    // The response from the remote device.
    private static String response = String.Empty;
    public bool isInstruction;

    private StateObject client;

	// Use this for initialization
	void Start () {

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

        client = new StateObject();
        client.workSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Connect to the remote endpoint.
        client.workSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client.workSocket); 
        //connectDone.WaitOne();

        // Send test data to the remote device.
        //sendDone.WaitOne();

        //client.workSocket.BeginReceive(client.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), client);
	}

    void onDestroy()
    {
        client.workSocket.Close();
    }

    public void setIsInstruction(bool isInstruction)
    {
        this.isInstruction = isInstruction;
    }

    public void ReceiveText(string transcribedText, string inputStream, bool execute)
    {
        try
        {
            if (client != null && client.workSocket != null)
                Send(client.workSocket, (isInstruction? "1" : "0") + transcribedText + "\n");
            else
                Debug.Log("Cant send socket is null");
        }
        catch (Exception e)
        {
            //Debug.Log("Exception in ReceiveText " + e.ToString());
        }
    }

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.
            client.EndConnect(ar);

            Debug.Log("Socket connected to " + client.RemoteEndPoint.ToString());
            Send(client, "Unity connected to network display\n");

            // Signal that the connection has been made.
            connectDone.Set();
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    private static void Receive(Socket client)
    {
        try
        {
            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = client;

            // Begin receiving the data from the remote device.
            //client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            //    new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket 
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            else
            {
                // All the data has arrived; put it in response.
                if (state.sb.Length > 1)
                {
                    response = state.sb.ToString();
                }
                // Signal that all bytes have been received.
                receiveDone.Set();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    private static void Send(Socket client, String data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = client.EndSend(ar);

            // Signal that all bytes have been sent.
            sendDone.Set();
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

}
