using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network;


public class Dodge_Chellenger_SRT10_FBX : MonoBehaviour
{
    [SerializeField] private Transform intersecCheckTransform = null;
    [SerializeField] private LayerMask dodgeMask;
    [SerializeField] private GameObject carGameObject;
    private Rigidbody carRigidbody;
    // Start is called before the first frame updat
    CarVision _carVision = null;
    void Start()
    {
        _carVision = new CarVision(carGameObject, GameObject.Find("Level1"), intersecCheckTransform, dodgeMask);
        carRigidbody = carGameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        

    }

    private void FixedUpdate()
    {   
        _carVision.setCarVelocity(carRigidbody.velocity.magnitude);
        _carVision.updateObserving();
    }
}
