using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RawImage))]

public class TCPSend : MonoBehaviour
{



    #region Netowrking 

    private Thread networkthread;
    private Thread streamthread;


    private TcpClient streamClient;
    private TcpClient PNGClient;

    private TcpListener listner;

    private int port = 8010;

    private bool stop = false;

    private List<TcpClient> clients = new List<TcpClient>();

    #endregion

    private WebCamTexture webCam;

    private int streamsize, requireReload;
    private int Connected = 0;

    RawImage myImage;

    private void Start()
    {
        requireReload = 0;
        port = int.Parse(CameraManager.instance.CurrentAddress.PortNo);

        Application.runInBackground = true;

        webCam = new WebCamTexture();
        webCam.deviceName = WebCamTexture.devices[CameraManager.instance.CurrentAddress.Webcam].name;
        webCam.requestedHeight = int.Parse(CameraManager.instance.CurrentAddress.Height);
        webCam.requestedWidth = int.Parse(CameraManager.instance.CurrentAddress.Width);
        webCam.Play();
        myImage = GetComponent<RawImage>();
        myImage.rectTransform.sizeDelta = new Vector2(webCam.requestedWidth ,webCam.requestedHeight);
        myImage.texture = webCam;
        streamsize = webCam.requestedHeight * webCam.requestedWidth * 3;
        print("w" + webCam.width + "H" + webCam.height);

        // Connect to the server
        listner = new TcpListener(port);
        listner.Start();

        networkthread = new Thread(new ThreadStart(ThreadMethod));
        networkthread.IsBackground = true;
        networkthread.Start();

        streamthread = new Thread(new ThreadStart(streamThreadMethod));
        streamthread.IsBackground = true;
    }

    byte[] pngBytes;
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
        try
        {


            if (senddata && dataload == 0)
            {

                Color32[] data = new Color32[webCam.width * webCam.height];
                pngBytes = new byte[webCam.width * webCam.height * 3];
                data = webCam.GetPixels32();

                for (int i = 0; i < data.Length; i++)
                {
                    pngBytes[3 * i] = data[i].b;
                    pngBytes[3 * i + 1] = data[i].g;
                    pngBytes[3 * i + 2] = data[i].r;
                }
                dataload = 1;
                //  print("" + pngBytes.Length);
                //Color32[] tmp = new Color32[webCam.width * webCam.height];
                //for (int i = 0; i < tmp.Length; i++)
                //{
                //    tmp[i].b = pngBytes[3 * i];
                //    tmp[i].g = pngBytes[3 * i + 1];
                //    tmp[i].r = pngBytes[3 * i + 2];
                //}
                //currentTexture.SetPixels32(tmp);
                //currentTexture.Apply();
                //myImage.texture = currentTexture;

            }
        }
        catch (Exception er)
        {
            print("" + er.ToString());
        }

    }


    private bool senddata;

    private int dataload = 0;

    void streamThreadMethod()
    {
        while (!stop)
        {
            try
            {
                if (streamClient.Connected)
                {
                    var stream = streamClient.GetStream();
                    // print("In");
                    if (stream.CanRead && !senddata)
                    {
                        // we need storage for data
                        using (var messageData = new MemoryStream())
                        {
                            Byte[] buffer = new Byte[streamClient.ReceiveBufferSize];


                            while (stream.DataAvailable)
                            {
                                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                                if (bytesRead == 0)
                                    break;

                                // Writes to the data storage
                                messageData.Write(buffer, 0, bytesRead);
                                Thread.Sleep(1);
                            }

                            if (messageData.Length > 0)
                            {
                                senddata = true;
                            }
                        }
                    }

                    streamClient.SendBufferSize = streamsize;
                    if (stream.CanWrite && senddata && dataload == 1)
                    {
                        // Write the image bytes
                        if (streamClient != null)
                        {
                            stream.Write(pngBytes, 0, pngBytes.Length);
                            Thread.Sleep(1);
                            // send it 
                            stream.Flush();
                        }
                        senddata = false;
                        dataload = 0;
                    }
                }
            }
            catch (Exception er)
            {
                print(er.ToString());
                requireReload = 1;

            }
        }

    }
    void ThreadMethod()
    {
        while (!stop)
        {
            try
            {
                // Wait for client approval
                var clienttmp = listner.AcceptTcpClient();
                // We are connected
                print("Connected");
                Connected = 1;
                clients.Add(clienttmp);
                streamClient = clienttmp;
                streamthread.Start();
            }
            catch (Exception e)
            {
                print("" + e);
            }
        }
    }

    // stop everything
    private void OnApplicationQuit()
    {
        if (webCam != null)
        {
            webCam.Stop();
            stop = true;
            if (clients != null)
            {
                foreach (TcpClient c in clients)
                {
                    if (c != null)
                    {
                        print("in " + c.ToString());
                        c.GetStream().Close();
                        c.Close();
                    }
                }
            }
            if(listner!=null)
            listner.Stop();

            if (networkthread != null)
                networkthread.Abort();

            if (streamthread != null)
                streamthread.Abort();
        }
    }



}
