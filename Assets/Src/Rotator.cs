using UnityEngine;
using System.Collections;

public class Rotator : MonoBehaviour 
{
	public float targetRotateSpeed = 3.0f;
	protected Quaternion targetAngle = Quaternion.identity;
    protected bool hasSetTarget;

    public void Awake()
    {
        ClearTarget();
    }

	public void SetTargetAngles( Vector3 angles )
	{
        hasSetTarget = true;
		targetAngle = Quaternion.Euler( angles );
	}
    
    public void ClearTarget()
    {
        hasSetTarget = false;
    }

	void Update () 
	{
        if ( !hasSetTarget )
        {
            return;
        }
        
		if( ( transform.rotation.eulerAngles - targetAngle.eulerAngles ).sqrMagnitude <= 0.0001f )
		{
			//Debug.Log( "Instant" );
			transform.localRotation = targetAngle;
		}
		else
		{
			transform.localRotation = Quaternion.Lerp( transform.localRotation, targetAngle, Time.deltaTime * targetRotateSpeed );
		}
	}
}
