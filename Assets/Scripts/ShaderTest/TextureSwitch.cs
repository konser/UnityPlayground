using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureSwitch : MonoBehaviour
{
    public Material mat;

    public float checkDist;
    // Start is called before the first frame update
    public GameObject player;
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        mat = this.GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        mat.SetFloat("_Dist", checkDist);
        mat.SetVector("_PlayerPos",player.transform.position);
    }
}
