using graduationWork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NamespaceHeroesNetWorkView;
using NamespaceConstKinds;
using NamespaceMathCalc;
using NamespaceUseful;

public class MoveSynchronization : MonoBehaviour {

    WaitForSeconds WaitMove = new WaitForSeconds(ConstKind.Move_WaitForSeconds);
    WaitForSeconds WaitMove2 = new WaitForSeconds(0.01f);
    //HeroesNetWorkView netWork;
    //보낼 데이터를 담을 큐
    private Queue<PostData> SendRemotTrQueue;
    //받은 데이터를 담을 큐
    private Queue<PostData>[] RecvRemotTrQueue;
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
        SendRemotTrQueue.Enqueue(pushData);
    }

    //큐에있는 데이터 꺼내서 서버에 보냄
    public bool GetSendData(ref PostData sendData)
    {
        //데이타가 1개라도 있을 경우 꺼내서 반환
        if (SendRemotTrQueue.Count > 0)
        {
            sendData = SendRemotTrQueue.Dequeue();
            return true;
        }else
        {
            return false;
        }
    }

    /// <summary>
    // 서버에서 받은 데이터 큐에 담기
    public void PushRecvData(g_DataType type, object obj, int clientNum = ConstKind.EnterRoomPeopleLimit)
    {
        PostData pushData = new PostData(type, obj, clientNum);
        //Debug.Log("담을 큐 인덱스 = " + clientNum + " // " + RecvRemotTrQueue.Length);
        //Debug.Log("clientNum = " + pushData.ClientNum + " // type = " + pushData.Type + " // x = " + ((g_Transform)pushData.data).position.x);
        RecvRemotTrQueue[clientNum].Enqueue(pushData);
        //StartCoroutine(Move(clientNum));
        //Debug.Log("Push 끝~~~");
    }

    // 서버에서 받은 데이터 큐에서 꺼내기
    private bool GetRecvData(ref g_Transform[] recvData, int clientNum)
    {
        //데이타가 2개 이상일경우
        if (isGetRecvData(clientNum))
        {
            for(int i=0; i< 2; i++)
            {
                recvData[i] = (g_Transform)RecvRemotTrQueue[clientNum].Dequeue().data;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool isGetRecvData(int clientNum)
    {
        if(RecvRemotTrQueue[clientNum].Count > 1)
        {
            return true;
        }else
        {
            return false;
        }
    }
    /// </summary>

    void Awake()
    {
        instance = GameObject.FindGameObjectWithTag(ConstKind.TagMoveSynchronization).GetComponent<MoveSynchronization>();
        //큐 초기화
        SendRemotTrQueue = new Queue<PostData>();
        RecvRemotTrQueue = new Queue<PostData>[ConstKind.EnterRoomPeopleLimit];
        for (int i=0; i<4; i++)
        {
            RecvRemotTrQueue[i] = new Queue<PostData>();
        }
        
        //Debug.Log("======================" + RecvRemotTrQueue.Length);
        InitComponent = InitializationCharacter.GetInstance;
        newPosition = new Vector3();
        newRotation = new Vector3();
        newScal = new Vector3();
    }

    // Use this for initialization
    void Start()
    {
        for(int i=0; i<4; i++)
        {
            if(i != HeroesNetWorkView.MyClientNum)
            {
                StartCoroutine(Move(i));
            }
        }
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

    //public void MoveCharacter(int pkNum, ref g_Transform source)
    //{
    //    newPosition.x = source.position.x;
    //    newPosition.y = source.position.y;
    //    newPosition.z = source.position.z;

    //    newRotation.x = source.rotation.x;
    //    newRotation.y = source.rotation.y;
    //    newRotation.z = source.rotation.z;

    //    newScal.x = source.scale.x;
    //    newScal.y = source.scale.y;
    //    newScal.z = source.scale.z;
    //    targetPK = pkNum;
    //    isNewTransform = true;
    //}

    //Vector3[] GetMoveSpline(int clientNum)
    //{
    //    g_Transform[] recvTransformArray = new g_Transform[2];
    //    Vector3[] moveSpline = new Vector3[ConstKind.MiddleResultCoordinateNum + 2];
    //    if (GetRecvData(ref recvTransformArray, clientNum))
    //    {
    //        //Debug.Log("Move 시작");
    //        float deltaX = recvTransformArray[1].position.x - recvTransformArray[0].position.x;
    //        float deltaY = recvTransformArray[1].position.y - recvTransformArray[0].position.y;
    //        float deltaZ = recvTransformArray[1].position.z - recvTransformArray[0].position.z;
    //        Vector3 deltaVec = new Vector3(deltaX, deltaY, deltaZ);
    //        float magnitude = deltaVec.magnitude;
    //        //Debug.Log("시간 = " + ((ConstKind.MoveSpeed * Time.deltaTime) - magnitude));
    //        Vector3 normal = deltaVec.normalized;
    //        float deltaValue = magnitude / (ConstKind.MiddleResultCoordinateNum + 1);
    //        Vector3 normaldelta = normal * deltaValue;
    //        //균등하게 나누어서 증감값을 구함
    //        //float InterpolationAmountX = (deltaX) / (ConstKind.MiddleResultCoordinateNum + 1);
    //        //float InterpolationAmountY = (deltaY) / (ConstKind.MiddleResultCoordinateNum + 1);
    //        //float InterpolationAmountZ = (deltaZ) / (ConstKind.MiddleResultCoordinateNum + 1);
    //        Vector3 recvTransformArray0 = Useful.getVector3(ref recvTransformArray[0], ConstKind.Transform.Position);
    //        for (int i = 0; i < (ConstKind.MiddleResultCoordinateNum + 2); i++)
    //        {
    //            moveSpline[i] = recvTransformArray0 + (normaldelta * i);
    //            //moveSpline[i].x = recvTransformArray[0].position.x + (normal.x * i);
    //            //moveSpline[i].y = recvTransformArray[0].position.y + (normal.y * i);
    //            //moveSpline[i].z = recvTransformArray[0].position.z + (normal.z * i);
    //        }
    //        return moveSpline;
    //        //if (magnitude < (ConstKind.MoveSpeed * Time.deltaTime))
    //        //{
    //        //    //Debug.Log("목표 거리가 너무 가까움");
    //        //    for (int i = 0; i < (ConstKind.MiddleResultCoordinateNum + 2); i++)
    //        //    {
    //        //        moveSpline[i].x = recvTransformArray[1].position.x;
    //        //        moveSpline[i].y = recvTransformArray[1].position.y;
    //        //        moveSpline[i].z = recvTransformArray[1].position.z;
    //        //    }
    //        //    return moveSpline;
    //        //}
    //        //else
    //        //{
    //        //    Vector3 normal = deltaVec.normalized;
    //        //    float deltaValue = magnitude / (ConstKind.MiddleResultCoordinateNum + 1);
    //        //    Vector3 normaldelta = normal * deltaValue;
    //        //    //균등하게 나누어서 증감값을 구함
    //        //    //float InterpolationAmountX = (deltaX) / (ConstKind.MiddleResultCoordinateNum + 1);
    //        //    //float InterpolationAmountY = (deltaY) / (ConstKind.MiddleResultCoordinateNum + 1);
    //        //    //float InterpolationAmountZ = (deltaZ) / (ConstKind.MiddleResultCoordinateNum + 1);
    //        //    Vector3 recvTransformArray0 = Useful.getVector3(ref recvTransformArray[0], ConstKind.Transform.Position);
    //        //    for (int i = 0; i < (ConstKind.MiddleResultCoordinateNum + 2); i++)
    //        //    {
    //        //        moveSpline[i] = recvTransformArray0 + (normaldelta * i);
    //        //        //moveSpline[i].x = recvTransformArray[0].position.x + (normal.x * i);
    //        //        //moveSpline[i].y = recvTransformArray[0].position.y + (normal.y * i);
    //        //        //moveSpline[i].z = recvTransformArray[0].position.z + (normal.z * i);
    //        //    }
    //        //    return moveSpline;
    //        //}
    //    }
    //    else
    //    {
    //        return null;
    //    }
    //}

    //보간 처리 후 캐릭터 이동
    IEnumerator Move(int clientNum)
    {
        g_Transform[] recvTransformArray = new g_Transform[2];
        Vector3[] moveSpline = new Vector3[ConstKind.MiddleResultCoordinateNum + 2];
        bool isMoveSyncStart1= false;
        bool isMoveSyncStart2 = false;
        while (true)
        {
            isMoveSyncStart1 = (3 < RecvRemotTrQueue[clientNum].Count);
            if (isMoveSyncStart2 || isMoveSyncStart1)
            {
                if (GetRecvData(ref recvTransformArray, clientNum))
                {
                    float deltaX = recvTransformArray[1].position.x - recvTransformArray[0].position.x;
                    float deltaY = recvTransformArray[1].position.y - recvTransformArray[0].position.y;
                    float deltaZ = recvTransformArray[1].position.z - recvTransformArray[0].position.z;
                    Vector3 deltaVec = new Vector3(deltaX, deltaY, deltaZ);
                    float magnitude = deltaVec.magnitude;
                    Vector3 normal = deltaVec.normalized;
                    float deltaValue = magnitude / (ConstKind.MiddleResultCoordinateNum + 1);
                    Vector3 normaldelta = normal * deltaValue;
                    Vector3 recvTransformArray0 = Useful.getVector3(ref recvTransformArray[0], ConstKind.Transform.Position);
                    for (int i = 0; i < (ConstKind.MiddleResultCoordinateNum + 2); i++)
                    {
                        moveSpline[i] = recvTransformArray0 + (normaldelta * i);
                    }
                    GameObject targetClient = InitComponent.PlayerArray[clientNum];
                    for (int i = 0; i < (ConstKind.MiddleResultCoordinateNum + 2); i++)
                    {
                        targetClient.transform.position = moveSpline[i];
                        yield return WaitMove;
                    }
                }
                isMoveSyncStart2 = (1 < RecvRemotTrQueue[clientNum].Count);
            }
            else
            {
                //Debug.Log("count = " + RecvRemotTrQueue[clientNum].Count);
                yield return WaitMove2;
                //Debug.Log("yield 끝");
            }
        }
    }

    // Update is called once per frame
    //void Update()
    //{

    //}
}
