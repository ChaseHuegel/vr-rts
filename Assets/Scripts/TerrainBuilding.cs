using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainBuilding : MonoBehaviour
{
    public GameObject buildingStage0;
    public GameObject buildingStage1;
    public GameObject buildingStageFinal;
    public float stage0Duration = 10.0f;
    public float stage1Duration = 10.0f;
    private float timeElapsed;
    private bool TimerStarted;   
    private float stage1EndTime;

    public float TimeElapsed { get { return timeElapsed; } }

    // Start is called before the first frame update
    void Start()
    {
        timeElapsed = 0.0f;
        TimerStarted = true;
        stage1EndTime = stage0Duration + stage1Duration;
    }

    void OnTriggerEnter(Collider other)
    {
        // TerrainBuilding tbOther = other.GetComponent<TerrainBuilding>();

        // if (tbOther != null)
        // {
        //     Destroy(this);
        //     // if (timeElapsed < tbOther.TimeElapsed)
        //     // {
        //     //     Destroy(this);
        //     // }
        // }
    }

    // Update is called once per frame
    void Update()
    {
        if (TimerStarted)
        {
            timeElapsed += Time.deltaTime;
            
            if (timeElapsed >= stage1EndTime)
            {
                buildingStage1.SetActive(false);
                buildingStageFinal.SetActive(true);
                TimerStarted = false;                  
            }
            else if (timeElapsed >= stage0Duration)
            {
                buildingStage0.SetActive(false);  
                buildingStage1.SetActive(true);   
                                        
            }
        }
    }
}
