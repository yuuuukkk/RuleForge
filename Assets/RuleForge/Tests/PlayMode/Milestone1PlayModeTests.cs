using System.Collections;
using NUnit.Framework;
using RuleForge.Config;
using RuleForge.Enemies;
using RuleForge.Runtime;
using RuleForge.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RuleForge.Tests
{
    public sealed class Milestone1PlayModeTests
    {
        [UnityTest]
        public IEnumerator WeaponRaycast_DamagesEnemy()
        {
            GameObject cameraObject = new GameObject("Test Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = Vector3.up * 50f;

            GameObject weaponObject = new GameObject("Test Weapon");
            WeaponConfig weaponConfig = ScriptableObject.CreateInstance<WeaponConfig>();
            WeaponRuntimeStats weaponRuntimeStats =
                weaponObject.AddComponent<WeaponRuntimeStats>();
            weaponRuntimeStats.Configure(weaponConfig);
            WeaponController weapon = weaponObject.AddComponent<WeaponController>();
            weapon.SetAimCamera(camera);

            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "Test Enemy";
            enemyObject.transform.position = Vector3.up * 50f + Vector3.forward * 5f;
            EnemyConfig enemyConfig = ScriptableObject.CreateInstance<EnemyConfig>();
            EnemyRuntimeStats enemyRuntimeStats = enemyObject.AddComponent<EnemyRuntimeStats>();
            enemyRuntimeStats.Configure(enemyConfig);
            EnemyHealth enemyHealth = enemyObject.AddComponent<EnemyHealth>();
            enemyHealth.ResetHealth();

            Physics.SyncTransforms();
            yield return null;

            bool fired = weapon.TryFire();

            Assert.That(fired, Is.True);
            Assert.That(enemyHealth.CurrentHealth, Is.LessThan(enemyHealth.MaxHealth));

            Object.Destroy(cameraObject);
            Object.Destroy(weaponObject);
            Object.Destroy(enemyObject);
            Object.Destroy(weaponConfig);
            Object.Destroy(enemyConfig);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Arena_EnemyDeathTriggersRespawn()
        {
            SceneManager.LoadScene("Arena", LoadSceneMode.Single);
            yield return null;

            EnemySpawner spawner = Object.FindObjectOfType<EnemySpawner>();
            Assert.That(spawner, Is.Not.Null);
            Assert.That(spawner.AliveEnemyCount, Is.EqualTo(1));

            EnemyHealth firstEnemy = Object.FindObjectOfType<EnemyHealth>();
            Assert.That(firstEnemy, Is.Not.Null);

            firstEnemy.TakeDamage(new DamageInfo(firstEnemy.MaxHealth));
            yield return null;

            Assert.That(spawner.AliveEnemyCount, Is.Zero);

            yield return new WaitForSeconds(2.1f);

            EnemyHealth respawnedEnemy = Object.FindObjectOfType<EnemyHealth>();
            Assert.That(respawnedEnemy, Is.Not.Null);
            Assert.That(respawnedEnemy, Is.Not.SameAs(firstEnemy));
            Assert.That(spawner.AliveEnemyCount, Is.EqualTo(1));
        }
    }
}
