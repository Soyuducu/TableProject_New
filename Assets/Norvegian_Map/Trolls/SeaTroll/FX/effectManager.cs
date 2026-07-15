using UnityEngine;
using System.Collections;

public class EffectManager_Water : MonoBehaviour
{
    public ParticleSystem effectA;   // первый эффект
    public ParticleSystem effectB;   // второй эффект

    public float delayA = 1.5f;
    public float delayB = 2.0f;

    public void PlayEffectA()
    {
        //StartCoroutine(PlayWithDelay(effectA, delayA));
        if (effectA != null)
        {
            effectA.Play();
        }
    }

    public void PlayEffectB()
    {
        if (effectB != null)
        {
            effectB.Play();
        }
    }

    IEnumerator PlayWithDelay(ParticleSystem ps, float delay)
    {
        if (ps == null)
        {
            Debug.LogError("ParticleSystem не назначен!");
            yield break;
        }

        yield return new WaitForSeconds(delay);

        ps.Play();
        Debug.Log($"Запущен эффект {ps.name} в позиции {ps.transform.position}");
    }
    
}
