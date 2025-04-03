using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class ResultHistory : MonoBehaviour
{
    [SerializeField]
    internal TMP_Text multiplierText;
    [SerializeField]
    internal RectTransform _transform;
    [SerializeField]
    internal int heightQueuePosition;
    [SerializeField]
    internal UiManager uiManager;


    internal void nextPosition(string multiplier)
    {
       
        heightQueuePosition++;
        if (heightQueuePosition > uiManager.multiplierObjs.Count - 1)
        {
            heightQueuePosition = 0;
            multiplierText.text = multiplier;
            _transform.localPosition = new Vector2(_transform.localPosition.x, uiManager.startPosition);
         
        }
        
        _transform.DOLocalMoveY(uiManager.multiplierObjsYPositions[heightQueuePosition], 0.1f).SetEase(Ease.Flash);
       
       
    }

}
