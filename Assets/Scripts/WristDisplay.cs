using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WristDisplay : MonoBehaviour
{
    public TextMesh woodQuantityTextMesh;
    public TextMesh grainQuantityTextMesh;
    public TextMesh goldQuantityTextMesh;
    public TextMesh civilianPopulationTextMesh;
    public TextMesh militaryPopulationTextMesh;
    public TextMesh totalPopulationTextMesh;
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

    public void SetCivilianPopulationText(string text)
    {
        civilianPopulationTextMesh.text = text;
    }

    public void SetMilitaryPopulationText(string text)
    {
        militaryPopulationTextMesh.text = text;
    }

    public void SetTotalPopulationText(string text)
    {
        totalPopulationTextMesh.text = text;
    }
}
