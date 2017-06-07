using graduationWork;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandKinds;
using ConstKinds;
using MyNetWorkView;

public class InitializationCharacter : MonoBehaviour {

   // GameObject startNetWork;
    HeroesNetWorkView netWork;

    public GameObject tofu;
    public GameObject mando;

    //public List<GameObject> PlayerList = new List<GameObject>();
    public GameObject[] PlayerArray;
    public bool isInitCharacter = false;

    void Awake()
    {
        //startNetWork = GameObject.FindGameObjectWithTag("StartNetWork");
        // MyClientNum = startNetWork.GetComponent<StartAsyncNetWork>().getMyClientNum();
        netWork = HeroesNetWorkView.GetInstance();
        PlayerArray = new GameObject[4];
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
                netWork.SendByteMessage(Command.EnterRoom, g_DataType.COMMAND); // 방 입장 명령
                netWork.SendByteMessage(Command.SelectMandu, g_DataType.COMMAND); // 내 캐릭 만두
                //netWork.SendByteMessage(Command.SelectTofu, g_DataType.COMMAND); // 내 캐릭 두부
                netWork.SendByteMessage(Command.TeamRed, g_DataType.COMMAND); // 내 팀 레드
                //netWork.SendByteMessage(Command.TeamBlue, g_DataType.COMMAND); // 내 팀 블루
                StartCoroutine("CreateCharacter"); // 트랜스폼 코루틴 실행
                break;
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    void instantiatePlayer(g_CreateCharaterInfo player)
    {
        int pk = player.pkNum;
        int servant = player.servant;
        int team = player.team;

        GameObject obj = null;

        switch(servant)
        {
            case ConstKind.servantTofu:
                obj = Instantiate(tofu);
                break;
            case ConstKind.servantMando:
                obj = Instantiate(mando);
                break;
            case ConstKind.servantNone:
                Debug.Log("Error 종족 받아오지 않았음!");
                return;
            default:
                Debug.Log("Error 신규캐릭? 오류!!!!!!!!!!");
                return;
        }

        if(obj == null)
        {
            Debug.Log("캐릭터 생성 실패");
            return;
        }
        obj.tag = pk.ToString();
        //PlayerList.Add(obj);
        PlayerArray[pk] = obj;
    }

    IEnumerator CreateCharacter()
    {
        Debug.Log("플레이어를 기다리는 중...");
        while (true)
        {
            if (netWork.isStartState)
            {
                instantiatePlayer(netWork.g_readySet.player1);
                instantiatePlayer(netWork.g_readySet.player2);
                instantiatePlayer(netWork.g_readySet.player3);
                instantiatePlayer(netWork.g_readySet.player4);
                isInitCharacter = true;
                for(int i=0; i<3; i++)
                {
                    Debug.Log(i + " 번 tag = " + PlayerArray[i].tag);
                }
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
