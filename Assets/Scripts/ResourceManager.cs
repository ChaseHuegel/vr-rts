using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public class ResourceManager : Singleton<ResourceManager>
{
    [Header("ResourceNodes")]
    public List<ResourceNode> trees;
    public List<ResourceNode> gold;
    public List<ResourceNode> ore;
    public List<ResourceNode> grain;

    [Header("Buildings")]
    public List<TerrainBuilding> lumberMills;
    public List<TerrainBuilding> townHalls;
    public List<TerrainBuilding> granaries;
    public List<TerrainBuilding> buildAndRepair;
    
    public static List<ResourceNode> GetTrees() { return Instance.trees; }
    public static List<ResourceNode> GetGold() { return Instance.gold; }
    public static List<ResourceNode> GetOre() { return Instance.ore; }
    public static List<ResourceNode> GetGrain() { return Instance.grain; }

public static List<TerrainBuilding> GetBuildAndRepair() { return Instance.buildAndRepair; }
    public static List<TerrainBuilding> GetLumberMills() { return Instance.lumberMills; }
    public static List<TerrainBuilding> GetTownHalls() { return Instance.townHalls; }
    public static List<TerrainBuilding> GetGranaries() {return Instance.granaries; }
}
