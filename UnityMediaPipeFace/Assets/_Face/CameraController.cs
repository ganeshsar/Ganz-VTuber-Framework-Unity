using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Hacky camera system I wrote up to improve presentation of the avatar.

    public AvatarFace lookTarget;
    public float toleranceRadius = .5f;
    public float followSpeed = 25f;

    private Vector3 ogPosition;
    private Vector3 lookTargetInitial => lookTarget.initialTrackPosition;

    private void Start()
    {
        ogPosition = transform.position;
    }

    private void LateUpdate()
    {
        float d = (transform.position - lookTarget.TPosition).magnitude - toleranceRadius;
        float dy = Mathf.Lerp(0, .05f, Mathf.InverseLerp(-.1f, -.4f, d));
        if (d<0f)
        {
            Vector3 p = (lookTarget.TPosition - lookTargetInitial)*1.1f;
            p.z *= 0.05f;
            p.x *=.1f;
            p.y += dy*.5f;
            p.z -= dy*2.3f;
            transform.position = Vector3.Lerp(transform.position, ogPosition+p, Time.deltaTime * followSpeed / 2f); ;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, ogPosition, Time.deltaTime * followSpeed/2f);

        }
    }
}
