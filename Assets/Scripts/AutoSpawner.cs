using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Audio;
using UnityEditor;
using Valve.VR.InteractionSystem;
using Swordfish.Navigation;

public class AutoSpawner : MonoBehaviour
{
    [Header("Autospawn")]
    public bool autospawn;

    [Min(0.0f)]
    public float timeToAutospawnStart;

    [Min(3.0f)]
    public float timeBetweenSpawns = 5.0f;
    
    [Header("Unit")]
    public RTSUnitType unitToAutospawn;
    public byte factionID;
    public Transform unitSpawnPoint;
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
        {
            unitSpawnPoint = transform;
            Debug.Log("UnitSpawnPoint not set.", this);
        }
    }

    private void OnSpawnClicked()
    {
        SpawnUnit(unit);
    }

    void Start()
    {
        if (!(audioSource = gameObject.GetComponentInParent<AudioSource>()))
            Debug.Log("Missing audiosource component in parent.", this);
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
            GameObject unitGameObject = Instantiate(unitData.prefab, unitSpawnPoint.transform.position, Quaternion.identity);
            Unit unit = unitGameObject.GetComponent<Unit>();
            unit.rtsUnitType = unitData.unitType;
            unit.factionID = factionID;
            unit.SyncPosition();
            unit.GotoForced(World.ToWorldSpace(unitRallyWaypoint.position));
            unit.LockPath();

            // Debug.Log("Spawned " + unit.rtsUnitType + ".");
        }
        else
            Debug.Log (string.Format("Spawn {0} failed. Missing prefabToSpawn.", unitData.unitType));
    }
}
