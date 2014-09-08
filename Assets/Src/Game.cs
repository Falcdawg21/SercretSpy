using UnityEngine;
using System.Collections;

public class Game : MonoBehaviour {

    public enum State
    {
        Turns = 0,
        Telling,
        WaitingForServer,
		Won,
		Lost
    };
    
    public enum TurnState
    {
        WaitingForChoice = 0,
        WaitingForTell,
        ChoosingPushedPlayer,
        SelectingMove,
        SelectingShiftTile,
        SelectingShiftDirection,
        SelectingAsk,
        SelectingPushDirection
    };

	public GameObject gridPrefab;
    public GameObject playerPrefab;
    public GameObject testServerPrefab;

    public bool canAskOwnTiles = true;
    public bool canShiftSelf = false;
    public bool canShiftOthers = true;
    public bool canShiftGoal = true;
    
    public bool askUsesTurn = false;
    public int maxAskCount = -1;
    public int maxAskPerTurnCount = 1;
    
    public bool pushUsesTurn = false;
    public int maxPushCount = -1;
    public int maxPushPerTurnCount = 1;

	public bool shiftUsesTurn = false;
	public int maxShiftCount = -1;
	public int maxShiftPerTurnCount = 1;
	
	public bool spyCanMoveThroughOwnTraps = true;
    
    public float timeToShowTeam = 10.0f;

	private static Game _instance;

	private static Vector2 _origin;
	private static Vector2 _end;
	private static Vector2 _contentSize;

	private static Grid _grid;
    
	private Player _localPlayer;
	private int _localPlayerID;
    
    private State state;
    private TurnState turnState;
    private int _currentPlayerID;
    private Vector2 _selectedGridPosForShift;
    
    private Vector2 _tellingGridPos;
    private int _pushedPlayerID;
    
    private ArrayList playerIDsMissingNextTurn;
    
    protected float timeStarted;
	private ArrayList _highlightedTiles;

	public static Game GetInstance() 
	{
		return _instance;
	}

	void Awake() 
	{
		_instance = this;
        playerIDsMissingNextTurn = new ArrayList();

		_origin = Camera.main.ViewportToWorldPoint( new Vector3( 0.0f, 0.0f, 0.0f ) );
		_end = Camera.main.ViewportToWorldPoint( new Vector3( 1.0f, 1.0f, 0.0f ) );
		_contentSize = _end - _origin;

        state = State.Turns;

		GameObject obj = Object.Instantiate( gridPrefab ) as GameObject;
		_grid = obj.GetComponent( typeof( Grid ) ) as Grid;
        
        //for testing - if you start from MainScene there is no server
        if ( Server.GetInstance() == null )
        {
            Object.Instantiate( testServerPrefab );
            Server.GetInstance().StartServer();
            Server.GetInstance().TellClientsToBeginGame();
        }
	}

	// Use this for initialization
	void Start() 
	{
		Vector2 size = new Vector2( 9, 9 );
		_grid.Initialize( size );
		
		// Scale the grid to fit within the screen.
		float scale = Mathf.Min(_contentSize.x / (size.x * _grid.tileSize.x), _contentSize.y / (size.y * _grid.tileSize.y));
		_grid.transform.localScale = new Vector3( scale, scale, scale );
		
		// Move the grid so it is positioned at the top of the screen.
		Vector3 position = new Vector3( _origin.x, _end.y - ( ( ( size.y + 1 ) * _grid.tileSize.y ) * scale ), _grid.transform.position.z );
		_grid.transform.position = position;

		_localPlayerID = -1;
		_localPlayer = null;
        _pushedPlayerID = -1;
        
        timeStarted = Time.time;
        Client.GetInstance().TellServerIAmReady();
	}
	
