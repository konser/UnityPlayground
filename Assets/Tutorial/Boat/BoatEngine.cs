using UnityEngine;
using System.Collections;

public class BoatEngine : MonoBehaviour
{
    //Drags
    public Transform waterJetTransform;

    public float steerAngleSpeed = 1f;

    public float steerAngleLimit = 30f;
    //How fast should the engine accelerate?
    public float powerFactor;

    //What's the boat's maximum engine power?
    public float maxPower;

    //The boat's current engine power is public for debugging
    public float currentJetPower;

    private float thrustFromWaterJet = 0f;

    private Rigidbody boatRB;

    private float WaterJetRotation_Y = 0f;

    private float defaultAngle = 0f;
    BoatController boatController;

    void Start()
    {
        boatRB = GetComponent<Rigidbody>();
        defaultAngle = waterJetTransform.localEulerAngles.y;
        boatController = GetComponent<BoatController>();
    }


    void Update()
    {
        UserInput();
    }

    void FixedUpdate()
    {
        UpdateWaterJet();
    }

    void UserInput()
    {
        //Forward / reverse
        if (Input.GetKey(KeyCode.W))
        {
            if (boatController.CurrentSpeed < 50f && currentJetPower < maxPower)
            {
                currentJetPower += 1f * powerFactor * Time.deltaTime;
            }
        }
        else
        {
            currentJetPower = 0f;
        }

        //Steer left
        if (Input.GetKey(KeyCode.A))
        {
            WaterJetRotation_Y = waterJetTransform.localEulerAngles.y + steerAngleSpeed * Time.deltaTime;
            if (WaterJetRotation_Y > (defaultAngle + steerAngleLimit))
            {
                WaterJetRotation_Y = defaultAngle + steerAngleLimit;
            }
            Vector3 newRotation = new Vector3(0f, WaterJetRotation_Y, 0f);

            waterJetTransform.localEulerAngles = newRotation;
        }
        //Steer right
        else if (Input.GetKey(KeyCode.D))
        {
            WaterJetRotation_Y = waterJetTransform.localEulerAngles.y - steerAngleSpeed * Time.deltaTime;
            if (WaterJetRotation_Y < (defaultAngle - steerAngleLimit))
            {
                WaterJetRotation_Y = defaultAngle - steerAngleLimit;
            }
            Vector3 newRotation = new Vector3(0f, WaterJetRotation_Y, 0f);

            waterJetTransform.localEulerAngles = newRotation;
        }
        Debug.DrawRay(waterJetTransform.position, waterJetTransform.forward, Color.blue);
    }

    void UpdateWaterJet()
    {
        //Debug.Log(boatController.CurrentSpeed);

        Vector3 forceToAdd = -waterJetTransform.forward * currentJetPower;

        //Only add the force if the engine is below sea level
        float waveYPos = WaterController.current.GetWaveYPos(waterJetTransform.position, Time.time);

        if (waterJetTransform.position.y < waveYPos)
        {
            boatRB.AddForceAtPosition(forceToAdd, waterJetTransform.position);
        }
        else
        {
            boatRB.AddForceAtPosition(Vector3.zero, waterJetTransform.position);
        }
    }
}