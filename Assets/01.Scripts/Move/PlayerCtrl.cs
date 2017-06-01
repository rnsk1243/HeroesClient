using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;



public class PlayerCtrl : MonoBehaviour {

  

    private float h = 0.0f;
    private float v = 0.0f;

    private Transform tr;
    public float moveSpeed = 10.0f;

    //private Socket mSocket;
    // Use this for initialization
    void Awake () {
        tr = GetComponent<Transform>();
      
    }

    void Start()
    {
        //mSocket = startNetWork.GetComponent<StartAsyncNetWork>().getSocket();
        //if (mSocket == null)
        //    Debug.Log("소켓 널");

    }

    

    // Update is called once per frame
    void Update ()
    {
        //if (this.tag == MyClientNum.ToString())
        //{
        //    h = Input.GetAxis("Horizontal");
        //    v = Input.GetAxis("Vertical");

        //    Vector3 moveDir = (Vector3.forward * v) + (Vector3.right * h);
        //    tr.Translate(moveDir * Time.deltaTime * moveSpeed, Space.Self);
        //}

    }
}
