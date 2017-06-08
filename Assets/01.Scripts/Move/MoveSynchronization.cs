using graduationWork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyNetWorkView;

public class MoveSynchronization : MonoBehaviour {

    GameObject InitCharacter;
    InitializationCharacter InitComponent;
    HeroesNetWorkView netWork;
    
    public static GameObject MyPlayer;

    Transform MyTr;
    Vector3 newPosition;
    Vector3 newRotation;
    Vector3 newScal;

    int targetPK = 0;
    bool isNewTransform = false;

    void Awake()
    {
        netWork = HeroesNetWorkView.GetInstance();
        InitCharacter = GameObject.FindGameObjectWithTag("InitCharacter");
        InitComponent = InitCharacter.GetComponent<InitializationCharacter>();
        newPosition = new Vector3();
        newRotation = new Vector3();
        newScal = new Vector3();
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

    public void copyTransformToG_Transform(ref g_Transform target, ref Transform source)
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
         //   Debug.Log("targetPK = " + targetPK + " // 위치 = " + newPosition);
            InitComponent.PlayerArray[targetPK].transform.position = newPosition;
            isNewTransform = false;
        }
    }
}
