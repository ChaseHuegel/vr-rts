using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish
{

public class ConstantMovement : MonoBehaviour
{
    public Vector3 unitsPerSecond;
    public Vector3 variation;
    public bool hasVariation = true;
    public bool bidirectionalVariation = false;

    private Vector3 movement;

    public void Start()
    {
        UpdateMovement();
    }

    public void UpdateMovement()
    {
        movement = unitsPerSecond;

        if (hasVariation)
        {
            if (variation.x > 0) movement.x += Random.Range( bidirectionalVariation ? -variation.x : 0, variation.x );
            if (variation.y > 0) movement.y += Random.Range( bidirectionalVariation ? -variation.y : 0, variation.y );
            if (variation.z > 0) movement.z += Random.Range( bidirectionalVariation ? -variation.z : 0, variation.z );
        }
    }

    private void Update()
    {
        this.transform.Translate(movement * Time.deltaTime, Space.World);
    }
}

}