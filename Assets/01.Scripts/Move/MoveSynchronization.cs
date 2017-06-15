using graduationWork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NamespaceHeroesNetWorkView;
using ConstKinds;

public class MoveSynchronization : MonoBehaviour {

    //HeroesNetWorkView netWork;
    private static MoveSynchronization instance;
    //보낼 데이터를 담을 큐
    private Queue<PostData> SendQueue;
    //싱글턴 인스턴스 반환
    public static MoveSynchronization GetInstance
    {
        get
        {
            return instance;
        }
    }
    InitializationCharacter InitComponent;
    public static GameObject MyPlayer;

    Transform MyTr;
    Vector3 newPosition;
    Vector3 newRotation;
    Vector3 newScal;

    int targetPK = 0;
    bool isNewTransform = false;

    //서버에 보낼 데이터 큐에 담기
    private void PushSendData(g_DataType type, object obj, int clientNum = -1)
    {
        PostData pushData = new PostData(type, obj, clientNum);
        SendQueue.Enqueue(pushData);
    }

    //큐에있는 데이터 꺼내서 서버에 보냄
    public bool GetSendData(ref PostData sendData)
    {
        //데이타가 1개라도 있을 경우 꺼내서 반환
        if (SendQueue.Count > 0)
        {
            sendData = SendQueue.Dequeue();
            return true;
        }else
        {
            return false;
        }
    }

    void Awake()
    {
        instance = GameObject.FindGameObjectWithTag(ConstKind.TagMoveSynchronization).GetComponent<MoveSynchronization>();
        //큐 초기화
        SendQueue = new Queue<PostData>();
        InitComponent = InitializationCharacter.GetInstance;
        newPosition = new Vector3();
        newRotation = new Vector3();
        newScal = new Vector3();
    }

    // Use this for initialization
    void Start () {
        
    }

    public void AddComponent(int myClientNum)
    {
        MyPlayer = InitComponent.PlayerArray[myClientNum]; //GameObject.FindGameObjectWithTag(netWork.MyClientNum.ToString());
        MyPlayer.AddComponent<PlayerCtrl>();
        MyTr = MyPlayer.GetComponent<Transform>();
    }
    
    IEnumerator MoveSyncSend()
    {
        g_Transform sendTarget = new g_Transform();
        while (true)
        {
            copyTransformToG_Transform(ref sendTarget, ref MyTr);
            PushSendData(g_DataType.TRANSFORM, sendTarget); // tr전송
            yield return new WaitForSeconds(1.7f);
        }
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



    public void MoveCharacter(int pkNum, ref g_Transform source)
    {
        newPosition.x = source.position.x;
        newPosition.y = source.position.y;
        newPosition.z = source.position.z;

        newRotation.x = source.rotation.x;
        newRotation.y = source.rotation.y;
        newRotation.z = source.rotation.z;

        newScal.x = source.scale.x;
        newScal.y = source.scale.y;
        newScal.z = source.scale.z;
        targetPK = pkNum;
        isNewTransform = true;
    }

    // Update is called once per frame
    void Update () {

        if(isNewTransform)
        {
            InitComponent.PlayerArray[targetPK].transform.position = newPosition;
            isNewTransform = false;
        }
    }
}
