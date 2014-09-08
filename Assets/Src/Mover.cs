using UnityEngine;
using System.Collections;

public class Mover : MonoBehaviour
{
    public float targetMoveSpeed = 3.0f;
    public float runCutoff = 0.01f;
    protected Vector3 targetPos;
    protected bool hasSetTarget;

    public void Awake()
    {
        ClearTarget();
    }
    
    public void ClearTarget()
    {
        hasSetTarget = false;
    }
    
    public void Update()
    {
        if ( !hasSetTarget )
        {
            return;
        }
        
        float distanceMag = ( transform.localPosition - targetPos ).sqrMagnitude;
        
        if ( distanceMag <= 0.0001f )
        {
            transform.localPosition = targetPos;
        }
        else
        {
            transform.localPosition = Vector3.Lerp( transform.localPosition, targetPos, Time.deltaTime * targetMoveSpeed );
        }
        
        Animator anim = GetComponentInChildren( typeof( Animator ) ) as Animator;
        if ( anim != null )
        {
            anim.SetBool( "running", distanceMag >= runCutoff );
        }
    }
    
    public void SetTargetPos( Vector3 pos )
    {
		//Debug.Log ("Pos: " + " X: " + pos.x + " Y: " + pos.y);
		//Debug.Log ("targetPos: " + " X: " + targetPos.x + " Y: " + targetPos.y);
        hasSetTarget = true;
        targetPos = pos;
    }
}