	// Update is called once per frame
	void Update ()
    {
        //look for touches if it's my turn and I need to get them for something
        if ( IsMyTurn() && turnState >= TurnState.SelectingMove )
        {
	        if ( Input.GetMouseButtonUp( 0 ) )
            {
                Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
                RaycastHit hit;
                if ( Physics.Raycast( ray, out hit, Mathf.Infinity, Constants.TILE_MASK ) )
                {
                    Tile tile = hit.collider.GetComponent( typeof( Tile ) ) as Tile;
                    
                    if ( tile != null )
                    {
                        SelectTile( tile );
                    }
                }
            }
        }
        else if ( IsMyTurn() && turnState == TurnState.ChoosingPushedPlayer )
        {
	        if ( Input.GetMouseButtonUp( 0 ) )
            {
                Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
                RaycastHit[] hits = Physics.RaycastAll( ray, Mathf.Infinity, Constants.PLAYER_MASK );
                for ( int hitIndex = 0; hitIndex < hits.Length; hitIndex++ )
                {
                    RaycastHit hit = hits[ hitIndex ];
                    Player player = hit.collider.GetComponent( typeof( Player ) ) as Player;
                    
                    if ( player != null && player != GetLocalPlayer() )
                    {
                        _pushedPlayerID = player.GetPlayerID();
                        SetTurnState( TurnState.SelectingPushDirection );
                        break;
                    }
                }
            }
        }
	}

	public static Vector3 GetOrigin() {
		return _origin;
	}

	public static Vector3 GetEnd() {
		return _end;
	}
	
	public static Vector3 GetContentSize() {
		return _contentSize;
	}
    
    public int GetCurrentPlayerID()
    {
        return _currentPlayerID;
    }

    public Grid GetGrid()
    {
        return _grid;
    }
    
    public Player CreatePlayer( int playerId, Vector2 gridPos )
    {
        GameObject playerObj = Object.Instantiate( playerPrefab ) as GameObject;
        Player p = playerObj.GetComponent( typeof( Player ) ) as Player;
        p.SetPlayerID( playerId );
		p.transform.parent = _grid.transform;
		_grid.AddPlayer( p, gridPos );
        
        return p;
    }
    
	public Player GetLocalPlayer()
	{
		return _localPlayer;
	}
    
    public int GetLocalPlayerID()
    {
        return _localPlayerID;
    }
    
    public void SetLocalPlayerID( int setID )
    {
        _localPlayerID = setID;
        _localPlayer = GetPlayerForID( setID );
        
        //once we have this, we can choose which tiles in the maze are ours
        _grid.InitializeTiles( setID );
    }
    
    public void SetLocalPlayerInfo( bool isSpy )
    {
        _localPlayer.SetIsSpy( isSpy );
    }

	public int GetPlayerCount()
	{
		return _grid.GetPlayerCount();
	}
    
    public Player GetPlayerForID( int playerID )
    {
        for ( int playerIndex = 0; playerIndex < _grid.GetPlayerCount(); playerIndex++ )
        {
            Player p = _grid.GetPlayerAt( playerIndex );
            if ( p.GetPlayerID() == playerID )
            {
                return p;
            }
        }
        
        return null;
    }
   
	public void MakePlayerMissNextTurn( int playerID )
    {
        if ( !WillPlayerMissTurn( playerID ) )
        {
            playerIDsMissingNextTurn.Add( playerID );
        }
    }
    
	public void PlayerWon( bool won )
	{
		if( won )
		{
			SetState( State.Won );
		}
		else
		{
			SetState( State.Lost );
		}
        
        Animator anim = GetLocalPlayer().GetComponentInChildren( typeof( Animator ) ) as Animator;
        if ( anim != null )
        {
            anim.SetBool( won ? "won" : "lost", true );
        }
	}

	public void Restart()
	{
		Server.GetInstance().TellClientsToBeginGame();
	}

	public void MakePlayerWin( int playerID )
	{
		Client.GetInstance().TellServerPlayerWon( playerID );
	}
	
	public void PlayerMissedTurn( int playerID )
	{
		playerIDsMissingNextTurn.Remove( playerID );
	}
	
	public bool WillPlayerMissTurn( int playerID )
	{
		return playerIDsMissingNextTurn.Contains( playerID );
	}
	
	public bool IsTileBad( Vector2 gridPos )
	{
		Tile tile = _grid.GetTileAt( gridPos );
		return tile.GetStatus() == Tile.Status.Bad;
	}
	
	public bool IsTileGoal( Vector2 gridPos )
	{
		Tile tile = _grid.GetTileAt( gridPos );
		return tile.GetStatus() == Tile.Status.Good;
	}
	
