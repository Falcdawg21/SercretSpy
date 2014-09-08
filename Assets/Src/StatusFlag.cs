using UnityEngine;
using System.Collections;

public class StatusFlag : MonoBehaviour 
{
	public enum StatusFlagState
	{
		Off = 0,
		Turn,
		Ask
	};


	public float floatSpan = 2.0f;
	public float speed = 1.0f;
	protected float _startY = 0.0f;

	public Sprite arrowSprite;
	public Sprite questionMarkSprite;

	// Use this for initialization
	void Start () 
	{
		_startY = transform.localPosition.y;

		SetState ( StatusFlagState.Off );
	}
	
	// Update is called once per frame
	void Update () 
	{
		transform.forward = Camera.main.transform.forward;
		//transform.position = Camera.main.transform.forward * 2;

		Vector3 position = transform.localPosition;
		position.x = 0.0f;
		position.y = _startY + Mathf.Sin(Time.time * speed) * floatSpan / 4.0f;
		position.z = 0.0f;

		transform.localPosition = position;
	}

	public void SetState ( StatusFlagState state )
	{
		SpriteRenderer renderer = GetComponent < SpriteRenderer > ();

		switch ( state )
		{
			case StatusFlagState.Off:
				renderer.enabled = false;
				break;

			case StatusFlagState.Turn:
				renderer.enabled = true;
				renderer.material.color = Color.white;
				renderer.color = Color.white;
				renderer.sprite = arrowSprite;
				break;

			case StatusFlagState.Ask:
				renderer.enabled = true;
				renderer.material.color = Color.white;
				renderer.color = Color.yellow;
				renderer.sprite = questionMarkSprite;
				break;
		}
	}
}
