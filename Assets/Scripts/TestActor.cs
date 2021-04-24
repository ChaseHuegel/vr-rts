using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;

public class TestActor : Actor
{
    public void Update()
    {
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
