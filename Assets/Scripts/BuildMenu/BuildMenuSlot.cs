using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildMenuSlot : MonoBehaviour
{
    public GameObject menuSlotObject;
    public void RespawnMenuSlotObject()
    {
        GameObject spawned = GameObject.Instantiate(menuSlotObject, this.gameObject.transform);         
    }    
}
