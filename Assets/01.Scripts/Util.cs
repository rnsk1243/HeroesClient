using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

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

    }
}

namespace ConstKinds
{
    public class ConstKind
    {
        public const int DataSizeBuf = 6; // DataSize를 받는데 필요한 크기
        public const int RecvBufferFlushSize = 8192; // 잘 못 받은 패킷 버리는데 필요한 버퍼 크기(넉넉히 잡아둠)
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
        public const int GoodTransformSize = 63; // Deserialize할 수 있는 크기
        public const int GoodRecvDataSize = 53;
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
