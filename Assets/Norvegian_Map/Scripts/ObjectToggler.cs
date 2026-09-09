using UnityEngine;
using UnityEngine.UI; // Обязательно для работы с компонентом Button
using UnityEngine.EventSystems; // Обязательно для управления фокусом UI

public class ObjectToggler : MonoBehaviour
{
    [Header("Список объектов (ровно 4 шт.)")]
    [SerializeField] private GameObject[] objects;

    [Header("Первая кнопка (для подсветки при старте)")]
    [SerializeField] private Button firstButton;

    private void Start()
    {
        // При старте показываем только первый объект
        ShowObject(0);

        // Делаем первую кнопку выделенной, чтобы она окрасилась в Selected Color
        if (firstButton != null && EventSystem.current != null)
        {
            firstButton.Select();
        }
    }

    /// <summary>
    /// Включает объект по его индексу (0, 1, 2 или 3) и скрывает остальные.
    /// </summary>
    public void ShowObject(int targetIndex)
    {
        if (objects == null || objects.Length == 0) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(i == targetIndex);
            }
        }
    }
}

