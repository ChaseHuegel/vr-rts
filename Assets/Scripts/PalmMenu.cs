using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PalmMenu : MonoBehaviour
{
    public Hand leftHand;
    public Hand rightHand;

    Hand menuHand;
    Hand selectionHand;

    public GameObject prefabCube;
    public GameObject prefabSphere;

    // Start is called before the first frame update
    void Start()
    {
        menuHand = leftHand;
        selectionHand = rightHand;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnCube()
    {
        GameObject a = Instantiate(prefabCube) as GameObject;

        selectionHand.AttachObject(a, GrabTypes.Trigger, Hand.AttachmentFlags.ParentToHand |
                                                              Hand.AttachmentFlags.DetachOthers |
                                                              Hand.AttachmentFlags.DetachFromOtherHand |
                                                              Hand.AttachmentFlags.TurnOnKinematic |
                                                              Hand.AttachmentFlags.TurnOffGravity |
                                                              Hand.AttachmentFlags.SnapOnAttach);
    }

    public void SpawnSphere()
    {
        GameObject a = Instantiate(prefabSphere) as GameObject;
        selectionHand.AttachObject(a, GrabTypes.Trigger, Hand.AttachmentFlags.ParentToHand |
                                                              Hand.AttachmentFlags.DetachOthers |
                                                              Hand.AttachmentFlags.DetachFromOtherHand |
                                                              Hand.AttachmentFlags.TurnOnKinematic |
                                                              Hand.AttachmentFlags.TurnOffGravity |
                                                              Hand.AttachmentFlags.SnapOnAttach);
    }
}
