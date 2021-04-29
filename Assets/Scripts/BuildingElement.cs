using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Building Element", menuName = "RTS/Buildings/Building Element")]
public class BuildingElement : ScriptableObject
{
    [SerializeField] private GameObject[] variants;

    public GameObject GetVariant()
    {
        return variants[
            variants.Length > 1 ? Random.Range(0, variants.Length) : 0
        ];
    }
}