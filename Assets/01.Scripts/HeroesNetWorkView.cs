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

namespace MyNetWorkView
{
    public class HeroesNetWorkView
    {
        private static HeroesNetWorkView instance;
        // 통신용 변수.
        private Socket clientSock;  /* client Socket */
        public int MyClientNum = -1; // 클라이언트 간의 구분 번호
        private Socket cbSock;   /* client Async Callback Socket */


        private byte[] recvBuffer; // 받을 버퍼
        private byte[] recvTrBuffer; // Tr 받을 버퍼
        private byte[] recvDataInfoBuffer; // dataSize 받을 버퍼
        g_DataSize recvDataSize; // 누가, 얼마나, 어떤 타입을 보내는지 미리 받을 곳
        g_DataSize sendDataSize; // send하기 전에 미리 내가 얼마나, 어떤 타입을 보낼지 넣어두는 곳
        g_Message g_message; // 메세지 받는 곳
        g_Transform g_transform; //트렌스폼 받는 곳
        public g_ReadySet g_readySet; // 준비된 플레이어 정보 받는 곳(종족, 팀정보, 플레이어 번호)
        public bool isStartState = false; // 모든 준비 완료 게임 스타트?
                                          //NetWork netWork;

        int mPacketNumTransform = 0; // 받는 패킷 트렌스폼 번호

        bool isRecvSizeState = true; // g_DataSize을 받을 상태인가?

        GameObject MoveSynchronization;
        MoveSynchronization MoveSyncComponent;

        GameObject InitCharacter;
        InitializationCharacter InitComponent;

        bool isTransformDeserialize = false;

        public static HeroesNetWorkView GetInstance()
        {
            if (instance == null)
            {
                Debug.Log("NetworkView 생성");
                instance = new HeroesNetWorkView();
                return instance;
            }
            else
            {
                return instance;
            }
        }

        private HeroesNetWorkView()
        {
            Awake();
            Start();
        }

        void Awake()
        {
            MoveSynchronization = GameObject.FindGameObjectWithTag("MoveSynchronization");
            MoveSyncComponent = MoveSynchronization.GetComponent<MoveSynchronization>();

            InitCharacter = GameObject.FindGameObjectWithTag("InitCharacter");
            InitComponent = InitCharacter.GetComponent<InitializationCharacter>();
            recvBuffer = new byte[ConstKind.BufSize];
            recvTrBuffer = new byte[ConstKind.BufSize];
            recvDataInfoBuffer = new byte[ConstKind.BufSize];
            sendDataSize = new g_DataSize();
        }

        // Use this for initialization
        void Start()
        {
            DoInit();
            while (true)
            {
                if (clientSock.Connected)
                {
                    break;
                }
            }
        }

        // 받은 크기만큼 버퍼를 딱 맞춤.
        private byte[] ReSizeBuffer(ref byte[] sourceBuffer, int reSize)
        {
            byte[] newBuffer = new byte[reSize];
            Array.Copy(sourceBuffer, newBuffer, reSize);

            return newBuffer;
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

        private void DoInit()
        {
            Debug.Log("DoInit 호출");
            clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.BeginConnect();
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
                cbSock = tempSock;
                cbSock.BeginReceive(recvDataInfoBuffer, 0, ConstKind.DataSizeBuf, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSock);
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
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void SendByteSize(int clientNum, int size, g_DataType type)
        {
            try
            {
                /* 연결 성공시 */
                if (clientSock.Connected)
                {
                    sendDataSize.clientNum = clientNum;
                    sendDataSize.size = size;
                    sendDataSize.type = type;
                    MemoryStream sendMS = new MemoryStream();
                    Serializer.Serialize(sendMS, sendDataSize); // MemoryStream sendMS에 Serialize값 담기
                    byte[] buffer = sendMS.ToArray(); // sendMS
                    clientSock.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallBack), sendDataSize);
                }
            }
            catch (SocketException e)
            {
                Debug.Log("전송 에러 : " + e.Message);
            }
        }

