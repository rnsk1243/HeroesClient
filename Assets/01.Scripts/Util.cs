using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using graduationWork;

namespace CommandKinds
{
    public class Command
    {
       public const string SelectTofu = "servantTofu";
        public const string SelectMandu = "servantMandu";
        public const string EnterRoom = "EnterRoom";
        public const string TeamRed = "teamRed";
        public const string TeamBlue = "teamBlue";
        public const string StartButton = "start";
        public const string RequestDistinguishCode = "requestDistinguishCode";
    }
}

namespace ConstKinds
{
    public struct PostData
    {
        public int ClientNum;
        public g_DataType Type;
        public object data;
        public PostData(g_DataType type, object obj, int clientNum)
        {
            Type = type;
            data = obj;
            ClientNum = clientNum;
        }
    }

    

    enum ClientState
    {
        Connecting, // 연결 하는 중
        DistinguishCode, // 식별코드 받는 중
        SendMyCharacter, // 내가 할 캐릭 정하여 서버에 보내는 중
        RecvCharacter, // 서버로부터 어떤 캐릭터 생성해야하는지 받는 중
        AddComponent, // 내 캐릭터에 필요한 컴포턴트 붙임.
        GameStart // 준비완료
    }

    public class ConstKind
    {
        public const int StartClientPK = 5; // 시작전 클라이언트 번호
        public const int InitThreadSleepTime = 1000; // 초기화 쉬는 시간
        public const int RequestInitAmount = 50; // 이 횟수 이상 요청하면 문제가 발생으로 간주하고 스레드 죽임.
        public const int SendThreadSleepTime = 100; // millisecond
        public const int RecvThreadSleepTime = 100; // millisecond
        public const float MoveSyncSend_WaitForSeconds = 3.0f; // 위치를 queue에 Push 하고 쉬는 시간
        public const float CheckSendQueue_WaitForSeconds = 0.1f; // 여러 클래스의 큐를 돌면서 서버에 보낼 데이터를 Postbox에 넣고 쉬는 시간
        public const float CheckRecvQueue_WaitForSeconds = 0.1f; // 서버에서 받은 데이터를 Postbox에서 꺼내서 각 클래스에게 전달 하고 쉬는 시간

        public const int DataSizeBuf = 6; // DataSize를 받는데 필요한 크기
        public const int RecvBufferFlushSize = 16384; // 잘 못 받은 패킷 버리는데 필요한 버퍼 크기(넉넉히 잡아둠)
        // 접속할 곳의 IP주소.
        public const string address = "127.0.0.1";

        // 접속할 곳의 포트 번호.
        public const int port = 9000;
        public const int BufSize = 256;
        public const int servantTofu = 1; // 두부캐릭
        public const int servantMando = 2; // 만두캐릭
        public const int servantNone = 0; // 캐릭이 정해지지 않음
        public const int RedTeam = 1; // 레드 팀
        public const int BlueTeam = 2; // 블루 팀
        public const int NoneTeam = 0; // 팀이 정해지지 않음
        public const string TagInitializationCharacter = "InitCharacter";
        public const string TagMoveSynchronization = "MoveSynchronization";
    }
}

namespace NamespaceErrorHandler
{
    public static class ErrorHandler
    {
        public static void RecvBufferFlush(ref Socket Sock)
        {
            Debug.Log("RecvBuffer 비우기");
            byte[] tempBuf = new byte[ConstKinds.ConstKind.RecvBufferFlushSize];
            Sock.Receive(tempBuf);
        }
    }
}
