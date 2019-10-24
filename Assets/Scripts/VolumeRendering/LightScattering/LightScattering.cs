using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class LightScattering : MonoBehaviour
{
    public Shader effectShader;
    public Transform lightPos;
    private Material _effectMaterial;
    public Material effectMaterial
    {
        get
        {
            if (!_effectMaterial && effectShader)
            {
                _effectMaterial = new Material(effectShader);
                _effectMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _effectMaterial;
        }
    }

    private Camera _currentCamera;
    public Camera currentCamera
    {
        get
        {
            if (!_currentCamera)
            {
                _currentCamera = GetComponent<Camera>();
            }

            return _currentCamera;
        }
    }


    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        if (!effectMaterial)
        {
            Graphics.Blit(source, dest);
            return;
        }
        effectMaterial.SetMatrix("_FrustumCornersEyeSpace", GetFrustumCorners(currentCamera));
        effectMaterial.SetMatrix("_CameraInvViewMatrix", currentCamera.cameraToWorldMatrix);
        effectMaterial.SetVector("_CameraWorldPos", currentCamera.transform.position);
        effectMaterial.SetVector("_LightPos",lightPos.position);
        CustomGraphicsBlit(source, dest, effectMaterial, 0);
        //CustomGraphicsBlit(source, dest, effectMaterial, 1);
        //CustomGraphicsBlit(source, dest, effectMaterial, 2);
        //CustomGraphicsBlit(source, dest, effectMaterial, 3);
        //CustomGraphicsBlit(source, dest, effectMaterial, 4);
    }

    private static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material mat, int pass)
    {
        RenderTexture.active = dest;
        mat.SetTexture("_MainTex", source);
        GL.PushMatrix();
        GL.LoadOrtho();
        mat.SetPass(pass);
        // 将屏幕空间的四个角的索引作为z传入
        // 因为在正交投影下，z轴不会被用到
        GL.Begin(GL.QUADS);
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

        GL.End();
        GL.PopMatrix();
    }

    private Matrix4x4 GetFrustumCorners(Camera cam)
    {
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;
        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovHalf = camFov * 0.5f;
        float tanFov = Mathf.Tan(fovHalf * Mathf.Deg2Rad);
        Vector3 rightHalf = Vector3.right * tanFov * camAspect;
        Vector3 topHalf = Vector3.up * tanFov;
        Vector3 topLeft = (-Vector3.forward - rightHalf + topHalf);
        Vector3 topRight = (-Vector3.forward + rightHalf + topHalf);
        Vector3 botRight = (-Vector3.forward + rightHalf - topHalf);
        Vector3 botLeft = (-Vector3.forward - rightHalf - topHalf);

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, botRight);
        frustumCorners.SetRow(3, botLeft);

        return frustumCorners;
    }
}
