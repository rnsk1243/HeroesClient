﻿using graduationWork; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandKinds; 
using ConstKinds;
using NamespaceHeroesNetWorkView;

public class InitializationCharacter : MonoBehaviour {

    //HeroesNetWorkView netWork;
    private static InitializationCharacter instance;
    //보낼 데이터를 담을 큐
    private Queue<PostData> SendQueue;
    //싱글턴 인스턴스 반환
    public static InitializationCharacter GetInstance
    {
        get
        {
            return instance;
        }
    }

    public GameObject tofu;
    public GameObject mando;

    public GameObject[] PlayerArray;
    public bool isInitCharacter;

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
        instance = GameObject.FindGameObjectWithTag(ConstKind.TagInitializationCharacter).GetComponent<InitializationCharacter>();
        //큐 초기화
        SendQueue = new Queue<PostData>();
        // netWork = HeroesNetWorkView.GetInstance();
        PlayerArray = new GameObject[4];
        isInitCharacter = false;
    }

    // Use this for initialization
    void Start () {
        
    }


    // ui 구현되면 버튼 클릭시 호출되도록 만들 것.
    //public void SendMyCharacterFunc()
    //{
    //    Debug.Log("내가할 캐릭터 정보 보내기");
    //    PushSendData(g_DataType.COMMAND, Command.EnterRoom);
    //    PushSendData(g_DataType.COMMAND, Command.SelectTofu);
    //    PushSendData(g_DataType.COMMAND, Command.TeamBlue);
    //}

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

    public void CreateCharacter(g_ReadySet readySet)
    {
        instantiatePlayer(readySet.player1);
        instantiatePlayer(readySet.player2);
        instantiatePlayer(readySet.player3);
        instantiatePlayer(readySet.player4);
        isInitCharacter = true;
    }

}
