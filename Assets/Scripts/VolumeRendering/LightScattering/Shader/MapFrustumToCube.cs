#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class MapFrustumToCube : MonoBehaviour
{
    public int coordX;

    public int coordY;

    public int depthZ;

    public Camera mainCam;
    public GameObject testObj;
    public Vector3 froxelVolumePos;
    public float viewDistance;
    public float fovVertical;
    public float aspectRatio;
    public float halfTanFovVertical;

    private Vector3 nearPlaneCenter;
    private Vector3 farPlaneCenter;
    public float nearPlaneWidth;
    public float nearPlaneHeight;
    public float farPlaneWidth;
    public float farPlaneHeight;

    public Vector3 far_bl;
    public Vector3 far_tl;
    public Vector3 far_tr;
    public Vector3 far_br;
    public Vector3 near_bl;
    public Vector3 near_tl;
    public Vector3 near_tr;
    public Vector3 near_br;

    public Matrix4x4 projMatrix;
    // Use this for initialization
    void Start()
    {
        mainCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        CaculateFrustum();
        projMatrix = GetPerspectiveMat();
        froxelVolumePos = projMatrix * mainCam.worldToCameraMatrix * testObj.transform.position;
        float t = (-froxelVolumePos.z - mainCam.nearClipPlane) / viewDistance;
        froxelVolumePos = new Vector3((froxelVolumePos.x / (0.5f * Mathf.Lerp(nearPlaneWidth, farPlaneWidth, t)) + 1) * 0.5f,
            (froxelVolumePos.y / (0.5f * Mathf.Lerp(nearPlaneHeight, farPlaneHeight, t)) + 1) * 0.5f,
            t);
    }

    private void CaculateFrustum()
    {
        if (mainCam == null) return;
        viewDistance = mainCam.farClipPlane - mainCam.nearClipPlane;
        fovVertical = mainCam.fieldOfView;
        aspectRatio = mainCam.aspect;
        nearPlaneCenter = mainCam.transform.position + mainCam.transform.forward * mainCam.nearClipPlane;
        farPlaneCenter = mainCam.transform.position + mainCam.transform.forward * mainCam.farClipPlane;
        halfTanFovVertical = Mathf.Tan((0.5f * fovVertical) * Mathf.Deg2Rad);
        nearPlaneHeight = 2f * halfTanFovVertical * mainCam.nearClipPlane;
        farPlaneHeight = 2f * halfTanFovVertical * mainCam.farClipPlane;
        nearPlaneWidth = nearPlaneHeight * aspectRatio;
        farPlaneWidth = farPlaneHeight * aspectRatio;
        Vector3 camUp = mainCam.transform.up;
        Vector3 camRight = mainCam.transform.right;
        near_bl = -0.5f * nearPlaneWidth * camRight - 0.5f * nearPlaneHeight * camUp + nearPlaneCenter;
        near_tl = -0.5f * nearPlaneWidth * camRight + 0.5f * nearPlaneHeight * camUp + nearPlaneCenter;
        near_tr = 0.5f * nearPlaneWidth * camRight + 0.5f * nearPlaneHeight * camUp + nearPlaneCenter;
        near_br = 0.5f * nearPlaneWidth * camRight - 0.5f * nearPlaneHeight * camUp + nearPlaneCenter;

        far_bl = -0.5f * farPlaneWidth * camRight - 0.5f * farPlaneHeight * camUp + farPlaneCenter;
        far_tl = -0.5f * farPlaneWidth * camRight + 0.5f * farPlaneHeight * camUp + farPlaneCenter;
        far_tr = 0.5f * farPlaneWidth * camRight + 0.5f * farPlaneHeight * camUp + farPlaneCenter;
        far_br = 0.5f * farPlaneWidth * camRight - 0.5f * farPlaneHeight * camUp + farPlaneCenter;
    }

    Matrix4x4 GetPerspectiveMat()
    {
        Matrix4x4 proj = new Matrix4x4(
                new Vector4(1.0f / (halfTanFovVertical * aspectRatio), 0, 0, 0),
                new Vector4(0, 1.0f / halfTanFovVertical, 0, 0),
                new Vector4(0, 0,
                    (-mainCam.nearClipPlane - mainCam.farClipPlane) / -viewDistance,
                    (2.0f * mainCam.farClipPlane * mainCam.nearClipPlane) / -viewDistance),
                new Vector4(0, 0, 1.0f, 0)
            );
        return proj;
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (Application.isPlaying == false) return;
        Handles.color = Color.red;
        Handles.DrawLine(nearPlaneCenter, farPlaneCenter);
        Handles.color = Color.green;
        Handles.DrawLines(new Vector3[] { near_bl, near_tl, near_tr, near_br, far_bl, far_tl, far_tr, far_br },
            new int[]
            {
                0, 1, 1, 2, 2, 3, 3, 0 ,
                4,5,5,6,6,7,7,4,
                0,4,1,5,2,6,3,7
            });
#endif
    }
}
