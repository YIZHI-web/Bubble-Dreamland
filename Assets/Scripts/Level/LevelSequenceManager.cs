using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using DG.Tweening;

[System.Serializable]
public struct BlockSequenceData
{
    public Transform spawnPoint;
    public GameObject blockPrefab;
}

public class LevelSequenceManager : MonoBehaviour
{
    public static LevelSequenceManager Instance;

    [Header("Level Sequence")]
    public BlockSequenceData[] levelSequence;

    [Header("Preview Settings")]
    [Tooltip("每个方块依次出现的间隔时间（秒）")]
    public float spawnInterval = 0.3f; // 从0.15增加到0.3，更慢更清晰
    [Tooltip("所有方块显示后，停顿多久再消失")]
    public float previewHoldTime = 1.0f; // 从0.5增加到1.0，给玩家更多观察时间
    [Tooltip("方块消失动画的持续时间（秒）")]
    public float fadeOutDuration = 0.4f; // 新增：平滑消失动画

    private int currentIndex = 0;
    private Vector3 nextTeleportTargetPos = Vector3.zero;
    private HashSet<int> exitPortalIndices = new HashSet<int>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        BlockController.OnBlockTouchedWithData += HandleBlockTouched;
    }

    private void OnDisable()
    {
        BlockController.OnBlockTouchedWithData -= HandleBlockTouched;
    }

    private void Start()
    {
    }

    private void HandleBlockTouched(BlockController touchedBlock)
    {
        if (touchedBlock.type == BlockType.Teleport && !touchedBlock.isExitPortal)
        {
            PrepareNextTeleportDestination();
        }

        SpawnNextSequenceBlock();
    }

    public void PlayPreviewSequence(Action onPreviewComplete)
    {
        StartCoroutine(PreviewSequenceRoutine(onPreviewComplete));
    }

    private IEnumerator PreviewSequenceRoutine(Action onPreviewComplete)
    {
        ClearAllBlocksSilently();

        List<GameObject> previewBlocks = new List<GameObject>();

        for (int i = 0; i < levelSequence.Length; i++)
        {
            BlockSequenceData data = levelSequence[i];
            if (data.spawnPoint != null && data.blockPrefab != null)
            {
                GameObject newBlock = Instantiate(data.blockPrefab, data.spawnPoint.position, data.spawnPoint.rotation);
                previewBlocks.Add(newBlock);
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        yield return new WaitForSeconds(previewHoldTime);

        foreach (var block in previewBlocks)
        {
            if (block != null)
            {
                Transform blockTransform = block.transform;
                blockTransform.DOScale(Vector3.zero, fadeOutDuration).SetEase(Ease.InBack);
            }
        }

        yield return new WaitForSeconds(fadeOutDuration);

        foreach (var block in previewBlocks)
        {
            if (block != null) Destroy(block);
        }

        currentIndex = 0;
        SpawnNextSequenceBlock();

        onPreviewComplete?.Invoke();
    }

    public void ClearAllBlocksSilently()
    {
        currentIndex = 0;
        GameObject[] allBlocks = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allBlocks)
        {
            BlockController bc = obj.GetComponent<BlockController>();
            if (bc != null) Destroy(obj);
        }
    }

    private void SpawnNextSequenceBlock()
    {
        if (currentIndex >= levelSequence.Length) return;

        BlockSequenceData data = levelSequence[currentIndex];
        if (data.spawnPoint != null && data.blockPrefab != null)
        {
            GameObject newBlock = Instantiate(data.blockPrefab, data.spawnPoint.position, data.spawnPoint.rotation);
            BlockController newBc = newBlock.GetComponent<BlockController>();

            if (newBc != null && exitPortalIndices.Contains(currentIndex))
            {
                newBc.isExitPortal = true;
            }
        }
        currentIndex++;
    }

    private void PrepareNextTeleportDestination()
    {
        for (int i = currentIndex; i < levelSequence.Length; i++)
        {
            GameObject prefab = levelSequence[i].blockPrefab;
            if (prefab != null)
            {
                BlockController bc = prefab.GetComponent<BlockController>();
                if (bc != null && bc.type == BlockType.Teleport)
                {
                    nextTeleportTargetPos = levelSequence[i].spawnPoint.position;
                    exitPortalIndices.Add(i);
                    return;
                }
            }
        }
        nextTeleportTargetPos = transform.position;
    }

    public Vector3 GetNextTeleportTargetPosition()
    {
        return nextTeleportTargetPos;
    }

    public void DestroyOrRecycleBlock(GameObject block)
    {
        Destroy(block);
    }

    public void ResetAllBlocks()
    {
        currentIndex = 0;
        nextTeleportTargetPos = Vector3.zero;

        GameObject[] allBlocks = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allBlocks)
        {
            BlockController bc = obj.GetComponent<BlockController>();
            if (bc != null)
            {
                Destroy(obj);
            }
        }

        SwitchController[] allSwitches = GameObject.FindObjectsOfType<SwitchController>();
        foreach (SwitchController sw in allSwitches)
        {
            sw.ForceReset();
        }

        SpawnNextSequenceBlock();
    }
}