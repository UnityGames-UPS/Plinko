using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class ContainerClass : MonoBehaviour
{
   
    internal double risk;
    [SerializeField]
    internal TMP_Text riskText;
    internal float heightPosition;


    private void Start()
    {
        heightPosition = transform.localPosition.y;
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("marbles"))
        {
          transform
         .DOLocalMoveY(heightPosition - 15f, 0.2f)
         .SetEase(Ease.Flash)
         .OnComplete(() =>
         {
             transform.DOLocalMoveY(heightPosition, 0.3f)
                 .SetEase(Ease.Flash)
                 .SetUpdate(true);
         });
        }
    }
}
