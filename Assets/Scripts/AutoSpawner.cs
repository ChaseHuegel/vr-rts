using System.Collections;
using System.Collections.Generic;
using MLAPI;
using Swordfish;
using Swordfish.Audio;
using Swordfish.Navigation;
using UnityEditor;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class AutoSpawner : MonoBehaviour
{
    [Header("Autospawn Settings")]
    public bool autospawn;

    // [Range(0.0f, 600.0f)]
    public float secondsToAutospawnStart = 180.0f;

    [Header("Wave Settings")]
    // [Range(2.0f, 600.0f)]
    public float secondsBetweenWaves = 600.0f;
    [Tooltip("[Units to spawn per wave] is multiplied by [wave spawn multiplier] every [wave spawn increment interval].")]
    public byte unitsToSpawnPerWave = 1;

    [Tooltip("[Units to spawn per wave] is multiplied by [wave spawn multiplier] every [wave spawn increment interval].")]
    public byte waveSpawnMultiplier = 1;
    [Tooltip("The maximum number of units to spawn per wave.")]
    public byte maxUnitsPerWave = 5;

    [Tooltip("[Units to spawn per wave] is multiplied by [wave spawn multiplier] every [wave spawn increment interval].")]
    public byte waveSpawnIncrementInterval = 10;

    private byte currentWaveSpawnIncrementInterval;
    private int currentWave;

    [Header("Spawn/Rally Location")]    

    [Tooltip("Central point of the area where the unit will spawn.")]
    public Transform unitSpawnPoint;
    [Tooltip("The radius around the [unit spawn point] in which the unit's spawn location is generated.")]
    public float unitSpawnPointRadius;
    [Tooltip("The point the unit should navigate to after spawning.")]
    public Transform unitRallyWaypoint;
    [Tooltip("The radius around the [unit rally waypoint] in which the unit's rally location is genereated.")]
    public float unitRallyWaypointRadius;

    [Header("Unit settings")]
    public Faction faction;

    [Tooltip("Randomly choose a unit to spawn from [unit spawn list].")]
    public bool randomize;
    [Tooltip("The list of units to spawn. Once the list has been completed it will reset to the beginning.")]
    public UnitData[] unitSpawnList;

    [Header("Instant Spawn")]
    public UnitData unit;

    [InspectorButton("OnSpawnClicked")]
    public bool spawn;
    private LinkedList<UnitData> unitSpawnQueue = new LinkedList<UnitData>();
    private AudioSource audioSource;
    private float spawnTimer;
    private bool started;
    void Awake()
    {
        Body body = GetComponent<Body>();
        if (body)
            faction = body.Faction;
            
        // TODO: Pick a spot around the building and set it as the spawn point
        // when no spawn point is found. Using transform center currently.
        if (!unitSpawnPoint)
            unitSpawnPoint = transform;

        if (!unitRallyWaypoint)
            unitRallyWaypoint = unitSpawnPoint;

        if (randomize)
            currentSpawnListIndex = Random.Range(0, unitSpawnList.Length);
    }

    private void OnSpawnClicked()
    {
        SpawnUnit(unit);
    }

    private int currentSpawnListIndex;

    void Update()
    {
        if (!autospawn)
            return;

        if (started)
        {
            if (spawnTimer >= secondsBetweenWaves)
            {
                byte countToSpawn = unitsToSpawnPerWave < maxUnitsPerWave ? unitsToSpawnPerWave : maxUnitsPerWave;
                for (byte i = 0; i < countToSpawn; i++)
                {
                    SpawnUnit(unitSpawnList[currentSpawnListIndex]);

                    if (randomize)
                        currentSpawnListIndex = Random.Range(0, unitSpawnList.Length);
                    else
                    {
                        currentSpawnListIndex++;
                        if (currentSpawnListIndex >= unitSpawnList.Length)
                            currentSpawnListIndex = 0;
                    }
                }

                currentWave++;
                spawnTimer = 0.0f;
                currentWaveSpawnIncrementInterval++;

                if (currentWaveSpawnIncrementInterval == waveSpawnIncrementInterval)
                {
                    unitsToSpawnPerWave += waveSpawnMultiplier;
                    currentWaveSpawnIncrementInterval = 0;
                }
            }
        }
        else if (spawnTimer >= secondsToAutospawnStart)
        {
            started = true;
            spawnTimer = 0.0f;
        }

        spawnTimer += Time.deltaTime;
    }

    private void SpawnUnit(UnitData unitData)
    {
        if (unitData?.worldPrefab)
        {
            Vector3 randomPos = Random.insideUnitSphere * unitSpawnPointRadius;
            Vector3 position = unitSpawnPoint.transform.position + randomPos;

            // TODO: Project ray to get y position?
            position.y = unitSpawnPoint.transform.position.y;

            GameObject unitGameObject = Instantiate(unitData.worldPrefab, position, Quaternion.identity);

            if (NetworkManager.Singleton.IsServer)
                unitGameObject.GetComponent<NetworkObject>().Spawn();

            UnitV2 unit = unitGameObject.GetComponent<UnitV2>();
            if (faction)
                unit.Faction = faction;
            unit.unitData = unitData;

            randomPos = Random.insideUnitSphere * unitSpawnPointRadius;
            position = unitRallyWaypoint.transform.position + randomPos;

            unit.Destination = World.at(World.ToWorldCoord(position));
        }
        else
        {
            Debug.Log(string.Format("Spawn {0} failed. Missing prefabToSpawn.", unitData.name));
        }
    }

    public void OnDrawGizmos()
    {
        if (Application.isEditor != true || Application.isPlaying) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(unitSpawnPoint.position, unitSpawnPointRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(unitRallyWaypoint.position, unitRallyWaypointRadius);
    }
}
