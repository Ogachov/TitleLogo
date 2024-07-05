using UnityEngine;

// [ExecuteInEditMode]
public class IK6DoF : MonoBehaviour
{
    public GameObject target;
    public Transform[] joints = new Transform[6];
    public float[] lengths = new float[6];
    public float tolerance = 0.01f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (target == null)
        {
            return;
        }
        
        for (int i = 0; i < 6; i++)
        {
            lengths[i] = Vector3.Distance(joints[i].position, joints[i + 1].position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
        {
            return;
        }
        
        SolveIK();
    }

    void SolveIK()
    {
        Vector3 targetPosition = target.transform.position;
        for (int i = joints.Length - 2; i >= 0; i--)
        {
            Vector3 toTarget = targetPosition - joints[i].position;
            float distance = toTarget.magnitude;
            float length = lengths[i];

            // 回転角度を計算
            float angle = Mathf.Acos(Mathf.Clamp(length / distance, -1f, 1f));
            Vector3 axis = Vector3.Cross(joints[i + 1].position - joints[i].position, toTarget).normalized;
            joints[i].rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * angle, axis) * joints[i].rotation;

            // ジョイントの新しい位置を計算
            joints[i + 1].position = joints[i].position + joints[i].rotation * Vector3.forward * length;

            targetPosition = joints[i + 1].position;
        }

        for (int i = 1; i < joints.Length; i++)
        {
            Vector3 toNext = joints[i].position - joints[i - 1].position;
            float distance = toNext.magnitude;
            float length = lengths[i - 1];

            if (distance > length)
            {
                Vector3 direction = toNext.normalized;
                joints[i].position = joints[i - 1].position + direction * length;
            }
            else
            {
                joints[i].position = joints[i - 1].position + toNext;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (target == null)
        {
            return;
        }
        
        Vector3[] positions = new Vector3[7];
        positions[0] = transform.position;
        for (int i = 0; i < 6; i++)
        {
            positions[i + 1] = joints[i].position;
        }
        positions[6] = target.transform.position;
        
        for (int i = 0; i < 6; i++)
        {
            Gizmos.color = i % 2 == 0 ? Color.red : Color.magenta;
            Gizmos.DrawLine(positions[i], positions[i + 1]);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawLine(positions[6], positions[5]);
    }
}