    //server only function
	public void EvaluatePlayerMove( int playerID, Vector2 targetPos )
	{
		//is this a bad space? if so, that player needs to lose their next turn
		if( IsTileBad( targetPos ) ) 
		{
            DoBadTileMove( playerID, targetPos );
		}
		else if( IsTileGoal( targetPos ) ) 
		{
			// Player Wins!!!
			MakePlayerWin( playerID );
		}
	}
    
    //server only function
    protected void DoBadTileMove( int playerID, Vector2 targetPos )
    {
        if ( !spyCanMoveThroughOwnTraps )
        {
            MakePlayerMissNextTurn (playerID);
            return;
        }
                
        //if the player moving into the space is the spy, and the space is his own, then he
        //doesn't get hurt! But we need to quickly update the visual for the space on
        //everyone else's machines
        Player p = GetPlayerForID( playerID );
        Tile t = _grid.GetTileAt( targetPos );
        if ( p.IsSpy() && t.GetOwnerID() == p.GetPlayerID() )
        {
            //there must be nobody else on the tile
            if ( _grid.GetPlayersAtGridPosition( targetPos ).Count == 0 )
            {
                Server.GetInstance().TellNonSpyClientsToUpdateTileVisual( targetPos, false );
            }
        }
        else
        {
            int spyID = _grid.GetPlayerAt( 0 ).GetPlayerID();
            for ( int playerIndex = 1; playerIndex < _grid.GetPlayerCount(); playerIndex++ )
            {
                Player other = _grid.GetPlayerAt( playerIndex );
                if ( other.IsSpy() )
                {
                    spyID = playerIndex;
                    break;
                }
            }
            
            //if they're not a spy and they're moving onto a space that is the spy's,
            //update it as a trap to everyone in case the spy messed it up before
            if ( !p.IsSpy() && t.GetOwnerID() == spyID )
            {
                Server.GetInstance().TellNonSpyClientsToUpdateTileVisual( targetPos, true );
            }
            
		    MakePlayerMissNextTurn (playerID);
        }
    }
	
    public bool ShouldPlayerMissTurn( int playerID, Vector2 gridPos )
    {
        Tile t = _grid.GetTileAt( gridPos );
        return t.GetStatus() == Tile.Status.Bad;
    }
    
    public void DoMissedTurnCheck( int playerID, Vector2 targetPos )
    {
        //is this a bad space? if so, that player needs to lose their next turn
        if ( ShouldPlayerMissTurn( playerID, targetPos ) )
        {
            MakePlayerMissNextTurn( playerID );
        }
    }

    public State GetState()
    {
        return state;
    }
    
    public void SetState( State newState )
    {
		// Block other states when we are in the won or lost state.
		if( state == State.Won ||
			state == State.Lost ) 
		{
			return;
		}
        
        state = newState;
    }
    
    public TurnState GetTurnState()
    {
        return turnState;
    }
    
    public void SetTurnState( TurnState setState )
    {
        turnState = setState;
    }
    
    public void ResetStates()
    {
        SetState( State.Turns );
        SetTurnState( TurnState.WaitingForChoice );
    }
    
    public bool IsMyTurn()
    {
        return state == State.Turns && _localPlayerID == _currentPlayerID;
    }
    
    public bool IsBeingAsked()
    {
		return state == State.Telling;// && askedPlayerIndex == _localPlayerID;
    }
    
    public bool OtherPlayerIsAtMyPosition()
    {
		for( int playerID = 0; playerID < _grid.GetPlayerCount(); playerID++ )
        {
			if( playerID == _localPlayerID )
			{
				continue;
			}

			Player otherPlayer = _grid.GetPlayerAt( playerID );
			if ( DoesPlayerShareGridPositionWithPlayer( _localPlayer, otherPlayer ) )
			{
				return true;
			}
        }
        
        return false;
    }
    
    //the server calls this
    public bool IsValidShiftTile( int playerID, Vector2 tilePos )
    {
        return IsValidShiftTile( GetPlayerForID( playerID ), tilePos );
    }
    
    //the client calls this
    public bool IsValidShiftTile( Player player, Vector2 tilePos )
    {
        Vector2 playerPos = _grid.ConvertLocalPosToGridPos( player.transform.localPosition );
        return ( tilePos.x - playerPos.x == 0 && Mathf.Abs( tilePos.y - playerPos.y ) == 1 ) ||
               ( Mathf.Abs( tilePos.x - playerPos.x ) == 1 && tilePos.y - playerPos.y == 0 );
    }
    
