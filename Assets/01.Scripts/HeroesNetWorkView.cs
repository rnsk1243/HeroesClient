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



public class HeroesNetWorkView : MonoBehaviour
{
    // 통신용 변수.
    private Socket clientSock;  /* client Socket */
    public int MyClientNum = -1; // 클라이언트 간의 구분 번호
    private Socket cbSock;   /* client Async Callback Socket */

    const int DataSizeBuf = 6; // DataSize를 받는데 필요한 크기
    private byte[] recvBuffer; // 받을 버퍼
    g_DataSize recvDataSize; // 누가, 얼마나, 어떤 타입을 보내는지 미리 받을 곳
    g_DataSize sendDataSize; // send하기 전에 미리 내가 얼마나, 어떤 타입을 보낼지 넣어두는 곳
    g_Message g_message; // 메세지 받는 곳
    g_Transform g_transform; //트렌스폼 받는 곳
    public g_ReadySet g_readySet; // 준비된 플레이어 정보 받는 곳(종족, 팀정보, 플레이어 번호)
    public bool isStartState = false; // 모든 준비 완료 게임 스타트?
    //NetWork netWork;
    // 접속할 곳의 IP주소.
    private string m_address = "127.0.0.1";

    // 접속할 곳의 포트 번호.
    private const int m_port = 9000;
    const int BufSize = 256;
    int mPacketNumTransform = 0; // 받는 패킷 트렌스폼 번호

    bool isRecvSizeState = true; // g_DataSize을 받을 상태인가?

    // GameObject MoveSynchronization; // 위치동기화 객체 가져올 곳
    // MoveSynchronization MoveSyncComponent; // 위치동기화 객체에 딸린 컴포넌트

    // GameObject InitCharacter; // 캐릭터 생성 객체
    // InitializationCharacter InitComponent; // 캐릭터 생성 객체에 딸린 컴포넌트

    #region MoveSynchronization 을 위한 프로퍼티
    Vector3 newPosition;
    Vector3 newRotation;
    Vector3 newScal;
    bool isNewTransform = false;
    int targetPK = -1;

    public Vector3 getNewPosition() { return newPosition; }
    public Vector3 getNewRotation() { return newRotation; }
    public Vector3 getNewScal() { return newScal; }
    public bool getIsNewTransform() { return isNewTransform; }
    public void setFalseNewTransform() { isNewTransform = false; }
    public int getTargetPK() { return targetPK; }
    #endregion
    

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
            clientSock.BeginConnect(m_address, m_port, new AsyncCallback(ConnectCallBack), clientSock);
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
            tempSock.EndConnect(IAR);
            Debug.Log("서버로 접속 성공 : " + svrEP.Address);
            cbSock = tempSock;
            cbSock.BeginReceive(this.recvBuffer, 0, DataSizeBuf, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSock);
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

    private void copyTransformToG_Transform(ref g_Transform target, ref Transform source)
    {
        target.position.x = source.position.x;
        target.position.y = source.position.y;
        target.position.z = source.position.z;
        target.rotation.x = source.rotation.x;
        target.rotation.y = source.rotation.y;
        target.rotation.z = source.rotation.z;
        target.scale.x = source.localScale.x;
        target.scale.y = source.localScale.y;
        target.scale.z = source.localScale.z;
    }

