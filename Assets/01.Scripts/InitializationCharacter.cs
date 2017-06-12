using graduationWork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NamespaceCommandKinds;
using NamespaceConstKinds;
using MyNetWorkView;
using System.Threading;

public class InitializationCharacter {

    private static InitializationCharacter instance;

    Initialization init;
    HeroesNetWorkView netWork;

    public GameObject tofu;
    public GameObject mando;

    public GameObject[] PlayerArray;
    public bool isInitCharacter = false;

    public static InitializationCharacter GetInstance()
    {
        if (instance == null)
        {
            Debug.Log("InitializationCharacter 생성");
            instance = new InitializationCharacter();
            return instance;
        }
        else
        {
            return instance;
        }
    }
    private InitializationCharacter()
    {
        Awake();
        Start();
    }

    void Awake()
    {
        Debug.Log("InitializationCharacter Awake호출");
        netWork = HeroesNetWorkView.GetInstance();
        PlayerArray = new GameObject[ConstKind.EnterRoomPeopleLimit];
    }

    // Use this for initialization
    void Start () {
        Debug.Log("InitializationCharacter start");
        //StartCoroutine("ReadCheck");
        Thread Read = new Thread(ReadCheck);
        Read.Start();
    }

    void ReadCheck()
    {
        Debug.Log("클라이언트 번호 받는 중...");
        while (true)
        {
            if (-1 != netWork.MyClientNum)
            {
                //Debug.Log("나 클라이언트 번호 = " + netWork.MyClientNum);
                netWork.SendByteMessage(Command.EnterRoom, g_DataType.COMMAND); // 방 입장 명령
                netWork.SendByteMessage(Command.SelectMandu, g_DataType.COMMAND); // 내 캐릭 만두
                //netWork.SendByteMessage(Command.SelectTofu, g_DataType.COMMAND); // 내 캐릭 두부
                netWork.SendByteMessage(Command.TeamRed, g_DataType.COMMAND); // 내 팀 레드
                //netWork.SendByteMessage(Command.TeamBlue, g_DataType.COMMAND); // 내 팀 블루
                //StartCoroutine("CreateCharacter"); // 트랜스폼 코루틴 실행
                Thread ThCreateCharacter = new Thread(CreateCharacter);
                ThCreateCharacter.Start();
                break;
            }
            //yield return new WaitForSeconds(1.0f);
        }
        return;
    }

    void CreateCharacter()
    {
        Debug.Log("플레이어를 기다리는 중...");
        while (true)
        {
            if (netWork.isStartState)
            {
                init.instantiatePlayer(netWork.g_readySet.player1);
                init.instantiatePlayer(netWork.g_readySet.player2);
                init.instantiatePlayer(netWork.g_readySet.player3);
                init.instantiatePlayer(netWork.g_readySet.player4);
                isInitCharacter = true;
                //for(int i=0; i<3; i++)
                //{
                //    Debug.Log(i + " 번 tag = " + PlayerArray[i].tag);
                //}
                break;
            }
            else
            {
                netWork.SendByteMessage("start", g_DataType.COMMAND);
            }
        }
        return;
    }
}
