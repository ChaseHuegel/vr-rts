using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WristDisplay : MonoBehaviour
{
    public TextMesh woodQuantityTextMesh;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetWoodText(string text)
    {
        woodQuantityTextMesh.text = text;
    }
}
