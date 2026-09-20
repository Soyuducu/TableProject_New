using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [Header("Настройки вращения")]
    [Tooltip("Направление вращения (X, Y, Z)")]
    [SerializeField] private Vector3 rotationDirection = new Vector3(0f, 1f, 0f); // По умолчанию вокруг оси Y

    [Tooltip("Скорость вращения")]
    [SerializeField] private float rotationSpeed = 50f;

    void Update()
    {
        // Вращаем объект каждый кадр. 
        // Time.deltaTime делает вращение плавным и независимым от FPS.
        transform.Rotate(rotationDirection * rotationSpeed * Time.deltaTime);
    }
}
