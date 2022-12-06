using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointerInteractable : MonoBehaviour
{
    public Material highlightMaterial;
    public bool highlightOnPointedAt = true;
    protected GameObject highlightHolder;
    [Tooltip("An array of child gameObjects to not render a highlight for. Things like transparent parts, vfx, etc.")]    
    public List<GameObject> hideHighlight;
    protected MeshRenderer[] highlightRenderers;
    protected MeshRenderer[] existingRenderers;
    protected SkinnedMeshRenderer[] highlightSkinnedRenderers;
    protected SkinnedMeshRenderer[] existingSkinnedRenderers;
    protected bool isPointedAt;
    protected bool wasPointedAt;

    void Start()
    {
        if (highlightMaterial == null)
#if UNITY_URP
            highlightMaterial = (Material)Resources.Load("SteamVR_HoverHighlight_URP", typeof(Material));
#else
            highlightMaterial = (Material)Resources.Load("PointerHoverHighlight", typeof(Material));
#endif

        if (highlightMaterial == null)
            Debug.LogError("<b>[SteamVR Interaction]</b> Hover Highlight Material is missing. Please create a material named 'PointerHoverHighlight' and place it in a Resources folder", this);
        
    }

    protected virtual void Update()
    {        
        UpdateHighlightRenderers();
    
        if (isPointedAt == false && highlightHolder != null)
            Destroy(highlightHolder);
    }

    public bool TryAddToHideHighlight(GameObject gameObject)
    {
        if (!hideHighlight.Contains(gameObject))
        {
            hideHighlight.Add(gameObject);
            return true;
        }
        return false;
    }

    public void Highlight( bool highlight )
    {
        if (highlight)
        {
            wasPointedAt = isPointedAt;        
            isPointedAt = true;  

            if (wasPointedAt == false)
            {
                CreateHighlightRenderers();
                UpdateHighlightRenderers();
            }
        }
        else
        {
            wasPointedAt = isPointedAt;
            isPointedAt = false;

            if (highlightHolder != null)
                Destroy(highlightHolder);
        }

    }

    protected virtual bool ShouldIgnoreHighlight(Component component)
    {
        return ShouldIgnore(component.gameObject);
    }

    protected virtual bool ShouldIgnore(GameObject check)
    {
        for (int ignoreIndex = 0; ignoreIndex < hideHighlight?.Count; ignoreIndex++)
        {
            if (check == hideHighlight[ignoreIndex])
                return true;
        }

        return false;
    }
    
    protected virtual void CreateHighlightRenderers()
    {
        existingSkinnedRenderers = this.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        highlightHolder = new GameObject("Highlighter");
        highlightSkinnedRenderers = new SkinnedMeshRenderer[existingSkinnedRenderers.Length];

        for (int skinnedIndex = 0; skinnedIndex < existingSkinnedRenderers.Length; skinnedIndex++)
        {
            SkinnedMeshRenderer existingSkinned = existingSkinnedRenderers[skinnedIndex];

            if (ShouldIgnoreHighlight(existingSkinned))
                    continue;

            GameObject newSkinnedHolder = new GameObject("SkinnedHolder");
            newSkinnedHolder.transform.parent = highlightHolder.transform;
            SkinnedMeshRenderer newSkinned = newSkinnedHolder.AddComponent<SkinnedMeshRenderer>();
            Material[] materials = new Material[existingSkinned.sharedMaterials.Length];
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                materials[materialIndex] = highlightMaterial;
            }

            newSkinned.sharedMaterials = materials;
            newSkinned.sharedMesh = existingSkinned.sharedMesh;
            newSkinned.rootBone = existingSkinned.rootBone;
            newSkinned.updateWhenOffscreen = existingSkinned.updateWhenOffscreen;
            newSkinned.bones = existingSkinned.bones;

            highlightSkinnedRenderers[skinnedIndex] = newSkinned;
        }

        MeshFilter[] existingFilters = this.GetComponentsInChildren<MeshFilter>(true);
        existingRenderers = new MeshRenderer[existingFilters.Length];
        highlightRenderers = new MeshRenderer[existingFilters.Length];

        for (int filterIndex = 0; filterIndex < existingFilters.Length; filterIndex++)
        {
            MeshFilter existingFilter = existingFilters[filterIndex];
            MeshRenderer existingRenderer = existingFilter.GetComponent<MeshRenderer>();

            if (existingFilter == null || existingRenderer == null || ShouldIgnoreHighlight(existingFilter))
                continue;

            GameObject newFilterHolder = new GameObject("FilterHolder");
            newFilterHolder.transform.parent = highlightHolder.transform;
            MeshFilter newFilter = newFilterHolder.AddComponent<MeshFilter>();
            newFilter.sharedMesh = existingFilter.sharedMesh;
            MeshRenderer newRenderer = newFilterHolder.AddComponent<MeshRenderer>();

            Material[] materials = new Material[existingRenderer.sharedMaterials.Length];
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                materials[materialIndex] = highlightMaterial;
            }
            newRenderer.sharedMaterials = materials;

            highlightRenderers[filterIndex] = newRenderer;
            existingRenderers[filterIndex] = existingRenderer;
        }
    }

    protected virtual void UpdateHighlightRenderers()
    {
        if (highlightHolder == null)
            return;

        for (int skinnedIndex = 0; skinnedIndex < existingSkinnedRenderers.Length; skinnedIndex++)
        {
            SkinnedMeshRenderer existingSkinned = existingSkinnedRenderers[skinnedIndex];
            SkinnedMeshRenderer highlightSkinned = highlightSkinnedRenderers[skinnedIndex];

            if (existingSkinned != null && highlightSkinned != null && isPointedAt == true)
            {
                highlightSkinned.transform.position = existingSkinned.transform.position;
                highlightSkinned.transform.rotation = existingSkinned.transform.rotation;
                highlightSkinned.transform.localScale = existingSkinned.transform.lossyScale;
                highlightSkinned.localBounds = existingSkinned.localBounds;
                highlightSkinned.enabled = isPointedAt && existingSkinned.enabled && existingSkinned.gameObject.activeInHierarchy;

                int blendShapeCount = existingSkinned.sharedMesh.blendShapeCount;
                for (int blendShapeIndex = 0; blendShapeIndex < blendShapeCount; blendShapeIndex++)
                {
                    highlightSkinned.SetBlendShapeWeight(blendShapeIndex, existingSkinned.GetBlendShapeWeight(blendShapeIndex));
                }
            }
            else if (highlightSkinned != null)
                highlightSkinned.enabled = false;

        }

        for (int rendererIndex = 0; rendererIndex < highlightRenderers.Length; rendererIndex++)
        {
            MeshRenderer existingRenderer = existingRenderers[rendererIndex];
            MeshRenderer highlightRenderer = highlightRenderers[rendererIndex];

            if (existingRenderer != null && highlightRenderer != null && isPointedAt == true)
            {
                highlightRenderer.transform.position = existingRenderer.transform.position;
                highlightRenderer.transform.rotation = existingRenderer.transform.rotation;
                highlightRenderer.transform.localScale = existingRenderer.transform.lossyScale;
                highlightRenderer.enabled = isPointedAt && existingRenderer.enabled && existingRenderer.gameObject.activeInHierarchy;
            }
            else if (highlightRenderer != null)
                highlightRenderer.enabled = false;
        }
    }
    
    protected virtual void OnDestroy()
    {
        if (highlightHolder != null)
                Destroy(highlightHolder);
    }

    protected virtual void OnDisable()
    {
        if (highlightHolder != null)
            Destroy(highlightHolder);
    }
}
