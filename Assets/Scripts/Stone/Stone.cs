using UnityEngine;

public class Stone : MonoBehaviour
{
    private StoneData _data;
    private StoneVisual _visual;

    public StoneData Data => _data;

    public void Initialize(StoneData data)
    {
        _data = data;
        _visual = GetComponent<StoneVisual>();
        _visual.SetVisual(data);
    }

}
