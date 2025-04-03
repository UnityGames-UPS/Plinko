using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class Marbles : MonoBehaviour
{
    [SerializeField]
    internal Transform _transform;
    [SerializeField]
    internal int containerNum;
    [SerializeField]
    internal GameManager.marbleType marbleType;
    public Vector2 containerToFill;
    [SerializeField]  private CircleCollider2D collider;
    [SerializeField]
    private Rigidbody2D rb;
    [SerializeField]
    private float strength;
    internal double winAmount;
    public Collision2D collisionObj;
    Vector2 direction;
    public float correctionThreshold, correctionSpeed;
    internal string multiplier;
    internal GameManager game_Manager;
    [SerializeField]
    private float lerpSpeed;
   

    private void OnCollisionEnter2D(Collision2D collision)
    {
        
            collisionObj = collision;
          
         
        if (collision.gameObject.CompareTag("container"))
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
            game_Manager.updateBalance(winAmount, true);
            game_Manager.uiManager.resultHistoryAnimation(multiplier);       
            game_Manager.totalMarbleInAction--;
            game_Manager.checkForFallingMarbles();
            this.gameObject.SetActive(false);
        }
    }


    private void OnCollisionExit2D(Collision2D collision)
    {



        

    }

    internal void descentStart()
    {
        Debug.Log(game_Manager.name);
        rb.velocity = Vector2.zero;
        rb.isKinematic = false;
        StartCoroutine(fallLogic());
    }

    private IEnumerator fallLogic()
    {
        float randomFactor = 0;
        if (game_Manager._gameType == GameManager.gameType.TWELVE)
        {
             randomFactor = Random.Range(-10f, 10f);
        }
        else
        {
             randomFactor = Random.Range(-5, 5);
        }

        float deviationDamping = 0.8f; 

        while (true)
        {

            direction = (containerToFill - rb.position).normalized;
            randomFactor *= deviationDamping;
            Vector2 randomDeviation = new Vector2(randomFactor, 0).normalized;
            Vector2 targetVelocity = Vector2.Lerp(randomDeviation, direction, 0.9f) * strength;
            if (Vector2.Distance(rb.position, containerToFill) < correctionThreshold)
            {
                targetVelocity = direction * correctionSpeed; 
            }
            rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, Time.deltaTime * lerpSpeed);
//            Debug.Log(rb.position.y - containerToFill.y);
            yield return null;
        }
    }



}
