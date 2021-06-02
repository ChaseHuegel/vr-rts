using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish
{

public class SelfDestructTimer : MonoBehaviour
{
    public float maxLifetime = 1.0f;

    private void Start()
    {
        Destroy(this.gameObject, maxLifetime);
    }
}

}