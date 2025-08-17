using UnityEngine;

// This follow point is actually something I want to
//    be imported into every file by default, so
//    we just simply don't give it a namespace
//    (Yea, i know...)

public class FollowPoint : MonoBehaviour
{
    public void Initialize(Transform target)
    {
        transform.SetParent(target);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}

// POP QUIZ #1 -
// Is the answer A, Ass, B, or Sus?
