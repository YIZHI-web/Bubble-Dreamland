using UnityEngine;
using DG.Tweening;
using System;

public class SwitchController : MonoBehaviour
{
    [Header("References")]
    public Transform handleVisuals;
    public GameObject sparkVFXPrefab;
    public Transform vfxSpawnPoint;

    [Header("Settings")]
    public Vector3 pulledRotation = new Vector3(0, 0, -60f);
    public Vector3 releasedRotation = new Vector3(0, 0, 0);
    public float pullDuration = 0.4f;
    public float autoResetDelay = 5f;

    private bool isActivated = false;
    private Tween delayTween;

    private void Start()
    {
        if (handleVisuals != null)
        {
            handleVisuals.localRotation = Quaternion.Euler(releasedRotation);
        }
    }

    public void ActivateSwitch(Action onSwitchActivated)
    {
        if (isActivated) return;
        isActivated = true;

        if (sparkVFXPrefab != null)
        {
            Vector3 spawnPos = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
            Instantiate(sparkVFXPrefab, spawnPos, Quaternion.identity);
        }

        handleVisuals.DOLocalRotate(pulledRotation, pullDuration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => {
                onSwitchActivated?.Invoke();

                delayTween?.Kill();
                delayTween = DOVirtual.DelayedCall(autoResetDelay, () => ResetSwitch());
            });
    }

    public void ResetSwitch()
    {
        if (!isActivated) return;

        handleVisuals.DOLocalRotate(releasedRotation, 0.2f).OnComplete(() => {
            isActivated = false;
        });
    }

    public void ForceReset()
    {
        delayTween?.Kill();

        transform.DOKill();
        if (handleVisuals != null)
        {
            handleVisuals.DOKill();
            handleVisuals.DOLocalRotate(releasedRotation, 0.1f);
        }

        isActivated = false;
    }
}
