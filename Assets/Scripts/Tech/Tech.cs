using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RTS/New Tech")]
public class Tech : ScriptableObject
{
    public string title;
    public string description;
    public Material worldButtonMaterial;
    public Sprite worldQueueImage;
}
