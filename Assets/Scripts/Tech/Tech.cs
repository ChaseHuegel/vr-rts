using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Core/new Tech")]
public class Tech : ScriptableObject
{
    public string title;
    public string description;
    public Texture2D worldButtonImage;
    public Texture2D iconImage;
}
