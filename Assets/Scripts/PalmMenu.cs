using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PalmMenu : MonoBehaviour
{
    // public Hand leftHand;
    // public Hand rightHand;

    Hand menuHand;
    Hand selectionHand;

    public GameObject prefabCube;
    public GameObject prefabSphere;
    public GameObject prefabMage;

    public GameObject prefabKnight;
    public GameObject prefabFatKnight;
    
    // Start is called before the first frame update
    void Start()
    {
        menuHand = Player.instance.leftHand;
        selectionHand = Player.instance.rightHand;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnCube()
    {
        GameObject a = Instantiate(prefabCube) as GameObject;
        selectionHand.AttachObject(a, GrabTypes.Scripted);
    }

    public void SpawnSphere()
    {
        GameObject a = Instantiate(prefabSphere) as GameObject;
        selectionHand.AttachObject(a, GrabTypes.Scripted);
    }

    public void SpawnMage()
    {
        GameObject a = Instantiate(prefabMage) as GameObject;
        selectionHand.AttachObject(a, GrabTypes.Scripted);
    }

    public void SpawnKnight()
    {
        GameObject a = Instantiate(prefabKnight) as GameObject;
        selectionHand.AttachObject(a, GrabTypes.Scripted);
    }
    public void SpawnFatKnight()
    {
        GameObject a = Instantiate(prefabFatKnight) as GameObject;
        selectionHand.AttachObject(a, GrabTypes.Scripted);
    }
}
