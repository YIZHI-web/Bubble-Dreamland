using UnityEngine;
using Cinemachine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance;

    private CinemachineVirtualCamera cvc;
    private CinemachineBasicMultiChannelPerlin perlin;
    private float shakeTimer;
    private float shakeTimerTotal;
    private float startingIntensity;

    private void Awake()
    {
        Instance = this;
        cvc = GetComponent<CinemachineVirtualCamera>();
    }

    public void ShakeCamera(float intensity, float time)
    {
        // 动态获取，防止 Awake 时机问题导致获取失败
        if (perlin == null && cvc != null)
        {
            perlin = cvc.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }

        if (perlin == null)
        {
            Debug.LogError("❌ CameraShakeManager: 找不到 Noise 组件！请检查虚拟相机是否配置了 Basic Multi Channel Perlin。");
            return;
        }

        Debug.Log($"✅ 触发相机震动！强度: {intensity}, 时间: {time}");
        
        perlin.m_AmplitudeGain = intensity;
        startingIntensity = intensity;
        shakeTimerTotal = time;
        shakeTimer = time;
    }

    private void Update()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0)
            {
                if (perlin != null) perlin.m_AmplitudeGain = 0f;
            }
            else
            {
                if (perlin != null)
                {
                    perlin.m_AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, 1 - (shakeTimer / shakeTimerTotal));
                }
            }
        }
    }
}