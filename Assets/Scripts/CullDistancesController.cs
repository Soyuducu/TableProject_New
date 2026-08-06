using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CullDistancesController : MonoBehaviour
{
    [Header("Дистанция для мелких объектов (в метрах)")]
    [SerializeField] private float smallPropsDistance = 25f;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        
        // Массив из 32 слоев Unity
        float[] distances = new float[32];

        // Получаем ID слоя по его имени "SmallProps"
        int layerIndex = LayerMask.NameToLayer("SmallProps");

        if (layerIndex != -1)
        {
            // Задаем дистанцию отключения (например, 25 метров)
            distances[layerIndex] = smallPropsDistance;
            
            // Передаем массив камере
            cam.layerCullDistances = distances;
        }
        else
        {
            Debug.LogWarning("Слой 'SmallProps' не найден в настройках проекта!");
        }
    }
}

