using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Audio;
using UnityEditor;
using Valve.VR.InteractionSystem;
using Swordfish.Navigation;
using MLAPI;

public class AutoSpawner : MonoBehaviour
{
    [Header("Autospawn Settings")]
    public bool autospawn;

    [Min(0.0f)]
    public float timeToAutospawnStart;

    [Min(3.0f)]
    public float timeBetweenSpawns = 5.0f;
    
    [Header("Unit")]
    public RTSUnitType unitToAutospawn;
    public byte factionID;
    public Transform unitSpawnPoint;
    public float unitSpawnPointRadius;
    public Transform unitRallyWaypoint;
    public float unitRallyWaypointRadius;

    [Header("Instant Spawn")]
    public RTSUnitType unit;
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
    }

    private void OnSpawnClicked()
    {
        SpawnUnit(unit);
    }

    void Update()
    {
        if (!autospawn)
            return;

        if (started)
        {
            if (spawnTimer >= timeBetweenSpawns)
            {
                SpawnUnit(unitToAutospawn);
                spawnTimer = 0.0f;
            }
        }
        else if (spawnTimer >= timeToAutospawnStart)
        {
            started = true;
            spawnTimer = 0.0f;
        }

        spawnTimer += Time.deltaTime;
    }

    private void SpawnUnit(RTSUnitType unitType)
    {
        UnitData unitData = GameMaster.GetUnit(unitType);

        if (unitData.prefab)
        {
            Vector3 randomPos = (Vector3)Random.insideUnitSphere * unitSpawnPointRadius;
            Vector3 position = unitSpawnPoint.transform.position + randomPos;
            position.y = unitSpawnPoint.transform.position.y;
            
            GameObject unitGameObject = Instantiate(unitData.prefab, position, Quaternion.identity);
            
            if (NetworkManager.Singleton.IsServer)
            {
                unitGameObject.GetComponent<NetworkObject>().Spawn();
            }
            
            Unit unit = unitGameObject.GetComponent<Unit>();
            unit.rtsUnitType = unitData.unitType;
            unit.factionID = factionID;
            unit.SyncPosition();

            randomPos = (Vector3)Random.insideUnitCircle * unitSpawnPointRadius; 
            position = unitRallyWaypoint.transform.position + randomPos;

            unit.GotoForced(World.ToWorldSpace(position));
            unit.LockPath();

            // Debug.Log("Spawned " + unit.rtsUnitType + ".");
        }
        else
            Debug.Log (string.Format("Spawn {0} failed. Missing prefabToSpawn.", unitData.unitType));
    }
}
