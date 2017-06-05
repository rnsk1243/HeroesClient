using graduationWork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MoveSynchronization : MonoBehaviour {

    GameObject InitCharacter;
    InitializationCharacter InitComponent;
    GameObject startNetWork;
    HeroesNetWorkView netWork;
    
    public static GameObject MyPlayer;

    Transform MyTr;
    
    //public struct TransformStruct
    //{
    //   public Vector3 newPosition;
    //   public Quaternion newRotation;
    //   public Vector3 newScale;
    //   public TransformStruct(Vector3 pos, Vector3 rot, Vector3 scal)
    //    {
    //        newPosition = new Vector3(pos.x, pos.y, pos.z);
    //        newRotation = Quaternion.Euler(rot.x, rot.y, rot.z);
    //        newScale = new Vector3(scal.x, scal.y, scal.z);
    //    }
    //}

    void Awake()
    {
        InitCharacter = GameObject.FindGameObjectWithTag("InitCharacter");
        InitComponent = InitCharacter.GetComponent<InitializationCharacter>();
        startNetWork = GameObject.FindGameObjectWithTag("StartNetWork");
        // MyClientNum = startNetWork.GetComponent<StartAsyncNetWork>().getMyClientNum();
        netWork = startNetWork.GetComponent<HeroesNetWorkView>();
    }

    // Use this for initialization
    void Start () {
        StartCoroutine("WaitCharactorInit");
    }

    IEnumerator WaitCharactorInit()
    {
        while (true)
        {
            if (InitComponent.isInitCharacter)
            {
                MyPlayer = InitComponent.PlayerArray[netWork.MyClientNum]; //GameObject.FindGameObjectWithTag(netWork.MyClientNum.ToString());
                MyPlayer.AddComponent<PlayerCtrl>();
                MyTr = MyPlayer.GetComponent<Transform>();
                StartCoroutine("MoveSyncSend");
                break;
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    IEnumerator MoveSyncSend()
    {
        while (true)
        {
            netWork.SendByteTransform(MyTr); // tr전송
            yield return new WaitForSeconds(1.7f);
        }
    }

    // Update is called once per frame
    void Update () {
        if (netWork.getIsNewTransform() && InitComponent.isInitCharacter)
        {
            Debug.Log("targetPK = " + netWork.getTargetPK() + " // newPosition = " + netWork.getNewPosition());
            InitComponent.PlayerArray[netWork.getTargetPK()].transform.position = netWork.getNewPosition();
            netWork.setFalseNewTransform();
        }

    }
}
