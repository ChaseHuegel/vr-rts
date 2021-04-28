using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WristDisplay : MonoBehaviour
{
    public TextMesh woodQuantityTextMesh;
    public TextMesh grainQuantityTextMesh;
    public TextMesh goldQuantityTextMesh;

    public void SetWoodText(string text)
    {
        woodQuantityTextMesh.text = text;
    }
    public void SetGoldText(string text)
    {
        goldQuantityTextMesh.text = text;
    }
    public void SetGrainText(string text)
    {
        grainQuantityTextMesh.text = text;
    }
}
