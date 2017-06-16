using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.IO;
using graduationWork;
using ProtoBuf;
using System.Collections;
using ConstKinds;
using NamespaceErrorHandler;
using System.Threading;
using NamespacePostbox;
using CommandKinds;

namespace NamespaceHeroesNetWorkView
{
    class HeroesNetWorkView
    {
        private static HeroesNetWorkView instance;
        public static int MyClientNum = ConstKind.StartClientPK; // 클라이언트 간의 구분 번호
        private byte[] recvBuffer; // 받을 버퍼
        bool isRecvSizeState = true; // g_DataSize을 받을 상태인가?
        g_DataSize recvDataSize; // 누가, 얼마나, 어떤 타입을 보내는지 미리 받을 곳
        g_DataSize sendDataSize; // send하기 전에 미리 내가 얼마나, 어떤 타입을 보낼지 넣어두는 곳
        g_ReadySet g_readySet; // 준비된 플레이어 정보 받는 곳(종족, 팀정보, 플레이어 번호)
        g_Message g_message; // 메세지 받는 곳 
        g_Transform g_transform; //트렌스폼 받는 곳v족, 팀정보, 플레이어 번호)
        g_Message g_command;
        private Socket clientSock;  /* client Socket */
        private Thread initThread;
        private Thread recvThread;
        private Thread sendThread;
        private Postbox postbox;    //메세지 큐를 관리하는 우편함
        int CurRecvDataSize;
        // 클라이언트 상태
        private static ClientState State;
        int InitRequestAmount = 0; // 요청 횟수
        //싱글턴 인스턴스 반환
        public static HeroesNetWorkView GetInstance
        {
            get
            {
                if (instance == null)
                    instance = new HeroesNetWorkView();

                return instance;
            }
        }

        private HeroesNetWorkView()
        {
            Debug.Log("HeroesNetWorkView 초기화");
            SetClientState(ClientState.Connecting);
            postbox = Postbox.GetInstance;
            CurRecvDataSize = 0;
            recvBuffer = new byte[ConstKind.BufSize];
            sendDataSize = new g_DataSize();
            initThread = new Thread(new ThreadStart(init));
            recvThread = new Thread(new ThreadStart(recvnThreadWork));            
            sendThread = new Thread(new ThreadStart(sendnThreadWork));
            initThread.Start();
        }

        private void SetClientState(ClientState newState)
        {
            State = newState;
            InitRequestAmount = 0; // 상태가 바뀌었으므로 요청횟수 초기화
        }
        public static ClientState GetCurrentState()
        {
            return State;
        }

        private void init()
        {
            Debug.Log("NetWorkView초기화 시작");
            try
            {
//                Debug.Log("Init()");
                while(true)
                {
                    switch (State)
                    {
                        case ClientState.Connecting:
                            Debug.Log("시작");
                            Start();
                            break;
                        case ClientState.DistinguishCode:
                            Debug.Log("구분 번호 받는 중...");
                            SetDistinguishCode();
                            break;
                        case ClientState.SendMyCharacter:
                            Debug.Log("내가할 캐릭터 정보 보내기");
                            postbox.PushSendData(g_DataType.COMMAND, Command.EnterRoom);
                            //postbox.PushSendData(g_DataType.COMMAND, Command.SelectTofu);
                            //postbox.PushSendData(g_DataType.COMMAND, Command.TeamBlue);
                            postbox.PushSendData(g_DataType.COMMAND, Command.SelectMandu);
                            postbox.PushSendData(g_DataType.COMMAND, Command.TeamRed);
                            SetClientState(ClientState.RecvCharacter);
                            break;
                        case ClientState.RecvCharacter:
                            Debug.Log("Start 보내는중");
                            postbox.PushSendData(g_DataType.COMMAND, Command.StartButton);
                            break;
                       case ClientState.AddComponent:
                            Debug.Log("AddComponent");
                            postbox.PushRecvData(g_DataType.MESSAGE, "AddComponent");
                            SetClientState(ClientState.GameStart);
                            break;
                        case ClientState.GameStart:
                            Debug.Log("준비 완료");
                            break;
                    }
                    if (State == ClientState.GameStart)
                    {
                        break;
                    }
                    InitRequestAmount++;
                    if (InitRequestAmount > ConstKind.RequestInitAmount)
                    {
                        Debug.Log("오류 = 요청횟수 초과로 인해 접속을 종료합니다. 최종 상태 = " + State);
                        initThread.Abort();
                    }
                    if(State != ClientState.RecvCharacter)
                    {
                        Thread.Sleep(ConstKind.InitThreadSleepTime);
                    }
                    else
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception ep)
            {
                Debug.Log(ep.Message);
                ErrorReset(ref clientSock);
            }
            Debug.Log("스레드 종료!");
        }
        #region 연결 관련

        void Start()
        {
            DoInit();
            while (true)
            {
                if (clientSock.Connected)
                {
                    SetClientState(ClientState.DistinguishCode);
                    Request();// 스레드 시작
                    break;
                }
            }
        }
        private void DoInit()
        {
            Debug.Log("DoInit 호출");
            clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.BeginConnect();
        }

        private void BeginConnect()
        {
            try
            {
                clientSock.BeginConnect(ConstKind.address, ConstKind.port, new AsyncCallback(ConnectCallBack), clientSock);
            }
            catch (SocketException se)
            {
                /*서버 접속 실패 */
                Debug.Log("서버접속 실패하였습니다. " + se.NativeErrorCode);
                this.DoInit();
            }
        }

        private void ConnectCallBack(IAsyncResult IAR)
        {
            try
            {
                // 보류중인 연결을 완성
                Socket tempSock = (Socket)IAR.AsyncState;
                IPEndPoint svrEP = (IPEndPoint)tempSock.RemoteEndPoint;
                Debug.Log("서버로 접속 성공 : " + svrEP.Address);
                tempSock.EndConnect(IAR);
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.NotConnected)
                {
                    Debug.Log("서버 접속 실패 CallBack " + se.Message);
                    this.BeginConnect();
                }
            }
        }
        // 서버에 요청(스레드 시작)
        private void Request()
        {
            Debug.Log("스레드 시작");
            recvThread.Start();
            sendThread.Start();
            
        }

