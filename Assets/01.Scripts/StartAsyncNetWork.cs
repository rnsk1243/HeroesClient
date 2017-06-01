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



public class StartAsyncNetWork : MonoBehaviour
{
    // 통신용 변수.
    private Socket clientSock;  /* client Socket */
    public int MyClientNum = -1;
    private Socket cbSock;   /* client Async Callback Socket */

    private byte[] recvBuffer;
    g_DataSize recvDataSize;
    g_DataSize nextDataSize; // recv한 것을 임시 보관
    g_DataSize sendDataSize;
   // g_Message mMessageToServer; // 서버로 부터 받은 메세지 저장
    g_Message g_message;
    g_Transform g_transform;
    public g_ReadySet g_readySet;
    public bool isStartState = false;
    //NetWork netWork;
    // 접속할 곳의 IP주소.
    private string m_address = "127.0.0.1";

    // 접속할 곳의 포트 번호.
    private const int m_port = 9000;
    const int BufSize = 1024;
    int mPacketNumTransform = 0;

    bool isRecvSizeState = true;
    public byte[] ReSizeBuffer(ref byte[] sourceBuffer, int reSize)
    {
        byte[] newBuffer = new byte[reSize];
        Array.Copy(sourceBuffer, newBuffer, reSize);
        return newBuffer;
    }

    public void BeginConnect()
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

    public void DoInit()
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
            cbSock.BeginReceive(this.recvBuffer, 0, 6, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSock);
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
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void copyTransform(ref g_Transform target, ref Transform source)
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
              //Debug.Log("보내기 = " + sendDataSize.size);
                MemoryStream sendMS = new MemoryStream();
                Serializer.Serialize(sendMS, sendDataSize); // MemoryStream sendMS에 Serialize값 담기
                byte[] buffer = sendMS.ToArray(); // sendMS
              //Debug.Log("Bufffffffffffffffff = " + buffer.Length);
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
                if (mPacketNumTransform >= 100)
                {
                    mPacketNumTransform = 0;
                }
                g_Transform g_Tr = new g_Transform();
                g_Tr.packetNum = mPacketNumTransform;
                copyTransform(ref g_Tr, ref tr);

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
       // Debug.Log("전송 완료 CallBack dataSize.size : " + dataSize.size);

