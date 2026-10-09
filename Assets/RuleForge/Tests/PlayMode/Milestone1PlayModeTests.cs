using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RuleForge.Config;
using RuleForge.Enemies;
using RuleForge.Runtime;
using RuleForge.Weapons;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

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
        public IEnumerator EnemySpawner_PreparationDefersInitialPopulation()
        {
            Time.timeScale = 1f;
            SpawnerFixture fixture = CreateSpawnerFixture(
                0.12f,
                CreateEnemyConfig("Grunt", 2, 0.1f));

            yield return null;

            Assert.That(fixture.Spawner.IsPreparing, Is.True);
            Assert.That(fixture.Spawner.AliveEnemyCount, Is.Zero);

            yield return WaitUntilOrFail(
                () => !fixture.Spawner.IsPreparing &&
                      fixture.Spawner.AliveEnemyCount ==
                      fixture.Spawner.DesiredAliveEnemyCount,
                2f,
                "Spawner did not finish preparation and create its desired population.");

            Assert.That(fixture.Spawner.AliveEnemyCount, Is.EqualTo(2));
            fixture.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemySpawner_ConsecutiveDeathsKeepIndependentDelays()
        {
            Time.timeScale = 1f;
            SpawnerFixture fixture = CreateSpawnerFixture(
                0f,
                CreateEnemyConfig("Fast", 2, 0.05f),
                CreateEnemyConfig("Slow", 2, 0.4f));

            yield return WaitUntilOrFail(
                () => fixture.Spawner.AliveEnemyCount == 2 &&
                      fixture.Spawned.Count >= 2,
                2f,
                "Spawner did not create the initial population.");

            EnemyHealth fastEnemy = fixture.FindAlive("Fast");
            EnemyHealth slowEnemy = fixture.FindAlive("Slow");
            Assert.That(fastEnemy, Is.Not.Null);
            Assert.That(slowEnemy, Is.Not.Null);

            fastEnemy.TakeDamage(new DamageInfo(fastEnemy.MaxHealth));
            slowEnemy.TakeDamage(new DamageInfo(slowEnemy.MaxHealth));

            Assert.That(fixture.Spawner.AliveEnemyCount, Is.Zero);
            Assert.That(fixture.Spawner.PendingRespawnCount, Is.EqualTo(2));

            yield return WaitUntilOrFail(
                () => fixture.Spawner.AliveEnemyCount == 1 &&
                      fixture.Spawner.PendingRespawnCount == 1,
                1f,
                "The first completed timer filled more than its own missing slot.");

            Assert.That(fixture.Spawner.AliveEnemyCount, Is.EqualTo(1));
            Assert.That(fixture.Spawner.PendingRespawnCount, Is.EqualTo(1));

            yield return WaitUntilOrFail(
                () => fixture.Spawner.AliveEnemyCount == 2 &&
                      fixture.Spawner.PendingRespawnCount == 0,
                2f,
                "The second independent respawn timer did not restore its slot.");

            fixture.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemySpawner_RestartClearsPendingRespawns()
        {
            Time.timeScale = 1f;
            const float firstDelay = 0.3f;
            const float secondDelay = 0.5f;
            SpawnerFixture fixture = CreateSpawnerFixture(
                0.08f,
                CreateEnemyConfig("First", 2, firstDelay),
                CreateEnemyConfig("Second", 2, secondDelay));

            yield return WaitUntilOrFail(
                () => fixture.Spawner.AliveEnemyCount == 2 &&
                      fixture.Spawned.Count >= 2,
                2f,
                "Spawner did not create the initial population.");

            fixture.Spawned[0].TakeDamage(
                new DamageInfo(fixture.Spawned[0].MaxHealth));
            fixture.Spawned[1].TakeDamage(
                new DamageInfo(fixture.Spawned[1].MaxHealth));
            Assert.That(fixture.Spawner.PendingRespawnCount, Is.EqualTo(2));

            fixture.Spawner.ResetForChallenge();

            Assert.That(fixture.Spawner.PendingRespawnCount, Is.Zero);
            Assert.That(fixture.Spawner.AliveEnemyCount, Is.Zero);
            Assert.That(fixture.Spawner.IsPreparing, Is.True);

            yield return WaitUntilOrFail(
                () => !fixture.Spawner.IsPreparing &&
                      fixture.Spawner.AliveEnemyCount == 2,
                2f,
                "Restart did not run a fresh preparation and population cycle.");

            yield return WaitForRealtimeDuration(secondDelay + 0.1f);
            Assert.That(fixture.Spawner.AliveEnemyCount, Is.EqualTo(2),
                "A coroutine from the previous challenge spawned an extra enemy.");
            Assert.That(fixture.Spawner.PendingRespawnCount, Is.Zero);

            fixture.Dispose();
            yield return null;
        }

        private static SpawnerFixture CreateSpawnerFixture(
            float preparationDuration,
            params EnemyConfig[] configs)
        {
            GameObject prefabObject = new GameObject("Test Enemy Prefab");
            EnemyRuntimeStats prefabStats =
                prefabObject.AddComponent<EnemyRuntimeStats>();
            prefabStats.Configure(configs[0]);
            EnemyHealth prefab = prefabObject.AddComponent<EnemyHealth>();

            GameObject root = new GameObject("Test Enemy Spawner");
            Transform[] spawnPoints = new Transform[4];
            for (int index = 0; index < spawnPoints.Length; index++)
            {
                GameObject point = new GameObject("Spawn " + index);
                point.transform.SetParent(root.transform);
                point.transform.position = new Vector3(index * 3f, 50f, 0f);
                spawnPoints[index] = point.transform;
            }

            EnemySpawner spawner = root.AddComponent<EnemySpawner>();
            spawner.ConfigureRoster(prefab, spawnPoints, configs);
            spawner.ConfigurePacing(preparationDuration, 0.01f);
            return new SpawnerFixture(root, prefabObject, spawner, configs);
        }

        private static EnemyConfig CreateEnemyConfig(
            string name,
            int desiredAlive,
            float respawnDelay)
        {
            EnemyConfig config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.ConfigureProfile(
                name,
                50f,
                0f,
                1f,
                10f,
                0f,
                1f,
                desiredAlive,
                respawnDelay,
                Color.red,
                1f);
            return config;
        }

        private static IEnumerator WaitUntilOrFail(
            Func<bool> condition,
            float timeoutSeconds,
            string failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Assert.Fail(failureMessage);
                }

                yield return null;
            }
        }

        private static IEnumerator WaitForRealtimeDuration(float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private sealed class SpawnerFixture
        {
            private readonly GameObject root;
            private readonly GameObject prefabObject;
            private readonly EnemyConfig[] configs;

            public SpawnerFixture(
                GameObject root,
                GameObject prefabObject,
                EnemySpawner spawner,
                EnemyConfig[] configs)
            {
                this.root = root;
                this.prefabObject = prefabObject;
                this.configs = configs;
                Spawner = spawner;
                Spawner.EnemySpawned += HandleEnemySpawned;
            }

            public EnemySpawner Spawner { get; }
            public List<EnemyHealth> Spawned { get; } =
                new List<EnemyHealth>();

            public EnemyHealth FindAlive(string enemyType)
            {
                for (int index = 0; index < Spawned.Count; index++)
                {
                    EnemyHealth enemy = Spawned[index];
                    EnemyRuntimeIdentity identity = enemy != null
                        ? enemy.GetComponent<EnemyRuntimeIdentity>()
                        : null;
                    if (enemy != null && enemy.IsAlive && identity != null &&
                        string.Equals(
                            identity.EnemyType,
                            enemyType,
                            StringComparison.Ordinal))
                    {
                        return enemy;
                    }
                }

                return null;
            }

            public void Dispose()
            {
                if (Spawner != null)
                {
                    Spawner.EnemySpawned -= HandleEnemySpawned;
                }

                for (int index = 0; index < Spawned.Count; index++)
                {
                    if (Spawned[index] != null)
                    {
                        Object.Destroy(Spawned[index].gameObject);
                    }
                }

                Object.Destroy(root);
                Object.Destroy(prefabObject);
                for (int index = 0; index < configs.Length; index++)
                {
                    Object.Destroy(configs[index]);
                }
            }

            private void HandleEnemySpawned(EnemyHealth enemy)
            {
                Spawned.Add(enemy);
            }
        }
    }
}
