using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GameController : MonoBehaviour
{
    [Header("Global State")]
    public int deathCount = 0;
    
    [Header("Environment & Visuals")]
    public Light globalLight;
    public Color starkColdColor = new Color(0.8f, 0.9f, 1.0f);
    public Color vibrantNeonColor = new Color(1.0f, 0.2f, 0.8f);
    
    [Header("Level Layers & Layouts")]
    public GameObject shortcutLayer; // The hidden golden passage
    public GameObject tilemapLayoutA; // Default Hard Layout
    public GameObject tilemapLayoutB; // Safer Geometry Layout
    public GameObject bridgeOverlay; // Permanent chasm bridges

    [Header("Ghost Tracking")]
    public GameObject ghostPrefab; // Prefab with Cyan_Glow shader and TrailRenderer
    private List<GhostInstance> activeGhosts = new List<GhostInstance>();

    // Singletons / References
    public static GameController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeLevel();
    }

    private void Update()
    {
        UpdateWorldState();
        UpdateVisuals();
    }

    private void InitializeLevel()
    {
        if (deathCount == 0)
        {
            SetDifficultyTierHard();
        }
        else
        {
            UpdateWorldState();
        }
    }

    private void SetDifficultyTierHard()
    {
        if (shortcutLayer != null) shortcutLayer.SetActive(false);
        if (tilemapLayoutA != null) tilemapLayoutA.SetActive(true);
        if (tilemapLayoutB != null) tilemapLayoutB.SetActive(false);
        if (bridgeOverlay != null) bridgeOverlay.SetActive(false);
    }

    private void UpdateWorldState()
    {
        if (deathCount >= 3)
        {
            SolidifyGhosts();
        }

        if (deathCount >= 5 && shortcutLayer != null && !shortcutLayer.activeSelf)
        {
            shortcutLayer.SetActive(true);
        }

        if (deathCount >= 8 && tilemapLayoutA != null && tilemapLayoutA.activeSelf)
        {
            tilemapLayoutA.SetActive(false);
            if (tilemapLayoutB != null) tilemapLayoutB.SetActive(true);
        }

        if (deathCount >= 10 && bridgeOverlay != null && !bridgeOverlay.activeSelf)
        {
            bridgeOverlay.SetActive(true);
        }
    }

    private void UpdateVisuals()
    {
        if (globalLight != null)
        {
            float t = Mathf.Clamp01((float)deathCount / 10f);
            globalLight.color = Color.Lerp(starkColdColor, vibrantNeonColor, t);
        }
    }

    public void OnPlayerDeath(List<FrameData> recordedPath)
    {
        deathCount++;
        SpawnGhost(recordedPath);
    }

    private void SpawnGhost(List<FrameData> recordedPath)
    {
        if (ghostPrefab == null) return;
        
        GameObject ghostObj = Instantiate(ghostPrefab);
        GhostInstance ghost = ghostObj.GetComponent<GhostInstance>();
        if (ghost != null)
        {
            ghost.Initialize(recordedPath);
            if (deathCount >= 3)
            {
                ghost.MakeSolid();
            }
            activeGhosts.Add(ghost);
        }
    }

    private void SolidifyGhosts()
    {
        foreach (var ghost in activeGhosts)
        {
            if (!ghost.isSolid)
            {
                ghost.MakeSolid();
            }
        }
    }
}

// -----------------------------------------------------------------------------------------
// PATH MANAGER & GHOST DATA STRUCTURES
// -----------------------------------------------------------------------------------------

[System.Serializable]
public struct FrameData
{
    public Vector2 position;
    public Vector2 inputState;
}

public class PathManager : MonoBehaviour
{
    private List<FrameData> currentRunData = new List<FrameData>();
    private float recordTimer = 0f;
    private const float RECORD_INTERVAL = 1f / 60f; // 60Hz intervals

    private bool isPlayerAlive = true;

    private void Update()
    {
        if (!isPlayerAlive) return;

        recordTimer += Time.deltaTime;
        if (recordTimer >= RECORD_INTERVAL)
        {
            RecordFrame();
            recordTimer -= RECORD_INTERVAL;
        }
    }

    private void RecordFrame()
    {
        FrameData frame = new FrameData
        {
            position = transform.position,
            inputState = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetButton("Jump") ? 1f : 0f)
        };
        currentRunData.Add(frame);
    }

    public void Die()
    {
        isPlayerAlive = false;
        if (GameController.Instance != null)
        {
            GameController.Instance.OnPlayerDeath(new List<FrameData>(currentRunData));
        }
        currentRunData.Clear();
    }
}

// -----------------------------------------------------------------------------------------
// GHOST INSTANCE BEHAVIOR
// -----------------------------------------------------------------------------------------

[RequireComponent(typeof(BoxCollider2D))]
public class GhostInstance : MonoBehaviour
{
    private List<FrameData> pathData;
    public bool isSolid { get; private set; } = false;
    
    private BoxCollider2D col;
    private SpriteRenderer sr;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (col != null) col.enabled = false; 
    }

    public void Initialize(List<FrameData> data)
    {
        pathData = data;
    }

    public void MakeSolid()
    {
        isSolid = true;
        if (col != null) col.enabled = true;
        
        if (sr != null)
        {
            Color solidColor = sr.color;
            solidColor.a = 1.0f;
            sr.color = solidColor;
        }
    }
}
