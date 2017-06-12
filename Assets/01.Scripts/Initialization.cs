using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyNetWorkView;
using NamespaceCommandKinds;
using NamespaceConstKinds;
using MyNetWorkView;
using graduationWork;

public class Initialization : MonoBehaviour {

    private static Initialization instance;
    HeroesNetWorkView netWork;
    InitializationCharacter initCharacter;
    MoveSynchronization moveSync;

    public static Initialization GetInstance()
    {
        if (instance == null)
        {
            Debug.Log("Initialization 생성");
            instance = GameObject.FindGameObjectWithTag("Init").GetComponent<Initialization>();
            return instance;
        }
        else
        {
            return instance;
        }
    }

    void Awake()
    {
        Debug.Log("Init Awake");
        netWork = HeroesNetWorkView.GetInstance();
        initCharacter = InitializationCharacter.GetInstance();
        moveSync = MoveSynchronization.GetInstance();
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}


    public void instantiatePlayer(g_CreateCharaterInfo player)
    {
        int pk = player.pkNum;// % ConstKind.EnterRoomPeopleLimit);
        int servant = player.servant;
        int team = player.team;

        GameObject obj = null;

        switch (servant)
        {
            case ConstKind.servantTofu:
                obj = Instantiate(initCharacter.tofu);
                break;
            case ConstKind.servantMando:
                obj = Instantiate(initCharacter.mando);
                break;
            case ConstKind.servantNone:
                //Debug.Log("Error 종족 받아오지 않았음!");
                return;
            default:
                //Debug.Log("Error 신규캐릭? 오류!!!!!!!!!!");
                return;
        }

        if (obj == null)
        {
            //Debug.Log("캐릭터 생성 실패");
            return;
        }
        obj.tag = pk.ToString();
        //PlayerList.Add(obj);
        //Debug.Log("PlayerArray.Length = " + PlayerArray.Length);
        initCharacter.PlayerArray[pk] = obj;
    }

}
