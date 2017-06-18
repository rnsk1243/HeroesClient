using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using NamespaceConstKinds;
using graduationWork;
using NamespaceHeroesNetWorkView;

public class PlayerCtrl : MonoBehaviour {

    WaitForSeconds WaitCheckMoveSendTr = new WaitForSeconds(ConstKind.CheckMoveSendTr_WaitForSeconds);
    private float h = 0.0f;
    private float v = 0.0f;
    //보낼 데이터를 담을 큐
    private Queue<PostData> SendMyTrQueue;
    private Transform tr;
    public float moveSpeed = ConstKind.MoveSpeed;

    private static PlayerCtrl instance;
    //싱글턴 인스턴스 반환
    public static PlayerCtrl GetInstance
    {
        get
        {
            return instance;
        }
    }

    //서버에 보낼 데이터 큐에 담기
    private void PushSendData(g_DataType type, object obj)
    {
        PostData pushData = new PostData(type, obj, HeroesNetWorkView.MyClientNum);
        SendMyTrQueue.Enqueue(pushData);
    }

    //큐에있는 데이터 꺼내서 서버에 보냄
    public bool GetSendData(ref PostData sendData)
    {
        //데이타가 1개라도 있을 경우 꺼내서 반환
        if (SendMyTrQueue.Count > 0)
        {
            sendData = SendMyTrQueue.Dequeue();
            return true;
        }
        else
        {
            return false;
        }
    }


    //private Socket mSocket;
    // Use this for initialization
    void Awake () {
        instance = MoveSynchronization.MyPlayer.GetComponent<PlayerCtrl>(); //GameObject.FindGameObjectWithTag(ConstKind.TagPlayerCtrl).GetComponent<PlayerCtrl>();
      tr = GetComponent<Transform>();
        //큐 초기화
        SendMyTrQueue = new Queue<PostData>();
    }

    void Start()
    {
        StartCoroutine(CheckMoveSendTr());
    }

    void copyTransformToG_Transform(ref g_Transform target, ref Transform source)
    {
        target.position.x = source.position.x;
        target.position.y = source.position.y;
        target.position.z = source.position.z;
        target.rotation.x = source.rotation.x;
        target.rotation.y = source.rotation.y;
        target.rotation.z = source.rotation.z;
        target.scale.x = source.localScale.x;
        target.scale.y = source.localScale.y;
        target.scale.z = source.localScale.z;
    }

    IEnumerator CheckMoveSendTr()
    {
        float deltaV = 0.0f;
        float deltaH = 0.0f;
        g_Transform sendTarget = new g_Transform();
        while (true)
        {
            deltaV = Input.GetAxis("Vertical");
            deltaH = Input.GetAxis("Horizontal");
            //Debug.Log("CheckMoveSendTr호출 deltaV = " + deltaV + " // H = " + deltaH);
            if (ConstKind.DeltaPositionSend < deltaV || deltaV < -ConstKind.DeltaPositionSend || 
                ConstKind.DeltaPositionSend < deltaH || deltaH < -ConstKind.DeltaPositionSend)
            {
                //Debug.Log("tr queue에 담음");
                copyTransformToG_Transform(ref sendTarget, ref tr);
                PushSendData(g_DataType.TRANSFORM, sendTarget); // tr전송
            }
            yield return WaitCheckMoveSendTr;
        }
    }

    // Update is called once per frame
    void Update ()
    {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        Vector3 moveDir = (Vector3.forward * v) + (Vector3.right * h);
        
        tr.Translate(moveDir * Time.deltaTime * moveSpeed, Space.Self);
    }
}
