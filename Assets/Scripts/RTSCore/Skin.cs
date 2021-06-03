using Swordfish;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skin", menuName = "RTS/Skin")]
public class Skin : ScriptableObject
{
    public byte index;
    public Color color = Color.blue;
    public Material buildingMaterial;
    public Material unitMaterial;

    public GameObject bannerObject;
}