    //the server calls this
    public bool IsValidShiftDirection( int playerID, Vector2 tilePos, Grid.Directions direction )
    {
        return IsValidShiftDirection( GetPlayerForID( playerID ), tilePos, direction );
    }
    
    //the client calls this
    public bool IsValidShiftDirection( Player player, Vector2 tilePos, Grid.Directions direction )
    {
        if ( direction == Grid.Directions.None )
        {
            return false;
        }
        
        if ( !canShiftGoal )
        {
            Vector2 goalPos = new Vector2( 0, 0 );
            if ( _grid.GridPosLiesWithinShift( goalPos, tilePos, direction ) )
            {
                return false;
            }
        }
        
        if ( !canShiftSelf )
        {
            if ( _grid.PlayerLiesWithinShift( player, tilePos, direction ) )
            {
                return false;
            }
        }
        
        if ( !canShiftOthers )
        {
            if ( _grid.AnyPlayerLiesWithinShift( tilePos, direction ) )
            {
                return false;
            }
        }
        
        return true;
    }
    
    public bool CanPerformAsk()
    {
        return CanPerformAsk( GetLocalPlayer() );
    }
    
    public bool CanPerformAsk( int playerID )
    {
        return CanPerformAsk( GetPlayerForID( playerID ) );
    }
    
    public bool CanPerformAsk( Player player )
    {
        return ( maxAskPerTurnCount < 0 || player.GetTimesAskedThisTurn() < maxAskPerTurnCount ) &&
               ( maxAskCount        < 0 || player.GetTimesAskedTotal()    < maxAskCount );
    }
    
    //the server calls this
    public bool CanAskAboutTile( int playerID, Vector2 tilePos )
    {
        return CanAskAboutTile( GetPlayerForID( playerID ), tilePos );
    }
    
    public bool CanSteal( int playerID )
    {
        return CanSteal( GetPlayerForID( playerID ) );
    }
    
    public bool CanSteal( Player player )
    {
        return false;
    }

	public void HighlightShiftableTiles()
	{
		_highlightedTiles = new ArrayList();
		FlattenedTileArray tiles = _grid.GetTiles();
		for( int x = 0; x < tiles.GetLength( 0 ); x++ ) 
		{
			for( int y = 0; y < tiles.GetLength( 1 ); y++ ) 
			{
				for( int dir = (int)Grid.Directions.Up; dir < (int)Grid.Directions.Size; dir++ )
				{
					if( IsValidShiftDirection( GetLocalPlayer(), new Vector2( x, y ), (Grid.Directions)dir ) && IsValidShiftTile( GetLocalPlayer(), new Vector2( x, y ) ) ) 
					{
						Tile tile = tiles.Get( x, y );
						tile.GetHighlightFader().FadeIn();
						_highlightedTiles.Add( tile );

						(tile.hightlight.renderer as SpriteRenderer).color = GetLocalPlayer().GetPlayerColor();
					}
				}
			}
		}
	}

	public void ClearHighlightedTiles()
	{
		if(_highlightedTiles == null) 
		{
			return;
		}

		for (int i = 0; i < _highlightedTiles.Count; i++) 
		{
			Tile tile = (Tile)_highlightedTiles[ i ];

			if( tile != null && tile.GetHighlightFader().IsShowing() )
			{
			    tile.GetHighlightFader().FadeOut();
			}
		}
	}
    
    //the client calls this
    public bool CanAskAboutTile( Player player, Vector2 tilePos )
    {
        Tile tile = _grid.GetTileAt( tilePos );
        
        //you can never ask about the goal
        if ( tile.GetStatus() == Tile.Status.Good )
        {
            return false;
        }
        
        //you can never ask about a tile where another player is standing
        for ( int playerIndex = 0; playerIndex < _grid.GetPlayerCount(); playerIndex++ )
        {
            Vector2 playerPos = _grid.ConvertLocalPosToGridPos( _grid.GetPlayerAt( playerIndex ).transform.localPosition );
            if ( playerPos == tilePos )
            {
                return false;
            }
        }
        
        //we can optionally decide to allow asking about our own tiles
        if ( canAskOwnTiles )
        {
            return true;
        }
        //if not, then we can only ask if we don't own this tile
        else
        {
            return tile.GetOwnerID() != player.GetPlayerID();
        }
    }
    
