using UnityEngine;

public class SetRandomPosition : MonoBehaviour {
    private RectTransform m_CanvasRectTransform;
    private RectTransform m_RectTransform;
    
    private void Start() {
        m_CanvasRectTransform = m_CanvasRectTransform == null ? GetComponentInParent<Canvas>().GetComponent<RectTransform>() : null;
        m_RectTransform = m_RectTransform == null ? GetComponent<RectTransform>() : null; 
    }
    public void Trigger() {
        Vector2 canvasSize = m_CanvasRectTransform.rect.size;
        Vector2 buttonSize = m_RectTransform.rect.size;

        float minX = -canvasSize.x / 2 + buttonSize.x / 2;
        float maxX = canvasSize.x / 2 - buttonSize.x / 2;

        float minY = -canvasSize.y / 2 + buttonSize.y / 2;
        float maxY = canvasSize.y / 2 - buttonSize.y / 2;

        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);

        m_RectTransform.anchoredPosition = new Vector2(randomX, randomY);
    }
}