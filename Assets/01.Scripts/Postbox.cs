using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using graduationWork;
using ProtoBuf;
using System.Collections;
using NamespaceConstKinds;
using NamespaceHeroesNetWorkView;
using UnityEngine;

namespace NamespacePostbox
{
    class Postbox
    {
        //싱글턴 인스턴스
        private static Postbox instance;
        //싱글턴 인스턴스 반환
        public static Postbox GetInstance
        {
            get
            {
                if (instance == null)
                    instance = new Postbox();

                return instance;
            }
        }

        //보낼 데이터를 담을 큐
        private Queue<PostData> SendQueue;
        //받은 데이터를 담을 큐
        private Queue<PostData> RecvQueue;

        private Postbox()
        {   //큐 초기화
            SendQueue = new Queue<PostData>();
            RecvQueue = new Queue<PostData>();
        }

        //서버에 보낼 데이터 큐에 담기
        public void PushSendData(g_DataType type, object obj)
        {
  //          Debug.Log("중요!!!!!!!!! = " + HeroesNetWorkView.MyClientNum);
            PostData pushData = new PostData(type, obj, HeroesNetWorkView.MyClientNum);
            SendQueue.Enqueue(pushData);
        }

        //큐에있는 데이터 꺼내서 서버에 보냄
        public bool GetSendData(ref PostData sendData)
        {
            //데이타가 1개라도 있을 경우 꺼내서 반환
            if (SendQueue.Count > 0)
            {
                sendData = SendQueue.Dequeue();
 //               Debug.Log("서버에게 보낼 데이터 가져옴 = " + sendData.Type + " // " + sendData.data);
                return true;
            }else
            {
 //               Debug.Log("서버에게 보낼 테이터 없다");
                return false;
            }
        }

        // 서버에서 받은 데이터 큐에 담기
        public void PushRecvData(g_DataType type, object obj, int clientNum = -1)
        {
            PostData pushData = new PostData(g_DataType.NULLDATA, obj, clientNum);
            pushData.Type = type;
            RecvQueue.Enqueue(pushData);
        }

        // 서버에서 받은 데이터 큐에서 꺼내기
        public bool GetRecvData(ref PostData recvData)
        {
            //데이타가 1개라도 있을 경우 꺼내서 반환
            if (RecvQueue.Count > 0)
            {
                recvData = RecvQueue.Dequeue();
                return true;
            }else
            {
                return false;
            }
        }
    }
}
