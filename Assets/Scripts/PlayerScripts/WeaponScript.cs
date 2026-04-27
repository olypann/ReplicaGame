using System.Collections.Generic;
using UnityEngine;

public class WeaponScript : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;

    private HashSet<EnemyScript> hitEnemies = new HashSet<EnemyScript>();

    

    private void OnTriggerEnter(Collider other)
    {
        if (playerCombat == null)
        {
            return;
        }

        if (!playerCombat.IsAttacking())
        {
            return;
        }

        // boss first
        WormBossPartHealth bossPart = other.GetComponentInParent<WormBossPartHealth>();

        if (bossPart != null)
        {
            bossPart.TakeDamage(playerCombat.GetCurrentDamage());

            playerCombat.AddAbilityCharge(playerCombat.abilityGainPerHit);

            SoundManager.Instance?.PlayAttack1Hit();

            return;
        }

        EnemyScript enemy = other.GetComponent<EnemyScript>();

        if (enemy != null && !hitEnemies.Contains(enemy))
        {
            hitEnemies.Add(enemy);

            enemy.TakeDamage(playerCombat.GetCurrentDamage());

            playerCombat.AddAbilityCharge(playerCombat.abilityGainPerHit);

            SoundManager.Instance?.PlayAttack1Hit();
        }
    }

    public void ResetHits()
    {
        hitEnemies.Clear();
    }
}