using System;
using Swordfish.Library.Collections;
using UnityEngine;

namespace Swordfish
{
    public class Damageable : MonoBehaviour
    {
        public event EventHandler<DamageEvent> OnDamageEvent;
        public class DamageEvent : Event
        {
            public AttributeChangeCause cause;
            public Damageable victim;
            public Damageable attacker;
            public DamageType type;
            public float damage;
        }

        public event EventHandler<HealthRegainEvent> OnHealthRegainEvent;
        public class HealthRegainEvent : Event
        {
            public AttributeChangeCause cause;
            public Damageable target;
            public Damageable healer;
            public float amount;
            public float health;
        }

        public static event EventHandler<SpawnEvent> OnSpawnEvent;
        public class SpawnEvent : Event
        {
            public Damageable target;
        }

        public static event EventHandler<DeathEvent> OnDeathEvent;
        public class DeathEvent : Event
        {
            public AttributeChangeCause cause;
            public Damageable victim;
            public Damageable attacker;
        }

        public ValueFieldCollection Attributes { get; set; } = new();

        [SerializeField]
        protected bool Invulnerable = false;

        [SerializeField]
        protected DamageType[] Weaknesses = new DamageType[0];

        [SerializeField]
        protected DamageType[] Resistances = new DamageType[0];

        [SerializeField]
        protected DamageType[] Immunities = new DamageType[0];

        protected virtual void Start()
        {
            Attributes.GetOrAdd(AttributeConstants.HEALTH, 100f, 100f);

            SpawnEvent e = new()
            {
                target = this
            };
            OnSpawnEvent?.Invoke(this, e);

            //  destroy this object if the event has been cancelled
            if (e.cancel)
                Destroy(gameObject);
        }

        public bool IsAlive() => Attributes.ValueOf(AttributeConstants.HEALTH) > 0;
        public float GetHealth() => Attributes.ValueOf(AttributeConstants.HEALTH);
        public float GetMaxHealth() => Attributes.MaxValueOf(AttributeConstants.HEALTH);
        public float GetHealthPercent() => Attributes.CalculatePercentOf(AttributeConstants.HEALTH);

        public void Damage(float damage, AttributeChangeCause cause = AttributeChangeCause.FORCED, Damageable attacker = null, DamageType type = DamageType.NONE)
        {
            //  Invoke a damage event
            DamageEvent e = new()
            {
                cause = cause,
                victim = this,
                attacker = attacker,
                type = type,
                damage = damage
            };
            OnDamageEvent?.Invoke(this, e);

            //  return if the event has been cancelled by any subscriber
            if (e.cancel)
                return;

            //  Check for immunity
            for (int i = 0; i < Immunities.Length; i++)
            {
                if (e.type == Immunities[i])
                {
                    e.damage = 0;
                    break;
                }
            }

            //  Modify any damage by any weaknesses or resistances
            if (e.damage > 0 && e.type != DamageType.NONE)
            {
                for (int i = 0; i < Weaknesses.Length; i++)
                {
                    if (e.type == Weaknesses[i])
                    {
                        e.damage *= 2;
                        break;
                    }
                }

                for (int i = 0; i < Resistances.Length; i++)
                {
                    if (e.type == Resistances[i])
                    {
                        e.damage /= 2;
                        break;
                    }
                }
            }

            Attributes.Get(AttributeConstants.HEALTH).Remove(e.damage);

            //  If the damage was enough to kill, invoke a death event
            if (Attributes.ValueOf(AttributeConstants.HEALTH) == 0)
            {
                DeathEvent e2 = new()
                {
                    cause = cause,
                    victim = this,
                    attacker = attacker
                };
                OnDeathEvent?.Invoke(this, e2);

                //  return if the event has been cancelled by any subscriber
                if (e2.cancel)
                    return;

                Destroy(gameObject);
            }
        }

        public void Heal(float amount, AttributeChangeCause cause = AttributeChangeCause.FORCED, Damageable healer = null)
        {
            if (Attributes.CalculatePercentOf(AttributeConstants.HEALTH) == 1.0f)
                return;

            //  Invoke a health regain event
            HealthRegainEvent e = new()
            {
                cause = cause,
                target = this,
                healer = healer,
                amount = amount,
                health = Attributes.Get(AttributeConstants.HEALTH).PeekAdd(amount)
            };
            OnHealthRegainEvent?.Invoke(this, e);

            //  return if the event has been cancelled by any subscriber
            if (e.cancel)
                return;

            Attributes.Get(AttributeConstants.HEALTH).Add(e.amount);
        }
    }

}