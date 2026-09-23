using UnityEngine;

public class SpriteBillboard : MonoBehaviour
{
    private Transform _cameraTransform;

    void Start()
    {
        // Находим главную камеру на сцене
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (_cameraTransform != null)
        {
            // 1. Копируем вращение камеры, чтобы спрайт был параллелен экрану
            transform.rotation = _cameraTransform.rotation;

            // 2. Разворачиваем спрайт на 180 градусов по Y, 
            // чтобы он смотрел на камеру «лицом», а не изнанкой
            //transform.Rotate(0, 180f, 0);
        }
    }
}

