using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceChangedListener : MonoBehaviour
{
    [Header("Target Text Compontents")]
    public TMPro.TextMeshPro woodQuantityText;
    public TMPro.TextMeshPro foodQuantityText;
    public TMPro.TextMeshPro goldQuantityText;
    public TMPro.TextMeshPro stoneQuantityText;
    public TMPro.TextMeshPro civilianPopulationText;
    public TMPro.TextMeshPro militaryPopulationText;
    public TMPro.TextMeshPro totalPopulationText;

    private int populationLimit;

    void Awake() { HookIntoEvents(); }

    private void HookIntoEvents()
    {
        if (stoneQuantityText)
            PlayerManager.Instance.OnStoneResourceChanged += OnStoneResourceChanged;
        
        if (foodQuantityText)
            PlayerManager.Instance.OnFoodResourceChanged += OnFoodResourceChanged;
        
        if (goldQuantityText)
            PlayerManager.Instance.OnGoldResourceChanged += OnGoldResourceChanged;
        
        if (woodQuantityText)
            PlayerManager.Instance.OnWoodResourceChanged += OnWoodResourceChanged;

        if (totalPopulationText)
        {
            PlayerManager.Instance.OnPopulationChanged += OnPopulationChanged;
            PlayerManager.Instance.OnPopulationLimitChanged += OnPopulationLimitChanged;
        }
    }

    private void CleanupEvents()
    {
        if (PlayerManager.Instance == null)
            return;
            
        if (stoneQuantityText)
            PlayerManager.Instance.OnStoneResourceChanged -= OnStoneResourceChanged;

        if (foodQuantityText)
            PlayerManager.Instance.OnFoodResourceChanged -= OnFoodResourceChanged;
        
        if (goldQuantityText)
            PlayerManager.Instance.OnGoldResourceChanged -= OnGoldResourceChanged;

        if (woodQuantityText)
            PlayerManager.Instance.OnWoodResourceChanged -= OnWoodResourceChanged;

        if (totalPopulationText)
        {
            PlayerManager.Instance.OnPopulationChanged -= OnPopulationChanged;
            PlayerManager.Instance.OnPopulationLimitChanged -= OnPopulationLimitChanged;
        }
    }

    private void OnPopulationChanged(object obj, PlayerManager.PopulationChangedEvent e)
    {
        if (civilianPopulationText)
            civilianPopulationText.text = e.civilianPopulation.ToString();

        if (militaryPopulationText)
            militaryPopulationText.text = e.militaryPopulation.ToString();

        
        if (totalPopulationText)
            totalPopulationText.text = e.totalPopulation.ToString() + "/" + populationLimit.ToString();
    }

    private void OnPopulationLimitChanged(object obj, PlayerManager.PopulationLimitChangedEvent e)
    {
        populationLimit = e.newValue;
        

        if (totalPopulationText)
            totalPopulationText.text = e.totalPopulation.ToString() + "/" + populationLimit.ToString();
    }

    private void OnStoneResourceChanged(object obj, PlayerManager.StoneResourceChangedEvent e)
    {
        if (stoneQuantityText)
            stoneQuantityText.text = e.newValue.ToString();
    }

    private void OnWoodResourceChanged(object obj, PlayerManager.WoodResourceChangedEvent e)
    {
        if (woodQuantityText)
            woodQuantityText.text = e.newValue.ToString();
    }
    private void OnGoldResourceChanged(object obj, PlayerManager.GoldResourceChangedEvent e)
    {
        if (goldQuantityText)
            goldQuantityText.text = e.newValue.ToString();
    }
    private void OnFoodResourceChanged(object obj, PlayerManager.FoodResourceChangedEvent e)
    {
        if (foodQuantityText)
            foodQuantityText.text = e.newValue.ToString();
    }

    void OnDestroy()
    {
        CleanupEvents();
    }


}
