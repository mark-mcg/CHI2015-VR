using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class OVRSubCamera : MonoBehaviour
{
    /// <summary>
    /// The OVRCamera that will render after this OVRSubCamera.
    /// </summary>
    public OVRCamera Parent;

    Vector3 StartPosition;
    Quaternion StartRotation;

    void Start()
    {
        if (Parent.transform.parent == null)
            return;

        //StartPosition = transform.parent.localPosition - Parent.transform.parent.localPosition;
        //StartRotation = transform.parent.localRotation * Quaternion.Inverse(Parent.transform.parent.localRotation);
    }

    void OnPreCull()
    {
        if (Parent == null || camera == null)
            return;

        if (transform.parent != null && Parent.transform.parent != null)
        {
            //transform.parent.localPosition = StartPosition + Parent.transform.parent.localPosition;
            //transform.parent.localRotation = StartRotation * Parent.transform.parent.localRotation;
        }

        transform.localPosition = Parent.transform.localPosition;
        transform.localRotation = Parent.transform.localRotation;
        transform.localScale = Parent.transform.localScale;

        camera.fieldOfView = Parent.camera.fieldOfView;
        Matrix4x4 projMatrix = Matrix4x4.identity;
        OVRDevice.GetCameraProjection(Parent.EyeId, camera.nearClipPlane, camera.farClipPlane, ref projMatrix);
        camera.projectionMatrix = projMatrix;
        camera.targetTexture = Parent.camera.targetTexture;
        camera.rect = Parent.camera.rect;
    }
}
