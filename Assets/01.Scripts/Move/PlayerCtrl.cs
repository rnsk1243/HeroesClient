using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using NamespaceConstKinds;


public class PlayerCtrl : MonoBehaviour {

    private float h = 0.0f;
    private float v = 0.0f;

    private Transform tr;
    public float moveSpeed = 10.0f;
    public Vector3[] deltaPosition;
    public bool isDeltaPosition = false;
    //private Socket mSocket;
    // Use this for initialization
    void Awake () {
        tr = GetComponent<Transform>();
        deltaPosition = new Vector3[2];
    }

    void Start()
    {
        StartCoroutine("RecordPosition");
    }

    bool isDifferent(float oldPos, float newPos)
    {
        float delta = oldPos - newPos;
        if (delta > ConstKind.DeltaPositionSend || delta < -ConstKind.DeltaPositionSend)
        {
            isDeltaPosition = true;
            return true;
        }
        else
        {
            isDeltaPosition = false;
            return false;
        }
    }

    IEnumerator RecordPosition()
    {
        Vector3 newPosition = tr.position;
        Vector3 oldPos = deltaPosition[ConstKind.NewData];
        deltaPosition[ConstKind.NewData] = newPosition;
        deltaPosition[ConstKind.OldData] = oldPos;

        isDifferent(deltaPosition[ConstKind.OldData].x, deltaPosition[ConstKind.NewData].x);
        isDifferent(deltaPosition[ConstKind.OldData].y, deltaPosition[ConstKind.NewData].y);
        isDifferent(deltaPosition[ConstKind.OldData].z, deltaPosition[ConstKind.NewData].z);

        yield return new WaitForSeconds(0.3f);
    }

    // Update is called once per frame
    void Update ()
    {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        Vector3 moveDir = (Vector3.forward * v) + (Vector3.right * h);
        tr.Translate(moveDir * Time.deltaTime * moveSpeed, Space.Self);
        
    }
}
