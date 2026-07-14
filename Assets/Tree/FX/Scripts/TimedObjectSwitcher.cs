using UnityEngine;
using System.Collections.Generic;

public class TimedObjectSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class TimedObject
    {
        public GameObject obj;
        [Tooltip("Задержка перед включением (в секундах) от начала шага")]
        public float delayBeforeActive;
        [Tooltip("Через сколько секунд выключить объект (Игнорируется, если включена галочка ниже)")]
        public float durationOfActive;
        [Tooltip("Если true, объект НЕ выключится сам и будет гореть до конца текущего шага")]
        public bool stayActiveUntilEnd = false;

        [HideInInspector] public bool isActivated;
        [HideInInspector] public bool isDeactivated;
        [HideInInspector] public float currentActiveTime;
    }

    [System.Serializable]
    public class TimedGroup
    {
        [Tooltip("Общая длительность этого шага (после нее начнется следующий шаг)")]
        public float totalStepDuration;
        [Tooltip("Если true, объекты этой группы НЕ будут принудительно выключаться при переходе к следующему шагу")]
        public bool keepActiveInNextSteps = false;
        public List<TimedObject> objects;
    }

    public List<TimedGroup> sequence;
    public bool loop = true;

    private int currentIndex = 0;
    private float stepTimer;

    void Start()
    {
        // Выключаем абсолютно все объекты во всех группах на старте
        foreach (var group in sequence)
        {
            if (group.objects == null) continue;
            foreach (var item in group.objects)
            {
                if (item.obj != null) item.obj.SetActive(false);
            }
        }

        ResetGroupTimers(currentIndex);
    }

    void Update()
    {
        if (sequence.Count == 0) return;

        TimedGroup currentGroup = sequence[currentIndex];
        stepTimer += Time.deltaTime;

        // Управляем объектами внутри текущей группы
        if (currentGroup.objects != null)
        {
            foreach (var item in currentGroup.objects)
            {
                if (item.obj == null) continue;

                // 1. Логика включения по задержке
                if (!item.isActivated && stepTimer >= item.delayBeforeActive)
                {
                    item.obj.SetActive(true);
                    item.isActivated = true;
                }

                // 2. Логика выключения по длительности (только если НЕ включен stayActiveUntilEnd)
                if (item.isActivated && !item.isDeactivated && !item.stayActiveUntilEnd)
                {
                    item.currentActiveTime += Time.deltaTime;
                    if (item.currentActiveTime >= item.durationOfActive)
                    {
                        item.obj.SetActive(false);
                        item.isDeactivated = true;
                    }
                }
            }
        }

        // Переключение на следующий шаг по истечении totalStepDuration
        if (stepTimer >= currentGroup.totalStepDuration)
        {
            // Выключаем объекты текущей группы, ТОЛЬКО если не разрешено оставить их в следующих шагах
            if (!currentGroup.keepActiveInNextSteps)
            {
                ForceDeactivateGroup(currentIndex);
            }

            currentIndex++;

            if (currentIndex >= sequence.Count)
            {
                if (loop)
                {
                    currentIndex = 0;
                    // При полном зацикливании гасим вообще всё, чтобы начать чистый цикл сначала
                    for (int i = 0; i < sequence.Count; i++)
                    {
                        ForceDeactivateGroup(i);
                    }
                }
                else
                {
                    enabled = false; // Отключаем Update
                    return;
                }
            }

            ResetGroupTimers(currentIndex);
        }
    }

    void ResetGroupTimers(int index)
    {
        stepTimer = 0f;
        if (index >= 0 && index < sequence.Count && sequence[index].objects != null)
        {
            foreach (var item in sequence[index].objects)
            {
                item.isActivated = false;
                item.isDeactivated = false;
                item.currentActiveTime = 0f;
            }
        }
    }

    void ForceDeactivateGroup(int index)
    {
        if (index >= 0 && index < sequence.Count && sequence[index].objects != null)
        {
            foreach (var item in sequence[index].objects)
            {
                if (item.obj != null) item.obj.SetActive(false);
            }
        }
    }
}
