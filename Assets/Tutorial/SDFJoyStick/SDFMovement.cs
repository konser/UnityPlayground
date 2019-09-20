using System;
using UnityEngine;
using System.Collections;
using UnityEditor;

public class SDFMovement : MonoBehaviour
{
    public float sampleValue;
    public int mapWidth;
    public int textureSize;
    public float playerRadius = 2f;
    public float speed = 3f;
    private Texture2D _edtTexture;
    private float _gridLength;
    private float _maxDistance;
    private void Awake()
    {
        _edtTexture = Resources.Load<Texture2D>("SDF");
        _gridLength = (float)mapWidth / textureSize;
        _maxDistance = Mathf.Sqrt(mapWidth * mapWidth * 2);
    }


    public float Sample(Vector3 pos)
    {
        pos = pos / _gridLength;
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.z);
        float dx = pos.x - x;
        float dy = pos.z - y;
        float b1 = _edtTexture.GetPixel(x, y).r * _maxDistance;
        float b2 = _edtTexture.GetPixel(x + 1, y).r * _maxDistance;
        float t1 = _edtTexture.GetPixel(x, y+1).r * _maxDistance;
        float t2 = _edtTexture.GetPixel(x+1, y+1).r * _maxDistance;
        return (b1 + (b2 - b1) * dx) + dy * (t1 + (t2 - t1) * dx - (b1+(b2-b1)*dx));
    }

    public Vector3 Gradient(Vector3 pos)
    {
        float delta = 1f;
        float x = pos.x;
        float y = pos.z;
        float div = 1 / delta;

        Vector3 gradient = div * new Vector3(
                               Sample(new Vector3(x + delta, 0, y)) - Sample(new Vector3(x - delta, 0, y)),
                               0,
                               Sample(new Vector3(x, 0, y + delta)) - Sample(new Vector3(x, 0, y - delta))
                               );
        Debug.DrawRay(pos,gradient,Color.red,Time.deltaTime);
        return gradient;
    }

    public Vector3 GetValidPosBySDF(Vector3 pos, Vector3 dir, float speed)
    {
        Vector3 newPos = pos + dir * speed;
        float sample = Sample(newPos);
        if (sample == 0)
        {

        }
        else if (sample < playerRadius)
        {
            Vector3 grad = Gradient(newPos);
            Vector3 adjustDir = dir - grad * Vector3.Dot(grad, dir);
            newPos = pos + adjustDir.normalized * speed;
            for (int i = 0; i < 3; i++)
            {
                sample = Sample(newPos);
                if(sample > playerRadius) break;
                newPos += Gradient(newPos) * (playerRadius - sample);
            }
            // 抖动处理
            //if (Vector3.Dot(newPos - pos, dir) < 0)
            //{
            //    newPos = pos;
            //}
        }
        //Debug.DrawRay(pos, (newPos - pos)*playerRadius, Color.green, Time.deltaTime);
        return newPos;
    }

    private void Update()
    {
        sampleValue = Sample(transform.position);
        Gradient(transform.position);
        //Vector3 newPos =  GetValidPosBySDF(transform.position, transform.forward.normalized, speed*Time.deltaTime);
        //Vector3 realDir = (newPos - transform.position).normalized;
        //transform.position += realDir * speed * Time.deltaTime;

    }


}