        #endregion


        // 내 구분 코드 요청과 받기
        void SetDistinguishCode()
        {
            postbox.PushSendData(g_DataType.PROTOCOL, CommandKinds.Command.RequestDistinguishCode);
        }

        // 받은 크기만큼 버퍼를 딱 맞춤.
        private byte[] ReSizeBuffer(ref byte[] sourceBuffer, int reSize)
        {
            byte[] newBuffer = new byte[reSize];
            Array.Copy(sourceBuffer, newBuffer, reSize);
            return newBuffer;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #region 직접 호출 금지 Send 관련
        private void SendByteSize(int clientNum, int size, g_DataType type)
        {
            try
            {
                /* 연결 성공시 */
                if (clientSock.Connected)
                {
                    int curSendSize = 0;
                    int sendSize = 0;
                    sendDataSize.clientNum = clientNum;
                    sendDataSize.size = size;
                    //Debug.Log("서버가 받아야하는 크기값 = " + sendDataSize.size);
                    sendDataSize.type = type;
                    //Debug.Log("clientNum = " + sendDataSize.clientNum);
                    //if(sendDataSize.clientNum < 0 )
                    //{
                    //    Debug.Log("clientNum -------------" + sendDataSize.clientNum);
                    //}
                    MemoryStream sendMS = new MemoryStream();
                    Serializer.Serialize(sendMS, sendDataSize); // MemoryStream sendMS에 Serialize값 담기
                    byte[] buffer = sendMS.ToArray(); // sendMS
                    sendSize = buffer.Length;
                    //Debug.Log("size = " + sendSize + "  //  type = " + type);
                    //if (sendSize > ConstKind.DataSizeBuf)
                    //{
                    //    Debug.Log("초과한 사이즈 타입 = " + type);
                    //}
                    while (true)
                    {
                        curSendSize += clientSock.Send(buffer, 0, sendSize, SocketFlags.None);
                        if (curSendSize >= sendSize)
                            break;
                    }
                }else
                {
                    sendThread.Abort();
                }
            }
            catch (SocketException e)
            {
                Debug.Log("전송 에러 : " + e.Message);
            }
        }
        public void SendByteTransform(g_Transform g_Tr)
        {
            try
            {
                /* 연결 성공시 */
                if (clientSock.Connected)
                {
                    int sendSize = 0;
                    MemoryStream sendStream = new MemoryStream();
                    Serializer.Serialize(sendStream, g_Tr);
                    byte[] buffer = sendStream.ToArray();
                    int size = buffer.Length;

                    SendByteSize(MyClientNum, size, g_DataType.TRANSFORM); // 사이즈 보내기
                    while (true)
                    {
                        sendSize += clientSock.Send(buffer, 0, size, SocketFlags.None);
                        if (sendSize >= size)
                            break;
                    }
                }else
                {
                    sendThread.Abort();
                }
            }
            catch (SocketException e)
            {
                Debug.Log("전송 에러 : " + e.Message);
            }
        }
        public void SendByteMessage(string message, g_DataType type)
        {
            //  Debug.Log("하하하");
            if (clientSock.Connected)
            {
                int sendSize = 0;
 //               Debug.Log("메세지 보내기 시작");
                g_Message g_ms = new g_Message();
                g_ms.message = message;

                MemoryStream sendStream = new MemoryStream();
                Serializer.Serialize(sendStream, g_ms);

                byte[] buffer = sendStream.ToArray();
                int size = buffer.Length;
                SendByteSize(MyClientNum, size, type); // 사이즈 보내기
                while (true)
                {
                    sendSize += clientSock.Send(buffer, 0, buffer.Length, SocketFlags.None);
                    if (sendSize >= buffer.Length)
                        break;
                }
            }
            else
            {
                Debug.Log("서버와 연결되지 않음");
                
            }
        }
        void sendData()
        {
            if (!clientSock.Connected)
            {
                Debug.Log("send스레드 종료 시작");
                sendThread.Abort();
                Debug.Log("send스레드 종료 끝");
            }
 //           Debug.Log("sendThread!");
            // 보낼 데이터 꺼내기
            PostData sendData = new PostData(g_DataType.NULLDATA, 0, MyClientNum);
            // 값 채우기
            if(postbox.GetSendData(ref sendData))
            {
              //  Debug.Log("보낼 데이타 = " + sendData.ClientNum + "//" + sendData.Type + "//" + (string)sendData.data);
                if (sendData.Type != g_DataType.NULLDATA)
                {
                    switch (sendData.Type)
                    {
                        case g_DataType.COMMAND:
                            //Debug.Log("보낼 데이타 = " + sendData.ClientNum + "//" + sendData.Type + "//" + (string)sendData.data);
                            SendByteMessage((string)sendData.data, g_DataType.COMMAND);
                            break;
                        case g_DataType.MESSAGE:
                            SendByteMessage((string)sendData.data, g_DataType.MESSAGE);
                            break;
                        case g_DataType.PROTOCOL:
                            SendByteMessage((string)sendData.data, g_DataType.PROTOCOL);
                            break;
                        case g_DataType.READYSET:
                            SendByteMessage((string)sendData.data, g_DataType.READYSET);
                            break;
                        case g_DataType.TRANSFORM:
                            SendByteTransform((g_Transform)sendData.data);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        #endregion
        // 서버 작업 진행
        void sendnThreadWork()
        {
            while (true)
            {
                try
                {
                    sendData();
                }
                catch(Exception e)
                {
                    Debug.Log(e.Message);
                }
                Thread.Sleep(ConstKind.SendThreadSleepTime);
            }
        }
        /*----------------------*
        * ##### CallBack ##### *
        *  Receive             *
        *----------------------*/
        #region 직접 호출 금지 Recv 관련
                                                                               
        // 받은 데이터가 g_DataSize일 경우 호출하는 함수(받은 크기)            
        private void recvDataSizeState(int nReadSize, ref Socket socket)       
        {                                                                      
            // 버퍼 크기를 받은만큼 정확히 맞춤                                
 //           Debug.Log("recvDataSize 호출");                                    
            byte[] reSizeBuffer = ReSizeBuffer(ref recvBuffer, nReadSize);     
            MemoryStream recvMS = new MemoryStream(reSizeBuffer);              
            try                                                                
            {
                recvDataSize = Serializer.Deserialize<g_DataSize>(recvMS); // 디시리얼라이즈(누가, 얼만큼, 어떤타입) 확보완료
            }
            catch(Exception e)
            {
                ErrorReset(ref socket);
            }
            isRecvSizeState = false;
        }
        // 받을 데이터가 ReadySet 일 경우
        private void recvReadySetState(ref byte[] recvBytes)
        {
            MemoryStream recvStream = new MemoryStream(recvBytes);
            g_readySet = Serializer.Deserialize<g_ReadySet>(recvStream);
            postbox.PushRecvData(g_DataType.READYSET, g_readySet);
 //           Debug.Log("start...");
            SetClientState(ClientState.AddComponent);
        }
        // 약속을 정하는 상태일 경우
        private void recvProtocolState(ref byte[] recvBytes)
        {
            MemoryStream recvMS = new MemoryStream(recvBytes);
            g_message = Serializer.Deserialize<g_Message>(recvMS);
            postbox.PushRecvData(g_DataType.PROTOCOL, g_message);
   //         Debug.Log("받은 PROTOCOL = " + g_message.message);
            if (ConstKind.StartClientPK == MyClientNum)
            {
                string strClientNum = g_message.message;
                MyClientNum = int.Parse(strClientNum);
                Debug.Log("나의 번호 부여 완료 = " + MyClientNum);
                SetClientState(ClientState.SendMyCharacter);
            }
        }
        // 메세지 받는 상태일 경우
        private void recvMessageState(ref byte[] recvBytes)
        {
            MemoryStream recvMS = new MemoryStream(recvBytes);
            g_message = Serializer.Deserialize<g_Message>(recvMS);
            postbox.PushRecvData(g_DataType.MESSAGE, g_message);
            Debug.Log("받은 메시지 = " + g_message.message);
        }
        // 트랜스폼 받는 상태일 경우
        private bool recvTransformState(ref byte[] recvBytes)
        {
            MemoryStream recvStream = new MemoryStream(recvBytes);
            g_transform = Serializer.Deserialize<g_Transform>(recvStream);
            postbox.PushRecvData(g_DataType.TRANSFORM, g_transform, recvDataSize.clientNum);
            return true;
        }
        private void recvCommandState(ref byte[] recvBytes)
        {
            MemoryStream recvStream = new MemoryStream(recvBytes);
            g_command = Serializer.Deserialize<g_Message>(recvStream);
            Debug.Log("받은 command message = " + g_command.message);
            //postbox.PushRecvData(g_DataType.TRANSFORM, g_transform, recvDataSize.clientNum);
        }

        void recvSize()
        {
            CurRecvDataSize = 0;
            while (true)
            {
  //              Debug.Log("size 받기 시작");
                CurRecvDataSize += clientSock.Receive(recvBuffer, 0, ConstKind.DataSizeBuf, SocketFlags.None);
                if (CurRecvDataSize >= ConstKind.DataSizeBuf)
                    break;
            }
            Debug.Log("받은 데이터 크기 = " + CurRecvDataSize);
            recvDataSizeState(ConstKind.DataSizeBuf, ref clientSock);
        }

        void recvnData()
        {
            CurRecvDataSize = 0;
            try
            {
                while (true)
                {
//                    Debug.Log("data 받기 시작");
                    CurRecvDataSize += clientSock.Receive(recvBuffer, 0, recvDataSize.size, SocketFlags.None);
                    if (CurRecvDataSize >= recvDataSize.size)
                        break;
                }
                //Debug.Log("받은 데이터 크기2 = " + CurRecvDataSize);
                //Debug.Log("recvDataSize.size2 = " + recvDataSize.size);
                byte[] recvBytes = ReSizeBuffer(ref recvBuffer, recvDataSize.size);
  //              Debug.Log("받은 데이터 타입 = " + recvDataSize.type);
                switch (recvDataSize.type)
                {
                    case g_DataType.READYSET:
                        recvReadySetState(ref recvBytes);
                        break;
                    case g_DataType.PROTOCOL:
                        recvProtocolState(ref recvBytes);
                        break;
                    case g_DataType.MESSAGE:
                        recvMessageState(ref recvBytes);
                        break;
                    case g_DataType.TRANSFORM:
                        recvTransformState(ref recvBytes);
                        break;
                    case g_DataType.COMMAND:
                        recvCommandState(ref recvBytes);
                        break;
                    default:
                        Debug.Log("정의 되어 있지 않은 타입 받음 type = " + recvDataSize.type);
                        break;
                }
                isRecvSizeState = true; // 서버로부터 사이즈 받는 상태 true로 바꿈
                recvDataSize = null; // 받을 데이터 초기화
            }
            catch(Exception e)
            {
                ErrorReset(ref clientSock);
            }
            
        }
        //  리셋 시킬 소켓(받은 패킷 다 버릴것)
        private void ErrorReset(ref Socket socket)
        {
            ErrorHandler.RecvBufferFlush(ref socket);
            recvDataSize = null;
            isRecvSizeState = true;
        }

        #endregion

        // 서버 작업 진행
        void recvnThreadWork()
        {
            while(true)
            {
                try
                {
                    if (!clientSock.Connected)
                    {
                        Debug.Log("recv스레드 종료 시작");
                        recvThread.Abort();
                        Debug.Log("recv스레드 종료 끝");
                    }
                   // Debug.Log("recvnThread! 일 하는 중~~~~~~");
                    if (isRecvSizeState)
                    {
                        recvSize();
                    }
                    else
                    {
                        recvnData();
                    }
                }
                catch (ProtoException se)
                {
                    Debug.Log(se.Message);
                    ErrorReset(ref clientSock);
                }
                Thread.Sleep(ConstKind.RecvThreadSleepTime);
            }
        }

    }
}
