using UnityEngine;

[ExecuteInEditMode]
public class IK6DoF : MonoBehaviour
{
    public GameObject target;
    public Transform[] joints = new Transform[6];
    public float[] lengths = new float[7];
    public float tolerance = 0.01f;
    
    private void Start()
    {
        if (target == null)
        {
            return;
        }
        
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }
        
        SolveIK();
    }

    void SolveIK()
    {
        joints[0].position = transform.position + Vector3.up * lengths[0];
        
        joints[5].position = target.transform.position - target.transform.forward * lengths[6];
        joints[5].rotation = target.transform.rotation;
        // Joint[4]はターゲットの向きに合わせる
        // ターゲットに向かって-(length[5]+lengths[6])の位置にJoint[4]を移動
        joints[4].position = target.transform.position - target.transform.forward * (lengths[5] + lengths[6]);
        joints[4].rotation = target.transform.rotation;

        var p0 = joints[0].position;
        var p2 = joints[2].position;
        var p3 = joints[3].position;
        var p4 = joints[4].position;
        var p5 = joints[5].position;
        
        // Joint[4]の位置からJoint[0]のY軸の角度を求める
        var x40 = p4.x - p0.x;
        var z40 = p4.z - p0.z;
        float theta0;
        if (Mathf.Approximately(x40, 0f) && Mathf.Approximately(z40, 0f))
        {
            theta0 = 0f;
        }
        else
        {
            theta0 = Mathf.Atan2(-z40, x40);
        }
        joints[0].rotation = Quaternion.LookRotation(Vector3.up, p4 - p0);
        
        // Joints[1]の位置を求める
        joints[1].position = p0 + joints[0].forward * lengths[1];
        var p1 = joints[1].position;
        
        var l14 = Mathf.Sqrt(Mathf.Pow(p4.x - p1.x, 2) + 
                             Mathf.Pow(p4.y - p1.y, 2) +
                             Mathf.Pow(p4.z - (lengths[0] + lengths[1]), 2));
        var theta2 = Mathf.Acos(-(Mathf.Pow(lengths[2], 2) + Mathf.Pow(lengths[3] + lengths[4], 2) - Mathf.Pow(l14, 2)) / (2 * lengths[2] * (lengths[3] + lengths[4])));

        var theta1_1 = Mathf.Acos((Mathf.Pow(lengths[2],2) + Mathf.Pow(l14, 2) - Mathf.Pow(lengths[3] + lengths[4], 2)) / (2 * lengths[2] * l14));
        var l04 = Mathf.Sqrt(Mathf.Pow(p4.x - p1.x, 2) + Mathf.Pow(p4.y - p1.y, 2) + Mathf.Pow(p4.z - lengths[0], 2));
        var theta1_2 = Mathf.Acos((Mathf.Pow(lengths[1], 2) + Mathf.Pow(l14, 2) - Mathf.Pow(l04, 2)) / (2 * lengths[1] * l14));
        var theta1 = Mathf.PI - (theta1_1 + theta1_2);
        joints[1].rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * theta1, Vector3.right);
        
        // Joints[2]の位置を求める
        var rot = joints[1].rotation * joints[0].rotation;
        joints[2].position = p1 + rot * Vector3.forward * lengths[2];
    }

    void OnDrawGizmos()
    {
        if (target == null)
        {
            return;
        }
        
        for (var i = 0; i < joints.Length; i++)
        {
            var from = joints[i].position;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(from, from + joints[i].right * 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(from, from + joints[i].up * 0.1f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(from, from + joints[i].forward * 0.1f);
            
        }
    }
}
