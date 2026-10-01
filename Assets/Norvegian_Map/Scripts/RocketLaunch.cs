using System.Collections;
using UnityEngine;

public class RocketLaunch : MonoBehaviour
{
    [Header("Движение")]
    [Tooltip("Начальная скорость ракеты при старте (м/с)")]
    public float startSpeed = 0f;
    [Tooltip("Ускорение ракеты (м/с²)")]
    public float acceleration = 10f;
    
    [Header("Параболическая траектория")]
    [Tooltip("Финальный наклон по оси X в самом конце полета (в градусах)")]
    public float maxTiltAngleX = 45f;
    [Tooltip("Финальный наклон по оси Y в самом конце полета (в градусах)")]
    public float maxTiltAngleY = 0f;

    [Header("Тайминги и Цикл")]
    [Tooltip("Время ожидания на стартовом столе ДО взлета (в секундах)")]
    public float idleOnPadDuration = 3f;
    [Tooltip("Время активного полета до исчезновения (в секундах)")]
    public float flightDuration = 5f;
    [Tooltip("Задержка в невидимости перед появлением на столе (в секундах)")]
    public float respawnDelay = 2f;

    [Header("Эффекты (Particle Systems)")]
    [Tooltip("Эффект зажигания/огня на земле (остается на стартовой площадке)")]
    public ParticleSystem launchPadEffect;
    [Tooltip("След/трейл, который летит вместе с ракетой")]
    public ParticleSystem rocketTrailEffect;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float currentSpeed;
    private bool isFlying = false;
    private float timeElapsedSinceLaunch;
    
    private MeshRenderer meshRenderer;
    private Collider rocketCollider;

    void Start()
    {
        // Запоминаем стартовую позицию и ротацию площадки
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;

        // Находим компоненты для скрытия/показа
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
        rocketCollider = GetComponent<Collider>();

        // Запуск бесконечного цикла космодрома
        StartCoroutine(LaunchCycle());
    }

    void Update()
    {
        if (isFlying)
        {
            // Считаем время, прошедшее с момента отрыва от земли
            timeElapsedSinceLaunch += Time.deltaTime;
            
            // Нормализуем время от 0 (старт) до 1 (конец полета)
            float progress = Mathf.Clamp01(timeElapsedSinceLaunch / flightDuration);

            // Увеличиваем скорость за счет ускорения
            currentSpeed += acceleration * Time.deltaTime;

            // Движение вперед (для ракеты это ее локальное направление вверх - Vector3.up)
            transform.Translate(Vector3.up * currentSpeed * Time.deltaTime);

            // Формируем параболу: наклон зависит от прогресса времени (t).
            // Ракета плавно переходит от стартовой ротации к финальной.
            Quaternion maxRotation = startRotation * Quaternion.Euler(maxTiltAngleX, maxTiltAngleY, 0);
            transform.rotation = Quaternion.Slerp(startRotation, maxRotation, progress);
        }
    }

    private IEnumerator LaunchCycle()
    {
        while (true)
        {
            // 1. ПОЯВЛЕНИЕ НА СТАРТОВОМ СТОЛЕ
            transform.localPosition = startPosition;
            transform.localRotation = startRotation;
            currentSpeed = startSpeed;
            timeElapsedSinceLaunch = 0f;
            SetRocketActive(true);

            // Сброс эффектов
            if (launchPadEffect != null) launchPadEffect.Stop();
            if (rocketTrailEffect != null) rocketTrailEffect.Stop();

            // РАКЕТА СТОИТ НА СТОЛЕ
            yield return new WaitForSeconds(idleOnPadDuration);

            // 2. ЗАЖИГАНИЕ И СТАРТ
            if (launchPadEffect != null) launchPadEffect.Play();
            if (rocketTrailEffect != null) rocketTrailEffect.Play();

            // Включаем движение в Update
            isFlying = true;

            // РАКЕТА ЛЕТИТ ПО ПАРАБОЛЕ
            yield return new WaitForSeconds(flightDuration);

            // 3. ОТКЛЮЧЕНИЕ ОБЪЕКТА (Окончание полета)
            isFlying = false;
            if (rocketTrailEffect != null) rocketTrailEffect.Stop();
            SetRocketActive(false);

            // 4. ПАУЗА ПЕРЕД СЛЕДУЮЩИМ ПОЯВЛЕНИЕМ НА СТОЛЕ
            yield return new WaitForSeconds(respawnDelay);
        }
    }

    private void SetRocketActive(bool active)
    {
        if (meshRenderer != null) meshRenderer.enabled = active;
        if (rocketCollider != null) rocketCollider.enabled = active;
    }
}
