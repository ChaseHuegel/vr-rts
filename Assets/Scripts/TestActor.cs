using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;

public class TestActor : Actor
{
    public void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     ToggleFreeze();
        // }

        // if (Input.GetMouseButtonDown(0))
        // {
        //     Vector3 target = Camera.main.ScreenToWorldPoint( new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0) );

        //     target = World.ToWorldSpace(target);

        //     Goto( (int)target.x, (int)target.z );
        // }
    }

    public override void Tick()
    {
        //  Choose a random path if we don't have one
        if (HasValidPath() == false)
        {
            Goto( Random.Range(0, World.GetGridSize()), Random.Range(0, World.GetGridSize()) );
        }
    }
}