    public void SendByteTransform(Transform tr)
    {
        try
        {
            /* 연결 성공시 */
            if (clientSock.Connected)
            {
                if (mPacketNumTransform >= 10)
                {
                    mPacketNumTransform = 0;
                }else
                {
                    mPacketNumTransform++;
                }
                g_Transform g_Tr = new g_Transform();
                g_Tr.packetNum = mPacketNumTransform;
                // g_Transform에 값 채우기
                copyTransformToG_Transform(ref g_Tr, ref tr);

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
        if(clientSock.Connected)
        {
            Debug.Log("메세지 보내기 시작");
            g_Message g_ms = new g_Message();
            g_ms.message = message;

            MemoryStream sendStream = new MemoryStream();
            Serializer.Serialize(sendStream, g_ms);

            byte[] buffer = sendStream.ToArray();
            int size = buffer.Length;

            SendByteSize(MyClientNum, size, type); // 사이즈 보내기
            clientSock.BeginSend(buffer, 0, size, SocketFlags.None, new AsyncCallback(SendCallBackMessage), g_ms); // message 보내기
        }else
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
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /*----------------------*
     * ##### CallBack ##### *
     *  Receive             *
     *----------------------*/
    // 받은 데이터가 g_DataSize일 경우 호출하는 함수(받은 크기)
    private void recvDataSizeState(int nReadSize)
    {
        //string message = new UTF8Encoding().GetString(recvBuffer, 0, nReadSize);
        byte[] reSizeBuffer = ReSizeBuffer(ref recvBuffer, nReadSize); // 버퍼 크기를 받은만큼 정확히 맞춤
        MemoryStream recvMS = new MemoryStream(reSizeBuffer);

        recvDataSize = Serializer.Deserialize<g_DataSize>(recvMS); // 디시리얼라이즈(누가, 얼만큼, 어떤타입) 확보완료
        this.Receive(recvDataSize.size);  // 다음 받을 만큼 받기
        isRecvSizeState = false;
    }
    // 받을 데이터가 ReadySet 일 경우
    private void recvReadySetState(MemoryStream recvStream)
    {
        g_readySet = Serializer.Deserialize<g_ReadySet>(recvStream);
        isStartState = true;
        Debug.Log("start...");
    }
    // 약속을 정하는 상태일 경우
    private void recvProtocolState(MemoryStream recvStream)
    {
        g_message = Serializer.Deserialize<g_Message>(recvStream);
        Debug.Log("받은 PROTOCOL = " + g_message.message);
        if (-1 == MyClientNum)
        {
            string strClientNum = g_message.message;
            MyClientNum = int.Parse(strClientNum);
            Debug.Log("나의 번호 부여 완료 = " + MyClientNum);
        }
    }
    // 메세지 받는 상태일 경우
    private void recvMessageState(MemoryStream recvStream)
    {
        g_message = Serializer.Deserialize<g_Message>(recvStream);
        Debug.Log("받은 메시지 = " + g_message.message);
    }
    // 트랜스폼 받는 상태일 경우
    private void recvTransformState(MemoryStream recvStream)
    {
        g_transform = Serializer.Deserialize<g_Transform>(recvStream);
        int pkNum = recvDataSize.clientNum; // 누가 보낸 트랜스폼인지 확인
        Debug.Log("pkNUm = " + pkNum);
        if (pkNum != MyClientNum)// 내가 보낸 트랜스폼이 아니고 캐릭터가 초기화 된 상태이면
        {
            newPosition.x = g_transform.position.x;
            newPosition.y = g_transform.position.y;
            newPosition.z = g_transform.position.z;

            newRotation.x = g_transform.rotation.x;
            newRotation.y = g_transform.rotation.y;
            newRotation.z = g_transform.rotation.z;

            newScal.x = g_transform.scale.x;
            newScal.y = g_transform.scale.y;
            newScal.z = g_transform.scale.z;
            isNewTransform = true;
            targetPK = pkNum;
        }
    }

    private void OnReceiveCallBack(IAsyncResult IAR)
    {
        try
        {
            Socket tempSock = (Socket)IAR.AsyncState;
            int nReadSize = tempSock.EndReceive(IAR); // BeginReceive호출에서 정해진 크기만큼 데이터 받기
            if (nReadSize == DataSizeBuf && isRecvSizeState) // 받은 크기가 g_DataSize 만하고(6바이트) g_DataSize 받을 상태인가?
            {
                recvDataSizeState(nReadSize);
                return; // 함수를 종료
            }
            else if(nReadSize == recvDataSize.size && isRecvSizeState == false) // 받을 크기만큼이고, 진짜 데이터를 받을 상태이면
            {
                // GameObject targetObj = null;
                //targetObj = players[recvDataSize.clientNum]; //GameObject.FindGameObjectWithTag(recvDataSize.clientNum.ToString());
                byte[] reSizeBuffer = ReSizeBuffer(ref recvBuffer, nReadSize);
                MemoryStream recvStream = new MemoryStream(reSizeBuffer);
                // 받을 타입에 따른 디시리얼라이즈
                switch (recvDataSize.type)
                {
                    case g_DataType.READYSET:
                        recvReadySetState(recvStream);
                        break;
                    case g_DataType.PROTOCOL:
                        recvProtocolState(recvStream);
                        break;
                    case g_DataType.MESSAGE:
                        recvMessageState(recvStream);
                        break;
                    case g_DataType.TRANSFORM:
                        recvTransformState(recvStream);
                        break;
                    default:
                        Debug.Log("정의 되어 있지 않은 타입 받음");
                        break;
                }
                isRecvSizeState = true; // 서버로부터 사이즈 받는 상태 true로 바꿈
                recvDataSize = null; // 받을 데이터 초기화
            }
            Receive(DataSizeBuf); // 진짜 데이터를 받았으니 이제 다시 얼마큼 받아야하는지 g_DataSize를 받을 차례
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.ConnectionReset)
            {
                this.BeginConnect();
            }
        }
    }

    private void Receive(int recvSize)
    {
       // Debug.Log("Receive 호출" + recvSize);
        cbSock.BeginReceive(this.recvBuffer, 0, recvSize, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSock);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    void Awake()
    {
        newPosition = new Vector3();
        newRotation = new Vector3();
        newScal = new Vector3();

        recvBuffer = new byte[BufSize];
        sendDataSize = new g_DataSize();

        DoInit();
        // 위치 동기화 객체 가져오고
        //MoveSynchronization = GameObject.FindGameObjectWithTag("MoveSynchronization");
        //MoveSyncComponent = MoveSynchronization.GetComponent<MoveSynchronization>();
        // 캐릭터 생성 객체 가져오고
        //InitCharacter = GameObject.FindGameObjectWithTag("InitCharacter");
        //InitComponent = InitCharacter.GetComponent<InitializationCharacter>();
    }

    // Use this for initialization
    void Start()
    {
        
        ////////
        while(true)
        {
            if(clientSock.Connected)
            {
                break;
            }
        }
       
    }


}
