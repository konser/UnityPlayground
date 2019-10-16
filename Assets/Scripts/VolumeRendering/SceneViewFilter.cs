using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class SceneViewFilter : MonoBehaviour
{
#if UNITY_EDITOR
    private bool hasChanged = false;

    public virtual void OnValidate()
    {
        hasChanged = true;
    }

    static SceneViewFilter()
    {
        SceneView.duringSceneGui += CheckSceneChanged;
    }

    static void CheckSceneChanged(SceneView sv)
    {
        if (Event.current.type != EventType.Layout)
        {
            return;
        }

        if (!Camera.main)
        {
            return;
        }

        SceneViewFilter[] sceneFilters =sv.camera.GetComponents<SceneViewFilter>();
        SceneViewFilter[] cameraeFilters = Camera.main.GetComponents<SceneViewFilter>();

        if (cameraeFilters.Length != sceneFilters.Length)
        {
            //Recreat all
            Recreate(sv);
            return;
        }

        for (int i = 0; i < cameraeFilters.Length; i++)
        {
            if (cameraeFilters[i].GetType() != sceneFilters[i].GetType())
            {
                // Recreate
                Recreate(sv);
                return;
            }
        }

        for (int i = 0; i < cameraeFilters.Length; i++)
        {
            if (cameraeFilters[i].hasChanged || sceneFilters[i].enabled != cameraeFilters[i].enabled)
            {
                EditorUtility.CopySerialized(cameraeFilters[i],sceneFilters[i]);
                cameraeFilters[i].hasChanged = false;
            }
        }
    }

    static void Recreate(SceneView sv)
    {
        SceneViewFilter filter;
        while (filter = sv.camera.GetComponent<SceneViewFilter>())
        {
            DestroyImmediate(filter);
        }

        foreach (SceneViewFilter f in Camera.main.GetComponents<SceneViewFilter>())
        {
            SceneViewFilter newFilter =  sv.camera.gameObject.AddComponent(f.GetType()) as SceneViewFilter;
            EditorUtility.CopySerialized(f,newFilter);
        }
    }
#endif
}