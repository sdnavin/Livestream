using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(RawImage))]

public class TCPReceive : MonoBehaviour
{

    RawImage myImage;

    [HideInInspector]
    public int port = 8010;
    [HideInInspector]
    public string IP = "127.0.0.1";

    TcpClient client;


    Thread networkthread;
    Thread streamthread;


    TcpClient streamClient;

    int widthT;
    int heightT;
    int streamsize;
    int Colorassigned = 0;


    Color32[] tmpColor;

    Texture2D Rtex;

    int Connected = 0;
    int requireReload = 0;

    // Use this for initialization
    void Start()
    {
        Application.runInBackground = true;

        port = int.Parse(CameraManager.instance.CurrentAddress.PortNo);
        IP = (CameraManager.instance.CurrentAddress.IPAddress);

        widthT = int.Parse(CameraManager.instance.CurrentAddress.Width);
        heightT = int.Parse(CameraManager.instance.CurrentAddress.Height);
        myImage = GetComponent<RawImage>();
        myImage.rectTransform.sizeDelta = new Vector2(widthT, heightT);
        streamsize = widthT * heightT * 3;
        Rtex = new Texture2D(widthT, heightT, TextureFormat.RGB24, false);
        client = new TcpClient();
        // connect to server
        networkthread = new Thread(new ThreadStart(NetworkThreadMethod));
        networkthread.IsBackground = true;
        networkthread.Start();

        streamthread = new Thread(new ThreadStart(streamThreadMethod));
        streamthread.IsBackground = true;

    }
    public void NetworkThreadMethod()
    {
        while (!stopthread)
        {
            try
            {
                if (Connected == 0)
                {
                    Debug.LogWarning("Connecting to server...");
                    // if on desktop
                    client.Connect(IPAddress.Parse(IP), port);
                    Debug.LogWarning("Connected!");
                    Connected = 1;
                    streamthread.Start();
                }
                else
                {
                    // networkthread.Abort();
                }
            }
            catch (Exception e)
            {
                print("" + e);
            }
        }
    }
    float lastTimeRequestedTex = 0;


    bool stopthread;
    void streamThreadMethod()
    {
        while (!stopthread)
        {
            try
            {
                var serverStream = client.GetStream();
                client.ReceiveBufferSize = streamsize;


                // request the texture from the server 
                if (serverStream.CanWrite && Colorassigned == 0)
                {
                    // send request
                    serverStream.WriteByte(byte.MaxValue);
                    serverStream.Flush();
                    //Debug.Log("Succesfully send 1 byte");
                    Colorassigned = 1;
                }



                if (serverStream.CanRead && Colorassigned == 1)
                {
                    // Read the bytes 
                    using (var writer = new MemoryStream())
                    {
                        var readBuffer = new byte[client.ReceiveBufferSize];

                        int numberOfBytesRead;
                        if (streamsize == (numberOfBytesRead = serverStream.Read(readBuffer, 0, readBuffer.Length)))
                        {
                            writer.Write(readBuffer, 0, numberOfBytesRead);
                            Thread.Sleep(1);
                        }
                        //print(readBuffer.Length+"W : " + writer.Length);
                        if (writer.Length == streamsize)
                        {

                            byte[] pngBytes = writer.ToArray();

                            tmpColor = new Color32[widthT * heightT];

                            for (int i = 0; i < tmpColor.Length; i++)
                            {
                                tmpColor[i].b = pngBytes[3 * i];
                                tmpColor[i].g = pngBytes[3 * i + 1];
                                tmpColor[i].r = pngBytes[3 * i + 2];
                            }
                            gotfinaldata = 1;
                            Thread.Sleep(1);

                            writer.Flush();
                        }
                        Colorassigned = 0;
                    }
                }
            }
            catch (System.Exception er)
            {
                print("" + er.ToString());
                requireReload = 1;
            }
        }
    }



    int gotfinaldata = 0;
    // Update is called once per frame
    void Update()
    {

        if (Connected == 1)
        {
            CameraManager.instance.ConnectionMade.Invoke();
            Connected = 2;
        }
        if (requireReload == 1)
        {
            OnApplicationQuit();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (tmpColor != null && gotfinaldata == 1)
        {
            Rtex.SetPixels32(tmpColor);
            Rtex.Apply();
            if (Rtex.width != 8)
            {
                myImage.texture = Rtex;
            }
            gotfinaldata = 0;
        }
    }

    void OnApplicationQuit()
    {
        Debug.LogWarning("OnApplicationQuit");
        stopthread = true;
        if (streamthread != null && streamthread.IsAlive)
            streamthread.Abort();
        if (networkthread != null && networkthread.IsAlive)
            networkthread.Abort();
        if (client != null)
            client.Close();

    }
}