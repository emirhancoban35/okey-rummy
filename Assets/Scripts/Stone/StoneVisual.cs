using TMPro;
using UnityEngine;

public class StoneVisual : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_numberText;

    [SerializeField]
    private SpriteRenderer m_symbolRenderer;

    [SerializeField]
    private GameObject _faceUpVisual;

    public void SetVisual(StoneData stone)
    {
        if (stone.Type == StoneType.FakeJoker)
            m_numberText.text = "😄";
        else
            m_numberText.text = stone.Value.ToString();

        m_symbolRenderer.color = GetColorByStoneColor(stone.Color);
    }

    public void SetFace(bool isFaceUp)
    {
        _faceUpVisual.SetActive(isFaceUp);
        m_symbolRenderer.enabled = isFaceUp;
        m_numberText.enabled = isFaceUp;
    }

    private Color GetColorByStoneColor(StoneColor color)
    {
        return color switch
        {
            StoneColor.Red => Color.red,
            StoneColor.Blue => Color.blue,
            StoneColor.Yellow => Color.yellow,
            StoneColor.Black => Color.black,
            StoneColor.FakeJoker => Color.gray,
            _ => Color.white
        };
    }
}
