using UnityEngine; 

public class FpsCounter : MonoBehaviour
{
private float deltaTime = 0.0f; 

void Update()
{
// Вычисляем плавное изменение времени между кадрами
deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
}

void OnGUI()
{
int w = Screen.width, h = Screen.height;
// Настройка стиля текста
GUIStyle style = new GUIStyle();
Rect rect = new Rect(10, 10, w, h * 2 / 100);
style.alignment = TextAnchor.UpperLeft;
style.fontSize = h * 2 / 50; // Размер шрифта зависит от разрешения экрана
style.normal.textColor = Color.green; // Зеленый цвет текста

// Расчет FPS и миллисекунд
float msec = deltaTime * 1000.0f;
float fps = 1.0f / deltaTime;
string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

// Отрисовка на экране
GUI.Label(rect, text, style);

}

}
