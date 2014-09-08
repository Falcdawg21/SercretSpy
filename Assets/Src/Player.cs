using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public Color[] playerColors;
    
    protected bool _isSpy;
    protected int _playerID;
    protected string _name;
    
    protected int _timesAskedThisTurn;
    protected int _timesAskedTotal;
    protected int _timesPushedThisTurn;
    protected int _timesPushedTotal;
	protected int _timesShiftedThisTurn;
	protected int _timesShiftedTotal;
	
	public void MoveToPosition( Vector3 position )
	{
		Mover mover = GetComponent( typeof( Mover ) ) as Mover;
		mover.SetTargetPos( position );
	}

	public void RotateToAngle( Vector3 angle )
	{
		Rotator rotator = GetComponent( typeof( Rotator ) ) as Rotator;
		rotator.SetTargetAngles( angle );
	}
    
    public int GetPlayerID()
    {
        return _playerID;
    }
    
    public void SetPlayerID( int playerID )
    {
        _playerID = playerID;
        
        if ( _name == null || _name.Length <= 0 )
        {
            _name = "Player " + ( _playerID + 1 );
        }
        
        Component[] cs = GetComponentsInChildren( typeof( Renderer ) );
        for ( int componentIndex = 0; componentIndex < cs.Length; componentIndex++ )
        {
            Renderer r = cs[ componentIndex ] as Renderer;
            r.material.color = GetPlayerColor();
        }
    }
    
    public string GetPlayerName()
    {
        return _name;
    }
    
    public void SetPlayerName( string newName )
    {
        _name = newName;
    }
    
    public Color GetPlayerColor()
    {
        if ( _playerID < 0 || _playerID >= playerColors.Length )
        {
            return playerColors[ 0 ];
        }
        
        return playerColors[ _playerID ];
    }
    
    public bool IsSpy()
    {
        return _isSpy;
    }
    
    public void SetIsSpy( bool isSpy )
    {
        _isSpy = isSpy;
    }
    
    public int GetTimesAskedThisTurn()
    {
        return _timesAskedThisTurn;
    }
    
    public void SetTimesAskedThisTurn( int timesAskedThisTurn )
    {
        _timesAskedThisTurn = timesAskedThisTurn;
    }
    
    public int GetTimesAskedTotal()
    {
        return _timesAskedTotal;
    }
    
    public void SetTimesAskedTotal( int timesAskedTotal )
    {
        _timesAskedTotal = timesAskedTotal;
    }
    
    public void AddAsk()
    {
        _timesAskedTotal++;
        _timesAskedThisTurn++;
	}
    
    public int GetTimesPushedThisTurn()
    {
        return _timesPushedThisTurn;
    }
    
    public void SetTimesPushedThisTurn( int timesPushedThisTurn )
    {
        _timesPushedThisTurn = timesPushedThisTurn;
    }
    
    public int GetTimesPushedTotal()
    {
        return _timesPushedTotal;
    }
    
    public void SetTimesPushedTotal( int timesPushedTotal )
    {
        _timesPushedTotal = timesPushedTotal;
    }
    
    public void AddPush()
    {
        _timesPushedTotal++;
        _timesPushedThisTurn++;
    }
 
	public int GetTimesShiftedThisTurn()
	{
		return _timesShiftedThisTurn;
	}
	
	public void SetTimesShiftedThisTurn( int timesShiftedThisTurn )
	{
		_timesShiftedThisTurn = timesShiftedThisTurn;
	}
	
	public int GetTimesShiftedTotal()
	{
		return _timesShiftedTotal;
	}
	
	public void SetTimesShiftedTotal( int timesShiftedTotal )
	{
		_timesShiftedTotal = timesShiftedTotal;
	}
	
	public void AddShift()
	{
		_timesShiftedTotal++;
		_timesShiftedThisTurn++;
	}
	
	public void ResetTurnTrackers()
	{
		_timesAskedThisTurn = 0;
		_timesPushedThisTurn = 0;
		_timesShiftedThisTurn = 0;
    }

	public void UpdateStatusFlag ( StatusFlag.StatusFlagState statusFlagState )
	{
		StatusFlag flag = GetComponentInChildren < StatusFlag > ();
		if ( flag )
		{
			flag.SetState ( statusFlagState );
		}
	}
}
