using UnityEngine;
using UnityEngine.EventSystems; // Обязательно для фиксации кликов

// Реализуем интерфейс IPointerClickHandler
public class ObjectClickOpener : MonoBehaviour, IPointerClickHandler
{
    [Header("Настройки UI")]
    [SerializeField] private GameObject windowToOpen; // Ссылка на окно UI, которое нужно открыть

    // Этот метод вызывается автоматически при клике на коллайдер объекта
    public void OnPointerClick(PointerEventData eventData)
    {
        if (windowToOpen != null)
        {
            windowToOpen.SetActive(true); // Включаем UI окно
        }
        else
        {
            Debug.LogWarning("Окно для открытия не назначено в инспекторе!", gameObject);
        }
    }
}
