using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Resource Element", menuName = "RTS/Resources/Resource Element")]
public class ResourceElement : ScriptableObject
{
    [SerializeField] private GameObject[] variants;

    public GameObject GetVariant()
    {
        return variants[
            variants.Length > 1 ? Random.Range(0, variants.Length) : 0
        ];
    }
}