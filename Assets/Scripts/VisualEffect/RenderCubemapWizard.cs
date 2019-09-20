using UnityEngine;
using UnityEditor;
using System.Collections;

public class RenderCubemapWizard : ScriptableWizard
{
    public BoxCollider collider;
    public Cubemap cubemap;
    public Shader alpahExtractShader;
    void OnWizardUpdate()
    {
        string helpString = "Select transform to render from and cubemap to render into";
        bool isValid = (collider != null) && (cubemap != null);
    }

    void OnWizardCreate()
    {
        // create temporary camera for rendering
        GameObject go = new GameObject("RenderCubemapCam");
        Camera cam =  go.AddComponent<Camera>();
        if (alpahExtractShader != null)
        {
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = Color.white;
            CameraReplacement camRep = go.AddComponent<CameraReplacement>();
            camRep.ReplacementShader = alpahExtractShader;
            camRep.enabled = false;
            camRep.enabled = true;
        }
        // place it on the object
        go.transform.position = collider.bounds.center;
        go.transform.rotation = Quaternion.identity;
        // render into cubemap
        go.GetComponent<Camera>().RenderToCubemap(cubemap);

        // destroy temporary camera
        //DestroyImmediate(go);
        AssetDatabase.Refresh();
        Debug.Log("Complete!");
    }

    [MenuItem("GameObject/Render into Cubemap")]
    static void RenderCubemap()
    {

        ScriptableWizard.DisplayWizard<RenderCubemapWizard>(
            "Render cubemap", "Render!");
    }
}