        public void SendByteTransform(Transform tr)
        {
            try
            {
                /* 연결 성공시 */
                if (clientSock.Connected)
                {
                    if (mPacketNumTransform >= 9)
                    {
                        mPacketNumTransform = 0;
                    }
                    else
                    {
                        mPacketNumTransform++;
                    }
                    g_Transform g_Tr = new g_Transform();
                    g_Tr.packetNum = mPacketNumTransform;
                    MoveSyncComponent.copyTransformToG_Transform(ref g_Tr, ref tr);

                    MemoryStream sendStream = new MemoryStream();
                    Serializer.Serialize(sendStream, g_Tr);

                    byte[] buffer = sendStream.ToArray();
                    int size = buffer.Length;

                    SendByteSize(MyClientNum, size, g_DataType.TRANSFORM); // 사이즈 보내기
                    clientSock.BeginSend(buffer, 0, size, SocketFlags.None, new AsyncCallback(SendCallBackTransform), g_Tr); // Tr 보내기
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
                //Debug.Log("메세지 보내기 시작");
                g_Message g_ms = new g_Message();
                g_ms.message = message;

                MemoryStream sendStream = new MemoryStream();
                Serializer.Serialize(sendStream, g_ms);

                byte[] buffer = sendStream.ToArray();
                int size = buffer.Length;

                SendByteSize(MyClientNum, size, type); // 사이즈 보내기
                clientSock.BeginSend(buffer, 0, size, SocketFlags.None, new AsyncCallback(SendCallBackMessage), g_ms); // message 보내기
            }
            else
            {
                Debug.Log("서버와 연결되지 않음");
            }
        }

        /*----------------------*
         * ##### CallBack ##### *
         *        Send          *
         *----------------------*/
        private void SendCallBack(IAsyncResult IAR)
        {
            g_DataSize dataSize = (g_DataSize)IAR.AsyncState;
        }
        private void SendCallBackTransform(IAsyncResult IAR)
        {
            g_Transform dataTr = (g_Transform)IAR.AsyncState;
            //    Debug.Log("전송 완료 CallBack position.x : " + dataTr.position.x);
        }

        private void SendCallBackMessage(IAsyncResult IAR)
        {
            g_Message message = (g_Message)IAR.AsyncState;
            //    Debug.Log("전송 완료 message = " + message.message);
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /*----------------------*
         * ##### CallBack ##### *
         *  Receive             *
         *----------------------*/

        /*
         * 다음 함수부분에 
         MemoryStream recvStream = new MemoryStream(recvBytes); 을 함수마다 쓰고 있어서 공통으로 빼지 않은 이유는
         파라미터로 byte[]을 받아 올바른 패킷이 왔는지 확인한 뒤에 MemoryStream recvStream을 만들기 위해서 임.
         */

        // 받은 데이터가 g_DataSize일 경우 호출하는 함수(받은 크기)
        private bool recvDataSizeState(int nReadSize, ref Socket socket)
        {
            Debug.Log("recvDataSize 호출");
            byte[] reSizeBuffer = ReSizeBuffer(ref recvDataInfoBuffer, nReadSize); // 버퍼 크기를 받은만큼 정확히 맞춤
                                                                           // Deserialize 가능 검사
                                                                           //if (ConstKind.GoodRecvDataSize != reSizeBuffer[nReadSize - 1])
                                                                           //{
                                                                           //    ErrorHandler.RecvBufferFlush(ref socket);
                                                                           //    return false;
                                                                           //}

            //string message = new UTF8Encoding().GetString(recvBuffer, 0, nReadSize);
            //if(reSizeBuffer[5] == 63)
            //{
            //    Debug.Log("error!!!!!!!!!!!!");
            //    this.Receive(ConstKind.DataSizeBuf);
            //    return false;
            //}
            MemoryStream recvMS = new MemoryStream(reSizeBuffer);
            recvDataSize = Serializer.Deserialize<g_DataSize>(recvMS); // 디시리얼라이즈(누가, 얼만큼, 어떤타입) 확보완료
            isRecvSizeState = false;
            return true;
        }
        // 받을 데이터가 ReadySet 일 경우
        private void recvReadySetState(ref byte[] recvBytes)
        {
            MemoryStream recvStream = new MemoryStream(recvBytes);
            g_readySet = Serializer.Deserialize<g_ReadySet>(recvStream);
            isStartState = true;
            Debug.Log("start...");
        }
        // 약속을 정하는 상태일 경우
        private void recvProtocolState(ref byte[] recvBytes)
        {
            MemoryStream recvMS = new MemoryStream(recvBytes);
            g_message = Serializer.Deserialize<g_Message>(recvMS);
            Debug.Log("받은 PROTOCOL = " + g_message.message);
            if (-1 == MyClientNum)
            {
                string strClientNum = g_message.message;
                MyClientNum = int.Parse(strClientNum);
                Debug.Log("나의 번호 부여 완료 = " + MyClientNum);
            }
        }
        // 메세지 받는 상태일 경우
        private void recvMessageState(ref byte[] recvBytes)
        {
            MemoryStream recvMS = new MemoryStream(recvBytes);
            g_message = Serializer.Deserialize<g_Message>(recvMS);
            Debug.Log("받은 메시지 = " + g_message.message);
        }
        // 트랜스폼 받는 상태일 경우
        private bool recvTransformState(ref byte[] recvBytes, ref Socket socket)
        {
            SendByteMessage("aaaaa", g_DataType.MESSAGE);
            MemoryStream recvStream = new MemoryStream(recvBytes);
            g_transform = Serializer.Deserialize<g_Transform>(recvStream);

            int pkNum = recvDataSize.clientNum;
            if (pkNum != MyClientNum && InitComponent.isInitCharacter)
            {
                MoveSyncComponent.MoveCharacter(pkNum, ref g_transform);
            }
            return true;
        }

        private void OnReceiveCallBack(IAsyncResult IAR)
        {
            Socket tempSock = (Socket)IAR.AsyncState;
            try
            {
                //Debug.Log("OnReceiveCallBack 호출");
               
                int nReadSize = tempSock.EndReceive(IAR);
                if (nReadSize == ConstKind.DataSizeBuf && isRecvSizeState)
                {
                    if (recvDataSizeState(ConstKind.DataSizeBuf, ref tempSock))
                    {
                        Receive(recvDataSize.size);  // 다음 받을 만큼 받기
                    }
                    else
                    {
                        Receive(ConstKind.DataSizeBuf);
                    }
                    return; // if문을 빠져나가서 또 Receive을 호출 하지 않기 위해 함수를 종료 시킴
                }
                else if (nReadSize == recvDataSize.size && isRecvSizeState == false)// 받을 크기만큼이고, 진짜 데이터를 받을 상태이면
                {
                    //Debug.Log("recvDataSize.type = " + recvDataSize.type);
                    // GameObject targetObj = null;
                    //targetObj = players[recvDataSize.clientNum]; //GameObject.FindGameObjectWithTag(recvDataSize.clientNum.ToString());

                    

                    
                    switch (recvDataSize.type)
                    {
                        case g_DataType.READYSET:
                            byte[] recvBytes = ReSizeBuffer(ref recvBuffer, nReadSize);
                            recvReadySetState(ref recvBytes);
                            break;
                        case g_DataType.PROTOCOL:
                            byte[] recvBytes2 = ReSizeBuffer(ref recvBuffer, nReadSize);
                            recvProtocolState(ref recvBytes2);
                            break;
                        case g_DataType.MESSAGE:
                            byte[] recvBytes3 = ReSizeBuffer(ref recvBuffer, nReadSize);
                            recvMessageState(ref recvBytes3);
                            break;
                        case g_DataType.TRANSFORM:
                            byte[] recvBytes4 = ReSizeBuffer(ref recvTrBuffer, nReadSize);
                            recvTransformState(ref recvBytes4, ref tempSock);
                            break;
                        default:
                            Debug.Log("정의 되어 있지 않은 타입 받음");
                            break;
                    }
                    //Debug.Log("6");
                    isRecvSizeState = true; // 서버로부터 사이즈 받는 상태 true로 바꿈
                                            //Debug.Log("7");
                    recvDataSize = null; // 받을 데이터 초기화
                }
                #region // 예외처리
                else if (nReadSize != recvDataSize.size && isRecvSizeState == false)
                {
                    Debug.Log("패킷 잘못 받음 nReadSize = " + nReadSize + " // recvDataSize.size = " + recvDataSize.size);
                    ErrorHandler.RecvBufferFlush(ref tempSock);
                    isRecvSizeState = true;
                    Receive(ConstKind.DataSizeBuf);
                }
                else
                {
                    Debug.Log("둘다 아니야. readSize = " + nReadSize + " // recvDataSize.size = " + recvDataSize.size + " // isRecvSizeState = " + isRecvSizeState);
                    ErrorHandler.RecvBufferFlush(ref tempSock);
                    isRecvSizeState = true;
                    Receive(ConstKind.DataSizeBuf);
                }
                #endregion
                Receive(ConstKind.DataSizeBuf); // 진짜 데이터를 받았으니 이제 다시 얼마큼 받아야하는지 g_DataSize를 받을 차례
            }
            catch (ProtoException se)
            {
                ErrorHandler.RecvBufferFlush(ref tempSock);
                recvDataSize = null;
                isRecvSizeState = true;
                Receive(ConstKind.DataSizeBuf);
                //if(isTransformDeserialize == false)
                //{
                //    isTransformDeserialize = true;
                //    Debug.Log("Type = " + se.GetType());
                //    //Debug.Log("ProtoExecept!!2");
                //    ErrorHandler.RecvBufferFlush(ref tempSock);
                //    Receive(ConstKind.DataSizeBuf);
                //}

                //if (se.SocketErrorCode == SocketError.ConnectionReset)
                //{
                //    Debug.Log("소켓 에러");
                //    this.BeginConnect();
                //}
            }
        }

        private void Receive(int recvSize)
        {
            if(isRecvSizeState)
            {
                cbSock.BeginReceive(recvDataInfoBuffer, 0, recvSize, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSock);
            }
            else if(recvDataSize.type == g_DataType.TRANSFORM)
            {
                cbSock.BeginReceive(recvTrBuffer, 0, recvSize, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSock);
            }else
            {
                cbSock.BeginReceive(recvBuffer, 0, recvSize, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSock);
            }
            
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}




/*
 string clientNum = MyClientNum.ToString();
        int i = dataSize.size - 1;
        int ii = dataSize.size - 2;
        int iii = dataSize.size - 3;
        string message2 = clientNum + " client // [1] = [ " + recvBytes[1].ToString() + " ]";
        //string message3 = clientNum + " client // [-1] = [ " + recvBytes[i].ToString() + " ]";
        if (recvBytes[i] == 128)
        {
            Debug.Log("오류 = // [0] = " + recvBytes[0] + " // [1] = " + recvBytes[1] + " // [2] = " + recvBytes[2] + " // [" + iii + "] = " + recvBytes[iii] + " // [" + ii + "] = "+ recvBytes[ii]);
        }
        SendByteMessage(message2, g_DataType.MESSAGE);
        //SendByteMessage(message3, g_DataType.MESSAGE);
        Debug.Log("+일반 = // [0] = " + recvBytes[0] + " // [1] = " + recvBytes[1] + " // [2] = " + recvBytes[2] + " // [" + ii + "] = " + recvBytes[ii] + "// [" + i + "] = " + recvBytes[i]);

     */
