using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.IO;


[System.Serializable]
public class Address
{
    public int PlayerType;
    public string IPAddress;
    public string PortNo;// { get; set; }
    public string Width;// { get; set; }
    public string Height;// { get; set; }
    public int Webcam;// { get; set; }

}

public class CameraManager : MonoBehaviour {

    public static CameraManager instance;

    public Address CurrentAddress;

    public UnityEvent ConnectionMade;
    public UnityEvent ConnectionGone;


    private void Awake()
    {
        instance = this;
        string datatoget = File.ReadAllText(Application.dataPath + "/com.Rtools.livestream/data.txt");
        print("D : " + datatoget);
        CurrentAddress = JsonUtility.FromJson<Address>(datatoget);
        if (CurrentAddress.PlayerType == 0)
        {
            GetComponent<TCPSend>().enabled = true;
        }else
        {
            GetComponent<TCPReceive>().enabled = true;
        }
    }
    // Use this for initialization
    void Start () {


    }
	public void OnConnected()
    {
       
    }
    // Update is called once per frame
    void Update() {
       
	}
}
