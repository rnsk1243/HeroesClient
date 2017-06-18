using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using graduationWork;
using NamespaceConstKinds;
using NamespaceUseful;
using NamespaceErrorHandler;


namespace NamespaceCommandKinds
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

namespace NamespaceConstKinds
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
        public const int SendThreadSleepTime = 16; // millisecond
        public const int RecvThreadSleepTime = 16; // millisecond
        public const float CheckSendQueue_WaitForSeconds = 0.01f; // 여러 클래스의 큐를 돌면서 서버에 보낼 데이터를 Postbox에 넣고 쉬는 시간
        public const float CheckRecvQueue_WaitForSeconds = 0.01f; // 서버에서 받은 데이터를 Postbox에서 꺼내서 각 클래스에게 전달 하고 쉬는 시간

        public const float MoveSpeed = 10.0f;
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
        //public const string TagPlayerCtrl = "PlayerCtrl";
        public const int EnterRoomPeopleLimit = 4; // 방 최대 인원수 & 플레이어 인원수
        public const int NewData = 0; // 새로운 데이터
        public const int OldData = 1; // 오래된 데이터
        public const int OldOldData = 2; // 중간 오래된 데이터
        public const int OldOldOldData = 3; // 최고 오래된 데이터
        public const int MiddleResultCoordinateNum = 4; // 중간값 보간 몇개할지(높을 수록 같은 좌표가 겹쳐 잔상)
        public const float CheckMoveSendTr_WaitForSeconds = 0.05f; //3프레임당 1번 자기 위치를 queue에 Push 하고 쉬는 시간 
        public const float Move_WaitForSeconds = 0.013f; // 1프레임 이동 하고 얼마나 쉬는 시간(높을 수록 뛰엄뛰엄 이동)
        public const int InterpolationCoordinateNum = 4; // 보간하는데 필요한 점의 갯수 4개(3차 방정식)
        public const int InterpolationResultCoordinateNum = 7; // 보간이 완료된 결과 점의 좌표 갯수 4개 + 3개(보간되어 나온 좌표)
        public const int Matrix4x4Size = 16;
        public const float DeltaPositionSend = 0.01f; // 얼마큼 위치 변화가 있으면 나의 위치를 보낼지 정함(이동 민감도)

        public enum Transform { Position, Rotation, Scale };
        public enum ErrorCode
        {
            ERROR_MISS_PACKET = 51, // 잘 못된 패킷 정보를 받음
            ERROR_ARRAY_OUT_OF_LENGTH = 53 // 배열 크기를 벗어남.
        }
    }
}

namespace NamespaceErrorHandler
{
    public static class ErrorHandler
    {
        public static ConstKind.ErrorCode RecvBufferFlush(ref Socket Sock)
        {
            //Debug.Log("RecvBuffer 비우기");
            byte[] tempBuf = new byte[ConstKind.RecvBufferFlushSize];
            Sock.Receive(tempBuf);

            return ConstKind.ErrorCode.ERROR_MISS_PACKET;
        }

        public static ConstKind.ErrorCode ArrayLengthError()
        {
            return ConstKind.ErrorCode.ERROR_MISS_PACKET;
        }

        public static ConstKind.ErrorCode ErrorHandlerFunc(ConstKind.ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case ConstKind.ErrorCode.ERROR_MISS_PACKET:

                    break;
                case ConstKind.ErrorCode.ERROR_ARRAY_OUT_OF_LENGTH:
                    ArrayLengthError();
                    break;
            }
            Debug.Log("Error Code = " + errorCode);
            return errorCode;
        }
    }
}


namespace NamespaceMathCalc
{
    public static class MathCalc
    {
        // 역행렬 반환 함수
        private static double[,] MInverse(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            double[,] result = new double[n, n];
            double[,] tmpWork = new double[n, n];
            Array.Copy(matrix, tmpWork, n * n);  // 기존값을 보존하기 위함.  
                                                 // 계산 결과가 저장되는 result 행렬을 단위행렬로 초기화
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    result[i, j] = (i == j) ? 1 : 0;
            // 대각 요소를 0 이 아닌 수로 만듦
            const double ERROR = 1.0e-10;
            for (int i = 0; i < n; i++)
                if (-ERROR < tmpWork[i, i] && tmpWork[i, i] < ERROR) //if (-ERROR < tmpWork[i, i] && tmpWork[i, i] < ERROR)
                {
                    for (int k = 0; k < n; k++)
                    {
                        if (-ERROR < tmpWork[k, i] && tmpWork[k, i] < ERROR) continue;
                        for (int j = 0; j < n; j++)
                        {
                            tmpWork[i, j] += tmpWork[k, j];
                            result[i, j] += result[k, j];  // result[i*n+j] += result[k*n+j];
                        }
                        break;
                    }
                    if (-ERROR < tmpWork[i, i] && tmpWork[i, i] < ERROR) return result;
                }
            // Gauss-Jordan eliminatio
            for (int i = 0; i < n; i++)
            {
                // 대각 요소를 1로 만듦 
                double constant = tmpWork[i, i];      // 대각 요소의 값 저장 
                for (int j = 0; j < n; j++)
                {
                    tmpWork[i, j] /= constant;   // tmpWork[i][i] 를 1 로 만드는 작업 
                    result[i, j] /= constant; // result[i*n+j] /= constant;   // i 행 전체를 tmpWork[i][i] 로 나눔 
                }
                // i 행을 제외한 k 행에서 tmpWork[k][i] 를 0 으로 만드는 단계 
                for (int k = 0; k < n; k++)
                {
                    if (k == i) continue;      // 자기 자신의 행은 건너뜀 
                    if (tmpWork[k, i] == 0) continue;   // 이미 0 이 되어 있으면 건너뜀
                    // tmpWork[k][i] 행을 0 으로 만듦 
                    constant = tmpWork[k, i];
                    for (int j = 0; j < n; j++)
                    {
                        tmpWork[k, j] = tmpWork[k, j] - tmpWork[i, j] * constant;
                        result[k, j] = result[k, j] - result[i, j] * constant;  // result[k*n+j] = result[k*n+j] - result[i*n+j] * constant;
                    }
                }
            }
            return result;
        }

