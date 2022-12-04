using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;
using UnityEngine;

public class Priest : UnitV2
{
    public override bool IsCivilian => false;

    protected override BehaviorTree<ActorV2> BehaviorTreeFactory()
    {
        return PriestBehaviorTree.Value;
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeType.ARMOR, 1f);
        Attributes.AddOrUpdate(AttributeType.HEAL_RATE, 1f);

        audioSource = GetComponent<AudioSource>();
    }

    protected override void OnLoadUnitData(UnitData data)
    {
        base.OnLoadUnitData(data);
        Attributes.AddOrUpdate(AttributeType.ARMOR, unitData.armor);        
    }

    public override void IssueTargetedOrder(Body body)
    {
        switch (body)
        {
            case ActorV2 _:
                Target = body;
                if (body.Faction.IsAllied(Faction) && body is UnitV2)
                    Order = UnitOrder.Heal;
                else
                    Order = UnitOrder.GoTo;
                break;

            case Structure _:
            case Constructible _:
                Target = body;
                    Order = UnitOrder.GoTo;
                break;

            default:
                Target = body;
                Order = UnitOrder.GoTo;
                break;
        }
    }

    

    // protected override void ProcessHealRoutine(float deltaTime)
    // {
    //     base.ProcessHealRoutine(deltaTime);

    //     if (!HealingTarget)
    //     {
    //        // if (currentHealFx)                
    //             // Destroy(currentHealFx);

    //         return;
    //     }

    //     if (!currentHealFx)
    //         currentHealFx = Instantiate(healFxPrefab, Target.transform.position, healFxPrefab.transform.rotation, Target.transform);

    //     if (!audioSource.isPlaying) 
    //         audioSource.Play();

    // }

    // protected override void ProcessHealRoutine(float deltaTime)
    // {
    //     if (!HealingTarget)
    //         return;

    //     HealTimer += deltaTime;
    //     if (HealTimer >= 1f)
    //     {
    //         HealTimer = 0f;

    //         if (Target != null && !Target.Attributes.Get(AttributeType.HEALTH).IsMax() && !IsMoving)
    //         {
    //             // TODO: Should this be changed to be similar to Attack handling
    //             // ie. Repairing/healing is applied by the animation rather than at
    //             // a constant rate (for villagers)
    //             SetAnimatorsTrigger(ActorAnimationTrigger.CAST);
    //             Heal(Target);

    //             if (!currentHealFx)
    //                 currentHealFx = Instantiate(healFxPrefab, Target.transform.position, healFxPrefab.transform.rotation, Target.transform);

    //             if (!audioSource.isPlaying)
    //                 audioSource.Play();
    //         }
    //         else
    //         {
    //             HealingTarget = false;
    //         }
    //     }
    // }

    protected override void OnDamaged(DamageEvent e)
    {
        base.OnDamaged(e);
        // Target = this;
        // Order = UnitOrder.Repair;
    }
}