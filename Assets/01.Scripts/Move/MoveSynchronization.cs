using graduationWork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NamespaceHeroesNetWorkView;
using NamespaceConstKinds;
using NamespaceMathCalc;

public class MoveSynchronization : MonoBehaviour {

    //WaitForSeconds WaitMoveSyncSend = new WaitForSeconds(ConstKind.MoveSyncSend_WaitForSeconds);
    //HeroesNetWorkView netWork;
    //보낼 데이터를 담을 큐
    private Queue<PostData> SendQueue;
    private static MoveSynchronization instance;
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
    //g_Transform[,] recvTransformArray;
    //Vector3[] moveSpline;

    //서버에 보낼 데이터 큐에 담기
    private void PushSendData(g_DataType type, object obj)
    {
        PostData pushData = new PostData(type, obj, HeroesNetWorkView.MyClientNum);
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
        //StartCoroutine(MoveSyncSend());
    }
    
 //   IEnumerator MoveSyncSend()
 //   {
 ////       Debug.Log("MoveSyncSend 코루틴 실행");
 //       g_Transform sendTarget = new g_Transform();
 //       while (true)
 //       {
 //           copyTransformToG_Transform(ref sendTarget, ref MyTr);
 //           PushSendData(g_DataType.TRANSFORM, sendTarget); // tr전송
 //           yield return WaitMoveSyncSend;
 //       }
 //   }

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

    // 보간 처리 후 캐릭터 이동
    //public void MoveCharacter(int clientNum)
    //{
    //    if (isNewTransform)
    //    {
    //        moveSpline = MathCalc.InterpolationCoordinate(clientNum, recvTransformArray);
    //        GameObject targetClient = InitComponent.PlayerArray[clientNum];
    //        for (int i = ConstKind.InterpolationResultCoordinateNum; i > 0; i--)
    //        {
    //            targetClient.transform.position =
    //            Vector3.MoveTowards(InitComponent.PlayerArray[clientNum].transform.position, moveSpline[i], 10.0f * Time.deltaTime);
    //        }
    //        isNewTransform = false;
    //    }
    //}

    // Update is called once per frame
    void Update () {

        if(isNewTransform)
        {
            InitComponent.PlayerArray[targetPK].transform.position = newPosition;
            isNewTransform = false;
        }
    }
}
