public class Villager : Unit
{
    /*
    public override void OnHandHoverBegin(Hand hand)
    {
        base.OnHandHoverBegin(hand);
        handCommandMenu.Show();
    }

    public override void OnHandHoverEnd(Hand hand)
    {
        base.OnHandHoverEnd(hand);
        handCommandMenu.Hide();
    }

    public override void OnAttachedToHand(Hand hand)
    {
        base.OnAttachedToHand(hand);
        handCommandMenu.Show();
    }

    public override void OnDetachedFromHand(Hand hand)
    {
        base.OnDetachedFromHand(hand);
        handCommandMenu.Hide();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (!wasThrownOrDropped)
            return;

        // TODO: could just switch this to a cell lookup where
        // TODO: they land.
        // Don't wait for a collision indefinitely.
        if (Time.time - detachFromHandTime >= 2.0f)
        {
            wasThrownOrDropped = false;
            return;
        }

        Unfreeze();

        Resource resource = collider.gameObject.GetComponent<Resource>();
        if (resource)
        {
            AssignUnitToResourceTask(resource);
            return;
        }

        Fauna fauna = collider.gameObject.GetComponent<Fauna>();
        if (fauna)
        {
            AssignUnitToFaunaTask(fauna);
            return;
        }

        Structure structure = collider.gameObject.GetComponentInParent<Structure>();
        if (structure)
        {
            AssignUnitToStructureTask(structure);
            return;
        }

        Constructible constructible = collider.gameObject.GetComponentInParent<Constructible>();
        if (constructible)
        {
            AssignUnitToConstructibleTask(constructible);
            return;
        }
    }
    */
}
