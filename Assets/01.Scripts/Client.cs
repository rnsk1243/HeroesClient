﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NamespacePostbox;
using NamespaceHeroesNetWorkView;
using graduationWork;
using ConstKinds;

public class Client : MonoBehaviour {

    private Postbox postbox;
    WaitForSeconds WaitOne = new WaitForSeconds(1);

    InitializationCharacter InitChara = InitializationCharacter.GetInstance;
    MoveSynchronization MoveSync = MoveSynchronization.GetInstance;

    void Awake()
    {
        postbox = Postbox.GetInstance;
        // 큐 탐색 시작
        StartCoroutine(CheckRecvQueue());
    }

    // Use this for initialization
    void Start ()
    {
		
	}
    // 모든 클래스를 주기적으로 탐색하여 // 서버에 보낼 데이터를 Postbox에 담는다.
    private IEnumerator CheckSendQueue()
    {
        PostData sendData = new PostData(g_DataType.NULLDATA, 0, -1);
        while (true)
        {
            if(InitChara.GetSendData(ref sendData))
            {
                postbox.PushSendData(sendData.Type, sendData.data, sendData.ClientNum);
            }
            if(MoveSync.GetSendData(ref sendData))
            {
                postbox.PushSendData(sendData.Type, sendData.data, sendData.ClientNum);
            }
            yield return WaitOne;
        }
    }
    

    //큐를 주기적으로 탐색 // 받은 데이터를 꺼내서 적재적소에 보내기
    private IEnumerator CheckRecvQueue()
    {
        //1초 주기로 탐색
        while (true)
        {
            PostData recvData = new PostData(g_DataType.NULLDATA, 0, -1);
            // 값 채우기
            // 받은 데이터 꺼내기
            if (!postbox.GetRecvData(ref recvData))
                yield return WaitOne;
            Debug.Log("받은 데이터 타입 = " + recvData.Type);
            if (recvData.Type != g_DataType.NULLDATA)
            {
                switch (recvData.Type)
                {
                    case g_DataType.COMMAND:
                        
                        break;
                    case g_DataType.MESSAGE:
                        if("AddComponent" == (string)recvData.data)
                        {
                            MoveSync.AddComponent(HeroesNetWorkView.MyClientNum);
                        }
                        break;
                    case g_DataType.PROTOCOL:
                        
                        break;
                    case g_DataType.READYSET:
                        InitChara.CreateCharacter((g_ReadySet)recvData.data);
                        break;
                    case g_DataType.TRANSFORM:
                        g_Transform gTr = (g_Transform)recvData.data;
                        MoveSync.MoveCharacter(recvData.ClientNum, ref gTr);
                        break;
                    default:
                        Debug.Log("처리되지 않은 case type = " + recvData.Type);
                        break;
                }
            }
            yield return WaitOne;
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
}