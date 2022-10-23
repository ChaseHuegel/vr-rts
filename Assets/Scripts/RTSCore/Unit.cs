public class Unit
{
    // public virtual void Update()
    // {
    //     if (projectile)
    //         LaunchProjectile();
    // }

    // public virtual void LaunchProjectile(string clipName = "")
    // {
    //     if (!projectile)
    //     {
    //         projectile = Instantiate(rangedProjectile);
    //         projectile.transform.position = transform.position;
    //         projectile.transform.position += new Vector3(0, 0.09f, 0);
    //         projectileTargetPos = targetDamageable.transform.position;
    //         projectileTargetPos += new Vector3(0, 0.09f, 0);

    //         // if (clipName != "")
    //         // audioSource.PlayOneShot(GameMaster.GetAudio(clipName).GetClip());

    //         if (targetDamageable)
    //         {
    //             projectileTargetPos = targetDamageable.transform.position;
    //             projectileTargetPos += new Vector3(0, 0.09f, 0);
    //         }
    //     }

    //     // First we get the direction of the arrow's forward vector to the target position.
    //     Vector3 tDir = projectileTargetPos - projectile.transform.position;

    //     // Now we use a Quaternion function to get the rotation based on the direction
    //     Quaternion rot = Quaternion.LookRotation(tDir);

    //     // And finally, set the arrow's rotation to the one we just created.
    //     projectile.transform.rotation = rot;

    //     //Get the distance from the arrow to the target
    //     float dist = Vector3.Distance(projectile.transform.position, projectileTargetPos);

    //     if (dist <= 0.1f)
    //     {
    //         if (targetDamageable)
    //             targetDamageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, AttributeHandler, DamageType.SLASHING);

    //         // This will destroy the arrow when it is within .1 units
    //         // of the target location. You can set this to whatever
    //         // distance you're comfortable with.
    //         Destroy(projectile);
    //     }
    //     else
    //     {
    //         // If not, then we just keep moving forward
    //         projectile.transform.Translate(Vector3.forward * (projectileSpeed * Time.deltaTime));
    //     }
    // }
}