        private static double[,] MakePowMatrix(Vector3[] position)
        {
            if (position.Length < 4)
            {
                ErrorHandler.ErrorHandlerFunc(ConstKind.ErrorCode.ERROR_ARRAY_OUT_OF_LENGTH);
                return new double[4, 4];
            }

            double[,] matrix = new double[ConstKind.InterpolationCoordinateNum, ConstKind.InterpolationCoordinateNum];
            for (int i = ConstKind.NewData; i < ConstKind.InterpolationCoordinateNum; i++)
            {
                for (int j = ConstKind.NewData; j < ConstKind.InterpolationCoordinateNum; j++)
                {
                    matrix[i, j] = Math.Pow(position[i].x, j);
                }
            }
            return matrix;
        }

        private static double[] MultiplyMatrix(Vector3[] positionZ, double[,] InverseMatrix)
        {
            if (positionZ.Length < 4 && InverseMatrix.Length < 16)
            {
                ErrorHandler.ErrorHandlerFunc(ConstKind.ErrorCode.ERROR_ARRAY_OUT_OF_LENGTH);
                return new double[4];
            }

            double[] resultDouble = new double[4];
            for (int i = ConstKind.NewData; i < ConstKind.InterpolationCoordinateNum; i++)
            {
                for (int j = ConstKind.NewData; j < ConstKind.InterpolationCoordinateNum; j++)
                {
                    resultDouble[i] += (InverseMatrix[i, j] * positionZ[j].z);
                }

            }
            return resultDouble;
        }

        private static Vector3 GetPosition(double[] coe, double x, double y = 0.0)
        {
            if (coe.Length < 4)
            {
                ErrorHandler.ErrorHandlerFunc(ConstKind.ErrorCode.ERROR_ARRAY_OUT_OF_LENGTH);
                return new Vector3();
            }

            double z = coe[3] * Math.Pow(x, 3) + coe[2] * Math.Pow(x, 2) + coe[1] * Math.Pow(x, 1) + coe[0];
            return new Vector3((float)x, (float)y, (float)z);
        }

        private static double[] GetMiddlePositionX(Vector3[] value)
        {
            if (value.Length < 4)
            {
                ErrorHandler.ErrorHandlerFunc(ConstKind.ErrorCode.ERROR_ARRAY_OUT_OF_LENGTH);
                return new double[3];
            }

            double[] resultX = new double[3];
            for (int i = 0; i < 3; i++)
            {
                resultX[i] = (value[i].x + value[i + 1].x) / 2;
            }
            return resultX;
        }

        // 보간 좌표 구하기
        public static Vector3[] InterpolationCoordinate(int clientNum, g_Transform[] recvTransformArray)
        {
            Vector3[] resultPosition = new Vector3[ConstKind.InterpolationResultCoordinateNum];
            Vector3[] position = new Vector3[ConstKind.InterpolationCoordinateNum];
            double[] resultX;
            double[] coe = new double[4];
            // double a, b, c, d = 0.0; // f(x) = ax^3 + bx^2 + cx + d 에서 a,b,c,d 값 // 구해야하는 것.
            double[,] matrix;
            double[,] InverseMatrix;
            for (int i = ConstKind.NewData; i < ConstKind.InterpolationCoordinateNum; i++)
            {
                position[i] = Useful.getVector3(ref recvTransformArray[i], ConstKind.Transform.Position);
            }
            resultX = GetMiddlePositionX(position); // 구하고자 하는 z값에 대한 x값
            matrix = MakePowMatrix(position); // 행렬
            InverseMatrix = MInverse(matrix); // 역행렬
            coe = MultiplyMatrix(position, InverseMatrix); // 계수

            int j = 0;
            for (int i = ConstKind.NewData; i < ConstKind.InterpolationResultCoordinateNum; i++)
            {
                if (i % 2 == 1)
                {
                    // i가 홀수 일때
                    resultPosition[i] = GetPosition(coe, resultX[j], position[j].y);
                    j++;
                }
                else
                {
                    resultPosition[i] = position[(i / 2)];
                }
            }
            return resultPosition;
        }
    }
}

namespace NamespaceUseful
{
    public static class Useful
    {
        public static Vector3 getVector3(ref g_Transform gTr, ConstKind.Transform Tr)
        {
            switch (Tr)
            {
                case ConstKind.Transform.Position:
                    return new Vector3(gTr.position.x, gTr.position.y, gTr.position.z);
                case ConstKind.Transform.Rotation:
                    return new Vector3(gTr.rotation.x, gTr.rotation.y, gTr.rotation.z);
                case ConstKind.Transform.Scale:
                    return new Vector3(gTr.scale.x, gTr.rotation.y, gTr.scale.z);
                default:
                    return new Vector3(0, 0, 0);
            }
        }
    }
}