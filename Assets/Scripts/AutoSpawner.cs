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
    public bool randomize;

    [Range(0.0f, 600.0f)]
    public float secondsToAutospawnStart;

    [Range(2.0f, 600.0f)]
    public float timeBetweenWaves = 30.0f;
    public byte unitsToSpawnPerWave = 1;

    [Tooltip("How much to increase the spawns per wave by after each wave spawn increment interval.")]
    public byte waveSpawnIncrement = 0;
    public byte maxUnitsPerWave = 5;

    [Tooltip("The number of waves between each wave spawn increment change.")]
    public byte waveSpawnIncrementInterval = 10;

    private byte currentWaveSpawnIncrementInterval;
    private int currentWave;

    [Header("Unit")]
    public byte factionID;
    public Transform unitSpawnPoint;
    public float unitSpawnPointRadius;
    public Transform unitRallyWaypoint;
    public float unitRallyWaypointRadius;
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
            if (spawnTimer >= timeBetweenWaves)
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
                    unitsToSpawnPerWave += waveSpawnIncrement;
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

    public void OnDrawGizmos()
    {
        if (Application.isEditor != true || Application.isPlaying) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(unitSpawnPoint.position, unitSpawnPointRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(unitRallyWaypoint.position, unitRallyWaypointRadius);
    }

    private void SpawnUnit(UnitData unitData)
    {
        if (unitData?.worldPrefab)
        {
            Vector3 randomPos = Random.insideUnitSphere * unitSpawnPointRadius;
            Vector3 position = unitSpawnPoint.transform.position + randomPos;
            position.y = unitSpawnPoint.transform.position.y;

            GameObject unitGameObject = Instantiate(unitData.worldPrefab, position, Quaternion.identity);

            if (NetworkManager.Singleton.IsServer)
                unitGameObject.GetComponent<NetworkObject>().Spawn();

            UnitV2 unit = unitGameObject.GetComponent<UnitV2>();
            unit.Faction = GameMaster.Factions.Find(x => x.Id == factionID);
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
}
