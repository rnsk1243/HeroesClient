using graduationWork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyNetWorkView;
using NamespaceConstKinds;
using NamespaceMathCalc;
using System.Threading;

public class MoveSynchronization {

    private static MoveSynchronization instance;
    InitializationCharacter InitComponent;
    HeroesNetWorkView netWork;
    
    GameObject MyPlayer;
    public g_Transform[,] recvTransformArray;

    Transform MyTr;
    //Vector3 newPosition;
    //Vector3 newRotation;
    //Vector3 newScal; 

    //int targetPK = 0;
    public bool isNewTransform = false;
    bool isSendTrStart = false;
    PlayerCtrl playerCtrl;
    Vector3[] moveSpline;

    public static MoveSynchronization GetInstance()
    {
        if (instance == null)
        {
            Debug.Log("MoveSynchronization 생성");
            instance = new MoveSynchronization();
            return instance;
        }
        else
        {
            return instance;
        }
    }

    private MoveSynchronization()
    {
        Debug.Log("MoveSynchronization생성자");
        Awake();
        Start();
    }

    void Awake()
    {
        netWork = HeroesNetWorkView.GetInstance();
        InitComponent = InitializationCharacter.GetInstance();
        recvTransformArray = new g_Transform[ConstKind.EnterRoomPeopleLimit, ConstKind.InterpolationCoordinateNum];
        
        //newPosition = new Vector3();
        //newRotation = new Vector3();
        //newScal = new Vector3();
        moveSpline = new Vector3[ConstKind.InterpolationResultCoordinateNum];
    }

    // Use this for initialization
    void Start () {
        Debug.Log("MoveSynchronization Start");
        while (true)
        {
            if (InitComponent.isInitCharacter)
            {
                g_Transform g_TrTemp = new g_Transform();
                for (int i = 0; i < ConstKind.EnterRoomPeopleLimit; i++)
                {
                    copyTransformToG_Transform(ref g_TrTemp, InitComponent.PlayerArray[i].transform);
                    recvTransformArray[i, 0] = g_TrTemp;
                    recvTransformArray[i, 1] = g_TrTemp;
                    recvTransformArray[i, 2] = g_TrTemp;
                    recvTransformArray[i, 3] = g_TrTemp;
                }
                MyPlayer = InitComponent.PlayerArray[netWork.MyClientNum]; //GameObject.FindGameObjectWithTag(netWork.MyClientNum.ToString());
                MyPlayer.AddComponent<PlayerCtrl>();
                MyTr = MyPlayer.GetComponent<Transform>();
                playerCtrl = MyPlayer.GetComponent<PlayerCtrl>();
                isSendTrStart = true;
                break;
            }
        }
       // instance = 
        Thread SendThread = new Thread(MoveSyncSend);
        SendThread.Start();
    }

    
    
    public void MoveSyncSend()
    {
        Debug.Log("MoveSyncSend 호출");
        while (true)
        {
            if(playerCtrl.isDeltaPosition)
            {
                netWork.SendByteTransform(MyTr); // tr전송
            }
        }
    }

    public void copyTransformToG_Transform(ref g_Transform target, Transform source)
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

    // 보간 처리 후 캐릭터 이동
    public void MoveCharacter(int clientNum)
    {
        if(isNewTransform)
        {
            moveSpline = MathCalc.InterpolationCoordinate(clientNum, recvTransformArray);
            GameObject targetClient = InitComponent.PlayerArray[clientNum];
            for (int i= ConstKind.InterpolationResultCoordinateNum; i>0; i--)
            {
                targetClient.transform.position =
                Vector3.MoveTowards(InitComponent.PlayerArray[clientNum].transform.position, moveSpline[i], 10.0f * Time.deltaTime);
            }
            isNewTransform = false;
        }
    }
}

//public class MyMain
//{
//    static void Main()
//    {
//        MoveSynchronization MoveSynchroni = MoveSynchronization.GetInstance();
//        Thread SendThread = new Thread(MoveSynchroni.MoveSyncSend);
//    }
//}
