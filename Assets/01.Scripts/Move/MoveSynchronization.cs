using graduationWork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSynchronization : MonoBehaviour {

    GameObject startNetWork;
    StartAsyncNetWork netWork;

    public GameObject[] Players;

    void Awake()
    {
        startNetWork = GameObject.FindGameObjectWithTag("StartNetWork");
        // MyClientNum = startNetWork.GetComponent<StartAsyncNetWork>().getMyClientNum();
        netWork = startNetWork.GetComponent<StartAsyncNetWork>();
    }

    // Use this for initialization
    void Start () {
        StartCoroutine("ReadCheck");
    }

    IEnumerator ReadCheck()
    {
        Debug.Log("클라이언트 번호 받는 중...");
        while (true)
        {
            if (-1 != netWork.MyClientNum)
            {
                Debug.Log("나 클라이언트 번호 = " + netWork.MyClientNum);
                netWork.SendByteMessage("servantTofu", g_DataType.COMMAND); // 내 캐릭종류
                //netWork.SendByteMessage("teamRed", g_DataType.COMMAND); // 내 팀 레드
                netWork.SendByteMessage("teamBlue", g_DataType.COMMAND); // 내 팀 블루
                netWork.SendByteMessage("EnterRoom", g_DataType.COMMAND); // 방 입장 명령
                StartCoroutine("CreateCharacter"); // 트랜스폼 코루틴 실행
                break;
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    void printPlayerInfo(g_CreateCharaterInfo player)
    {
        Debug.Log("pk = " + player.pkNum);
        Debug.Log("servant = " + player.servant);
        Debug.Log("team = " + player.team);
    }

    IEnumerator CreateCharacter()
    {
        Debug.Log("플레이어를 기다리는 중...");
        while (true)
        {
            if (netWork.isStartState)
            {
                Debug.Log("player1 정보");
                printPlayerInfo(netWork.g_readySet.player1);
                Debug.Log("player2 정보");
                printPlayerInfo(netWork.g_readySet.player2);
                Debug.Log("player3 정보");
                printPlayerInfo(netWork.g_readySet.player3);
                Debug.Log("player4 정보");
                printPlayerInfo(netWork.g_readySet.player4);
                //Debug.Log("sendThread 시작");
                //while (true)
                //{
                //    //if (mSocket == null)
                //    //    Debug.Log("소켓 널");
                //    //Debug.Log("ggggg");

                //   // netWork.SendByteTransform(tr);
                //    yield return new WaitForSeconds(1.7f);
                //}
                break;
            }
            else
            {
                netWork.SendByteMessage("start", g_DataType.COMMAND);
            }
            yield return new WaitForSeconds(3.0f);
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
}
