using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1)]
public class Snake : MonoBehaviour
{
    // Start is called before the first frame update
    private Snake _parent;
    private Snake _child;
    private Matrix4x4 _startLocalTransform;
    private Matrix4x4 _lastWorldMatrix;
    public float PositionDamping = 0.5f;
    public float RotationDamping = 0.05f;
    void Start()
    {
        _lastWorldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        _startLocalTransform = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
        _parent = transform.parent?.GetComponent<Snake>();
        _child = 
            transform.childCount > 0 ? 
            transform.GetChild(0).GetComponent<Snake>() : 
            null;
    }

    void Update()
    {
        if(_parent == null) UpdateSelf();
    }

    void UpdateSelf()
    {
        UpdatePositionAndRotation();

        _child?.UpdateSelf();
    }
    private void UpdatePositionAndRotation()
    {
        if (_parent == null) return;

        var preTransformLocalTransform = transform.parent.worldToLocalMatrix * _lastWorldMatrix;
        transform.localRotation = Quaternion.Slerp(preTransformLocalTransform.rotation, _startLocalTransform.rotation, RotationDamping);
        transform.localPosition = Vector3.Lerp(preTransformLocalTransform.MultiplyPoint(Vector3.zero), _startLocalTransform.MultiplyPoint(Vector3.zero), PositionDamping);

        _lastWorldMatrix = transform.localToWorldMatrix;
    }
}