    public bool CanPerformPush()
    {
        return CanPerformPush( GetLocalPlayer() );
    }
    
    public bool CanPerformPush( int playerID )
    {
		return CanPerformPush( GetPlayerForID( playerID ) );
    }
    
    public bool CanPerformPush( Player player )
    {
        return ( maxPushPerTurnCount < 0 || player.GetTimesPushedThisTurn() < maxPushPerTurnCount ) &&
               ( maxPushCount        < 0 || player.GetTimesPushedTotal()    < maxPushCount );
    }

	public bool CanPerformShift()
	{
		return CanPerformShift( GetLocalPlayer() );
	}

	public bool CanPerformShift( int playerID )
	{
		return CanPerformShift( GetPlayerForID( playerID ) );
	}
	
	public bool CanPerformShift( Player player )
	{
		return ( maxShiftPerTurnCount < 0 || player.GetTimesShiftedThisTurn() < maxShiftPerTurnCount ) &&
			   ( maxShiftCount        < 0 || player.GetTimesShiftedTotal()    < maxShiftCount );
	}
	
	public bool CanShiftInDirection( Grid.Directions dir )
	{
		return ( IsValidShiftDirection( GetLocalPlayer(), new Vector2( _selectedGridPosForShift.x, _selectedGridPosForShift.y ), dir ) );
	}
    
    public void SelectTile( Tile tile )
    {
		Vector2 gridPos = _grid.ConvertLocalPosToGridPos( tile.transform.localPosition );
        
        if ( turnState == TurnState.SelectingMove )
        {
            tile.GetSelectionFader().Pulse();
			Vector2 myPos = _grid.ConvertLocalPosToGridPos( GetLocalPlayer().transform.localPosition );

			Grid.Directions dir = _grid.GetDirectionFromDelta( gridPos - myPos );
            
            if ( dir != Grid.Directions.None )
            {
                SetState( State.WaitingForServer );
                Client.GetInstance().PerformMove( dir );
                SetTurnState( TurnState.WaitingForChoice );
            }
        }
        else if ( turnState == TurnState.SelectingShiftTile )
        {
            if ( IsValidShiftTile( GetLocalPlayer(), gridPos ) )
            {
				ClearHighlightedTiles();

                tile.GetSelectionFader().FadeIn();
                _selectedGridPosForShift = gridPos;
                SetTurnState( TurnState.SelectingShiftDirection );
            }
            else
            {
                tile.GetSelectionFader().Pulse();
            }
        }
        else if ( turnState == TurnState.SelectingShiftDirection )
        {
			// Commenting Out!
			// Selected now using GUI buttons!

//            tile.GetSelectionFader().Pulse();
//			Grid.Directions dir = _grid.GetDirectionFromDelta( gridPos - _selectedGridPosForShift );
//            
//            if ( IsValidShiftDirection( GetLocalPlayer(), _selectedGridPosForShift, dir ) )
//            {
//                _grid.GetTileAt( _selectedGridPosForShift ).GetSelectionFader().FadeOut();
//                SetState( State.WaitingForServer );
//                Client.GetInstance().PerformShift( _selectedGridPosForShift, dir );
//                SetTurnState( TurnState.WaitingForChoice );
//            }
//            else
//            {
//                tile.GetSelectionFader().Pulse();
//            }
        }
        else if ( turnState == TurnState.SelectingAsk )
        {
            if ( CanAskAboutTile( GetLocalPlayer(), gridPos ) )
            {
                tile.GetSelectionFader().FadeIn();
                
                Client.GetInstance().PerformAsk( gridPos );
                SetTurnState( TurnState.WaitingForTell );
                GetLocalPlayer().AddAsk();
            }
        }
        else if ( turnState == TurnState.SelectingPushDirection )
        {
            Vector2 pushedPlayerPos = _grid.ConvertLocalPosToGridPos( GetPlayerForID( _pushedPlayerID ).transform.localPosition );
			Grid.Directions dir = _grid.GetDirectionFromDelta( gridPos - pushedPlayerPos );
            
            if ( dir != Grid.Directions.None )
            {
                SetState( State.WaitingForServer );
                SetTurnState( TurnState.WaitingForChoice );
                Client.GetInstance().PerformPush( _pushedPlayerID, dir );
                _pushedPlayerID = -1;
                GetLocalPlayer().AddPush();
            }
            else
            {
                tile.GetSelectionFader().Pulse();
            }
        }
    }
    
