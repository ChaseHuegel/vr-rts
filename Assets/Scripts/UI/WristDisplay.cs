using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WristDisplay : MonoBehaviour
{
    public TMPro.TextMeshPro woodQuantityTextMesh;
    public TMPro.TextMeshPro grainQuantityTextMesh;
    public TMPro.TextMeshPro goldQuantityTextMesh;
    public TMPro.TextMeshPro stoneQuantityTextMesh;
    public TMPro.TextMeshPro civilianPopulationTextMesh;
    public TMPro.TextMeshPro militaryPopulationTextMesh;
    public TMPro.TextMeshPro totalPopulationTextMesh;
    public void SetWoodText(string text)
    {
        woodQuantityTextMesh.text = text;
    }
    public void SetStoneText(string text)
    {
        stoneQuantityTextMesh.text = text;
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