        //switch(dataSize.type)
        //{
        //    case g_DataType.TRANSFORM:
        //        SendByteTransform(player.transform);
        //        break;
        //    case g_DataType.MESSAGE:
        //        break;
        //    default:
        //        break;
        //}

        
    }
    private void SendCallBackTransform(IAsyncResult IAR)
    {
        g_Transform dataTr = (g_Transform)IAR.AsyncState;
        Debug.Log("전송 완료 CallBack position.x : " + dataTr.position.x);

    }

    private void SendCallBackMessage(IAsyncResult IAR)
    {
        g_Message message = (g_Message)IAR.AsyncState;
        Debug.Log("전송 완료 message = " + message.message);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    /*----------------------*
     * ##### CallBack ##### *
     *  Receive             *
     *----------------------*/
    private void OnReceiveCallBack(IAsyncResult IAR)
    {
        try
        {
            Debug.Log("OnReceiveCallBack 호출");
            Socket tempSock = (Socket)IAR.AsyncState;
            int nReadSize = tempSock.EndReceive(IAR);

           // int s = nextDataSize.size;
          //  Debug.Log("!!!!!!!!recvDataSize.size = " + s);
            Debug.Log("!!!!!!!!nReadSize = " + nReadSize);
          //  Debug.Log("=========과녕 타입은 = " + nextDataSize.type);
            if (nReadSize == 6 && isRecvSizeState)
            {
                //string message = new UTF8Encoding().GetString(recvBuffer, 0, nReadSize);
                byte[] reSizeBuffer = ReSizeBuffer(ref recvBuffer, nReadSize);
                MemoryStream recvMS = new MemoryStream(reSizeBuffer);
                
                recvDataSize = Serializer.Deserialize<g_DataSize>(recvMS);
               // Debug.Log("서버로 데이터 수신 : " + recvDataSize.size);
               // Debug.Log("recvDataSize.size = " + recvDataSize.size);
                nextDataSize = recvDataSize;
                this.Receive(nextDataSize.size);
                isRecvSizeState = false;
                return; // if문을 빠져나가서 또 Receive을 호출 하지 않기 위해 함수를 종료 시킴
            }
            else if(nReadSize == nextDataSize.size && isRecvSizeState == false)
            {
                Debug.Log("if문 안");
                Debug.Log("과녕 타입은 = " + nextDataSize.type);
                if(nextDataSize == null)
                {
                    Debug.Log("nextDataSize = null");
                    return;
                }
                // GameObject targetObj = null;
                //targetObj = players[recvDataSize.clientNum]; //GameObject.FindGameObjectWithTag(recvDataSize.clientNum.ToString());
                byte[] reSizeBuffer = ReSizeBuffer(ref recvBuffer, nReadSize);
                MemoryStream recvStream = new MemoryStream(reSizeBuffer);

                switch (nextDataSize.type)
                {
                    case g_DataType.READYSET:
                        g_readySet = Serializer.Deserialize<g_ReadySet>(recvStream);
                        isStartState = true;
                        Debug.Log("start...");
                        break;
                    case g_DataType.PROTOCOL:
                        g_message = Serializer.Deserialize<g_Message>(recvStream);
                        Debug.Log("받은 PROTOCOL = " + g_message.message);
                        if (-1 == MyClientNum)
                        {
                            string strClientNum = g_message.message;
                            MyClientNum = int.Parse(strClientNum);
                            Debug.Log("나의 번호 부여 완료 = " + MyClientNum);
                        }
                        break;
                    case g_DataType.MESSAGE:
                        g_message = Serializer.Deserialize<g_Message>(recvStream);
                        Debug.Log("받은 메시지 = " + g_message.message);
                        break;
                    case g_DataType.TRANSFORM:
                        g_transform = Serializer.Deserialize<g_Transform>(recvStream);
                        Debug.Log("받은 x위치 = " + g_transform.position.x);
                        //Vector3 newPosition = new Vector3(g_transform.position.x, g_transform.position.y, g_transform.position.z);
                        //Vector3 newRotation = new Vector3(g_transform.rotation.x, g_transform.rotation.y, g_transform.rotation.z);
                        //Vector3 newScale = new Vector3(g_transform.scale.x, g_transform.scale.y, g_transform.scale.z);
                        //otherPlayer.transform.Translate(newPosition);
                        ////otherPlayer.transform.position = Vector3.MoveTowards(otherPlayer.transform.position, newPosition, 0.0f);
                        //otherPlayer.transform.Rotate(newRotation); // rotation(Quaternion.Euler(newRotation));
                        //Debug.Log("#############위치 이동 성공###############");
                        break;
                    default:

                        break;
                }
                isRecvSizeState = true; // 서버로부터 사이즈 받는 상태 true로 바꿈
                recvDataSize = null;
            }
            Receive(6);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.ConnectionReset)
            {
                this.BeginConnect();
            }
        }
    }

    public void Receive(int recvSize)
    {
        Debug.Log("Receive 호출" + recvSize);
        cbSock.BeginReceive(this.recvBuffer, 0, recvSize, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSock);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    // Use this for initialization
    void Start()
    {
        recvBuffer = new byte[BufSize];
        sendDataSize = new g_DataSize();
        nextDataSize = new g_DataSize();
       // recvDataSize = new g_DataSize();
        DoInit();
        ////////
        //Instantiate(player);
        //Instantiate(otherPlayer);
        //players = new GameObject[4];
        while(true)
        {
            if(clientSock.Connected)
            {
                 
             //   
                break;
            }
        }
       
    }


}