    public void MovePlayerTo( int playerID, Vector2 gridPosition )
    {
        Player player = _grid.GetPlayerAt( playerID );
        
        //if there isn't anyone else on this tile, show fog of war again
        if ( _grid.GetPlayersAtGridPosition( gridPosition ).Count <= 1 )
        {
            _grid.GetTileAt( _grid.ConvertLocalPosToGridPos( player.transform.localPosition ) ).SetFogOfWarShowing( true );
        }
        
		Vector2 localPosition = _grid.ConvertGridPosToLocalPos( gridPosition );

		ArrayList players = new ArrayList();
		players.Add( player );

		ArrayList gridPlayers = _grid.GetPlayersAtGridPosition( gridPosition );

		// No one else is on the tile.
		if( gridPlayers.Count == 0 ) 
		{
			float angle = Mathf.Atan2( localPosition.y - player.transform.localPosition.y, localPosition.x - player.transform.localPosition.x ) * Mathf.Rad2Deg;
			player.MoveToPosition( new Vector3( localPosition.x, localPosition.y, player.transform.localPosition.z ) );
			player.RotateToAngle( new Vector3( player.transform.localEulerAngles.x, -angle, player.transform.localEulerAngles.z ) );
		}
		else
		{
			// Construct an array of all players on the tile.
			foreach( Player p in gridPlayers )
			{
				if( !players.Contains( p ) )
					players.Add( p );
			}

			// Adjusts all the players positions on the tile when multiple
			// players occupy the same tile.
			AdjustPlayersTilePosition( players, localPosition );
		}
        
        _grid.GetTileAt( gridPosition ).SetFogOfWarShowing( false );
    }
	
	public void AdjustPlayersTilePosition( ArrayList players, Vector2 localPosition )
	{
		// Position the players at the corners of the tile.
		for( int i = 0; i < players.Count; i++ ) 
		{
			Player p = (Player)players[ i ];
			Vector2 cornerLocalPosition = new Vector2( localPosition.x, localPosition.y );
			if( i == 0 )
			{
				cornerLocalPosition.x -= 0.25f;
				cornerLocalPosition.y -= 0.25f;
			}
			else if( i == 1 )
			{
				cornerLocalPosition.x += 0.25f;
				cornerLocalPosition.y -= 0.25f;
			}
			else if( i == 2 )
			{
				cornerLocalPosition.x += 0.25f;
				cornerLocalPosition.y += 0.25f;
			}
			else if( i == 3 )
			{
				cornerLocalPosition.x -= 0.25f;
				cornerLocalPosition.y += 0.25f;
			}
			
			float angle = Mathf.Atan2( localPosition.y - p.transform.localPosition.y, localPosition.x - p.transform.localPosition.x ) * Mathf.Rad2Deg;
			p.MoveToPosition( new Vector3( cornerLocalPosition.x, cornerLocalPosition.y, p.transform.localPosition.z ) );
			p.RotateToAngle( new Vector3( p.transform.localEulerAngles.x, -angle, p.transform.localEulerAngles.z ) ); 
		}
	}
    
    public void ShiftTiles( Vector2 gridPosition, Grid.Directions direction )
    {
		_grid.ShiftTiles( gridPosition, direction );

		if ( state == State.WaitingForServer )
		{
			state = State.Turns;
		}
	}
	
	public void PerformShiftOnSelectedTile( Grid.Directions dir )
	{
		_grid.GetTileAt(_selectedGridPosForShift).GetSelectionFader().FadeOut();
		SetState( State.WaitingForServer );
		Client.GetInstance().PerformShift( _selectedGridPosForShift, dir );
		SetTurnState( TurnState.WaitingForChoice );
		GetLocalPlayer().AddShift();
	}
	
	public void BeginPlayerTurn( int playerId )
    {
        GetPlayerForID( _currentPlayerID ).ResetTurnTrackers();
		GetPlayerForID( _currentPlayerID ).UpdateStatusFlag( StatusFlag.StatusFlagState.Off );

        _currentPlayerID = playerId;
		if ( _localPlayerID == _currentPlayerID )
		{
			GetPlayerForID( _currentPlayerID ).UpdateStatusFlag( StatusFlag.StatusFlagState.Turn );
		}

        SetState( State.Turns );
    }

	public bool DoesPlayerShareGridPositionWithPlayer( Player player1, Player player2 )
	{
		Vector2 player1LocalPosition = player1.transform.localPosition;
		Vector2 player2LocalPosition = player2.transform.localPosition;

		Vector2 player1GridPosition = _grid.ConvertLocalPosToGridPos( player1LocalPosition );
		Vector2 player2GridPosition = _grid.ConvertLocalPosToGridPos( player2LocalPosition );

		return ( player1GridPosition.x == player2GridPosition.x ) && ( player1GridPosition.y == player2GridPosition.y );
	}
    
    public void BeginTelling( Vector2 gridPos )
    {
        SetState( State.Telling );
        _grid.GetTileAt( gridPos ).GetSelectionFader().FadeIn();
        _tellingGridPos = gridPos;
    }
    
    public void DoTell( bool tileIsGood )
    {
        Client.GetInstance().PerformTell( _tellingGridPos, tileIsGood );
        _grid.GetTileAt( _tellingGridPos ).GetSelectionFader().FadeOut();
    }
    
    public void FlagTile( Vector2 gridPos, bool tileIsGood )
    {
        Tile t = _grid.GetTileAt( gridPos );
        t.Flag( tileIsGood );
        
        if ( t.GetSelectionFader().IsShowing() )
        {
            t.GetSelectionFader().FadeOut();
        }
        
        //if we were the one who asked, clear our state now
        if ( turnState == TurnState.WaitingForTell )
        {
            SetTurnState( TurnState.WaitingForChoice );
        }
        
        if ( state == State.Telling )
        {
            state = State.Turns;
        }

		//my turn
		StatusFlag.StatusFlagState resetStatusFlagState = StatusFlag.StatusFlagState.Off;
		if ( _localPlayerID == _currentPlayerID )
		{
			resetStatusFlagState = StatusFlag.StatusFlagState.Turn;
		}
		GetPlayerForID ( _currentPlayerID ).UpdateStatusFlag ( resetStatusFlagState );
    }
    
    public void PushPlayerTo( int playerId, Vector2 gridPos )
    {
        MovePlayerTo( playerId, gridPos );
        
		if ( state == State.WaitingForServer )
		{
			state = State.Turns;
		}
	}
	
	public string GetCurrentPlayerPossessiveName()
    {
        if ( _currentPlayerID == _localPlayerID )
        {
            return "Your";
        }
        return GetCurrentPlayer().GetPlayerName() + "'s";
    }
    
    public Player GetCurrentPlayer()
    {
        return GetPlayerForID( _currentPlayerID );
    }
    
    public bool ShouldShowPlayerTeam()
    {
        return Time.time - timeStarted <= timeToShowTeam; 
    }
    
    public void ClearSelectedTiles()
    {
        Tile t = _grid.GetTileAt( _tellingGridPos );
        if ( t != null && t.GetSelectionFader().IsShowing() )
        {
            t.GetSelectionFader().FadeOut();
        }
        
        t = _grid.GetTileAt( _selectedGridPosForShift );
        if ( t != null && t.GetSelectionFader().IsShowing() )
        {
            t.GetSelectionFader().FadeOut();
        }
    }
    
    public void UpdateVisualForTile( Vector2 tilePos, bool trapped )
    {
        Tile t = _grid.GetTileAt( tilePos );
        t.SetVisualStatus( trapped ? Tile.Status.Bad : Tile.Status.Neutral );
    }

	public void UpdatePlayerStatusFlag ( int playerId, StatusFlag.StatusFlagState flagStatusState )
	{
		GetPlayerForID ( playerId ).UpdateStatusFlag ( flagStatusState );
	}
    
    public void SetTileStatusForPlayer( int playerId, Tile.Status tileStatus )
    {
        _grid.SetTileStatusForPlayer( playerId, tileStatus );
    }
}
