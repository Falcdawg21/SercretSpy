using UnityEngine;
using System.Collections;

public class Grid : MonoBehaviour {
	
	public enum Directions { None = 0, Up, Down, Left, Right, Size };
	
	public GameObject tilePrefab;
	public Vector2 tileSize;
    public float trapRatio = 1.0f;
	public int startDistanceFromGoal = 3;
	
	private Vector2 _size;
	private FlattenedTileArray _tiles;
	private ArrayList _players;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        //just a shitty brute force to make sure the fog of war is correct
/*        for( int x = 0; x < _size.x; x++ )
        {
            for( int y = 0; y < _size.y; y++ )
            {
                Tile t = _tiles.Get( x, y );

                if ( !t.IsFadingFogOfWar() )
                {
                    int playerCount = GetPlayersAtGridPosition( new Vector2( x, y ) ).Count;
                    bool showTile = playerCount >= 1;

                    t.SetFogOfWarShowing( !showTile );
                }
            }
        }*/
	}
	
	public void Initialize( Vector2 size ){
		_size = size;
		_tiles = new FlattenedTileArray( new int[] { (int)_size.x , (int)_size.y } );
		_players = new ArrayList();
		
		ConstructGrid();
	}
	
	void ConstructGrid()
	{
		Vector2 localPos = new Vector2 (0, 0);
		
		for( int x = 0; x < _size.x; x++ ) {
			for( int y = 0; y < _size.y; y++ ){
				GameObject obj = Object.Instantiate( tilePrefab ) as GameObject;
				obj.transform.parent = transform;
				
				localPos = ConvertGridPosToLocalPos( new Vector2( x, y ) );
				obj.transform.localPosition = localPos;
				
				_tiles.Set( obj.GetComponent( typeof( Tile ) ) as Tile, x, y );
			}
		}
	}

	public void AddPlayer( Player player, Vector2 gridPosition )
	{
		Vector3 localPosition = new Vector3( 0, 0, 0 );
		
		_players.Add( player );
		
		localPosition = ConvertGridPosToLocalPos( gridPosition );
		player.transform.localPosition = new Vector3( localPosition.x, localPosition.y, player.transform.localPosition.z );
	}

    public int GetPlayerCount()
    {
        return _players.Count;
    }

	public Player GetPlayerAt( int playerIndex )
	{
		Player player = null;
		player = (Player)_players[ playerIndex ];
		return player;
	}

	public ArrayList GetPlayersAtGridPosition( Vector2 gridPosition )
	{
		ArrayList players = new ArrayList();
		for( int i = 0; i < _players.Count; ++i ) 
		{
			Player player = GetPlayerAt( i );
			Vector2 playerGridPosition = ConvertLocalPosToGridPos( player.transform.localPosition );
			if( ( gridPosition.x == playerGridPosition.x ) && ( gridPosition.y == playerGridPosition.y ) )
			{
				players.Add( player );
			}
		}

		return players;
	}

	public FlattenedTileArray GetTiles()
	{
		return _tiles;
	}
	
	public ArrayList GetTilesDistanceAway( Vector3 position, float distance )
	{
		ArrayList tiles = new ArrayList();

		for ( int x = 0; x < _size.x; x++ )
		{
			for ( int y = 0; y < _size.y; y++ )
			{
				if ( Mathf.Abs ( x - position.x ) + Mathf.Abs ( y - position.y ) == distance )
				{
					tiles.Add ( _tiles.Get ( x, y ) );
				}
			}
		}

		return tiles;
	}

    public int GetWidth()
    {
        return (int) _size.x;
    }
    
    public int GetHeight()
    {
        return (int) _size.y;
    }

	public Tile GetTileAt( Vector2 location ) {
		if( !inBounds(location) ) {
			return null;
		}
		
		return _tiles.Get( (int)location.x, (int)location.y );
	}
	
	public void SetTileStatus( Vector2 location, Tile.Status status ){
		if( !inBounds(location) ) {
			return;
		}
		
		Tile tile = _tiles.Get( (int)location.x, (int)location.y );
		tile.SetStatus( status ); 
	}

	public void ShiftTiles( Vector2 gridPosition, Directions direction ) {
		
		if( !inBounds( gridPosition ) ) {
			return;
		}
		
		Vector2 currGridPosition = gridPosition;
		Vector2 prevGridPosition = new Vector2(0, 0);
		Vector2 nextGridPosition = new Vector2(0, 0);
		int nextLocationX = 0;
		int prevLocationX = 0;
		int nextLocationY = 0;
		int prevLocationY = 0;
		Tile nextTile = null;
		Tile prevTile = null;
		Tile currTile = _tiles.Get( (int)currGridPosition.x, (int)currGridPosition.y );
		ArrayList players = GetPlayersAtGridPosition( currGridPosition );

		if( direction == Directions.Up ) {
			for( int i = 0; i < _tiles.GetLength( 1 ); i++ ) {
				prevLocationY = ( (int)currGridPosition.y - 1 ) < 0 ? (int)_size.y - 1 : (int)currGridPosition.y - 1;
				
				// Move the currTile object to the previous location.
				prevGridPosition = new Vector2( currGridPosition.x, prevLocationY );
				currTile.MoveToPosition( ConvertGridPosToLocalPos( prevGridPosition ) );

				// Move the player(s) to the previous location.
				foreach( Player player in players )
				{
					player.MoveToPosition( new Vector3( ConvertGridPosToLocalPos( prevGridPosition ).x, ConvertGridPosToLocalPos( prevGridPosition ).y, player.transform.localPosition.z ) );
				}

				// Move the currTile data to the previous location.
				prevTile = _tiles.Get( (int)prevGridPosition.x, (int)prevGridPosition.y );
				_tiles.Set( currTile, (int)currGridPosition.x, (int)prevGridPosition.y );

				currGridPosition.y = prevLocationY;

				currTile = prevTile;
				players = GetPlayersAtGridPosition( currGridPosition );
			}
		} 
		else if( direction == Directions.Down ) {
			for( int i = 0; i < _tiles.GetLength( 1 ); i++ ) {
				nextLocationY = ( (int)currGridPosition.y + 1 ) >= (int)_size.y ? 0 : (int)currGridPosition.y + 1;
				
				// Move the currTile object to the next location.
				nextGridPosition = new Vector2( currGridPosition.x, nextLocationY );
				currTile.MoveToPosition( ConvertGridPosToLocalPos( nextGridPosition ) );

				// Move the player(s) to the next location.
				foreach( Player player in players )
				{

					player.MoveToPosition( new Vector3( ConvertGridPosToLocalPos( nextGridPosition ).x, ConvertGridPosToLocalPos( nextGridPosition ).y, player.transform.localPosition.z ) );
				}

				// Move the currTile data to the next location.
				nextTile = _tiles.Get( (int)currGridPosition.x, (int)nextGridPosition.y );
				_tiles.Set( currTile, (int)currGridPosition.x, (int)nextGridPosition.y );

				currGridPosition.y = nextLocationY;

				currTile = nextTile;
				players = GetPlayersAtGridPosition( currGridPosition );
			}
		}
		else if( direction == Directions.Right ) {
			for( int i = 0; i < _tiles.GetLength( 0 ); i++ ) {
				nextLocationX = ( (int)currGridPosition.x + 1 ) >= (int)_size.x ? 0 : (int)currGridPosition.x + 1;
				
				// Move the currTile object to the previous location.
				nextGridPosition = new Vector2( nextLocationX, currGridPosition.y );
				currTile.MoveToPosition( ConvertGridPosToLocalPos( nextGridPosition ) );

				// Move the player(s) to the next location.
				foreach( Player player in players )
				{
					player.MoveToPosition( new Vector3( ConvertGridPosToLocalPos( nextGridPosition ).x, ConvertGridPosToLocalPos( nextGridPosition ).y, player.transform.localPosition.z ) );
				}

				// Move the currTile data to the previous location.
				nextTile = _tiles.Get( (int)nextGridPosition.x, (int)currGridPosition.y );
				_tiles.Set( currTile, (int)nextGridPosition.x, (int)currGridPosition.y );

				currGridPosition.x = nextLocationX;

				currTile = nextTile;
				players = GetPlayersAtGridPosition( currGridPosition );
			}
		}
		else if( direction == Directions.Left ) {
			for( int i = 0; i < _tiles.GetLength( 0 ); i++ ) {
				prevLocationX = ( (int)currGridPosition.x - 1 ) < 0 ? (int)_size.x - 1 : (int)currGridPosition.x - 1;
				
				// Move the currTile object to the previous location.
				prevGridPosition = new Vector2( prevLocationX, (int)currGridPosition.y );
				currTile.MoveToPosition( ConvertGridPosToLocalPos( prevGridPosition ) );

				// Move the player(s) to the previous location.
				foreach( Player player in players )
				{
					player.MoveToPosition( new Vector3( ConvertGridPosToLocalPos( prevGridPosition ).x, ConvertGridPosToLocalPos( prevGridPosition ).y, player.transform.localPosition.z ) );
				}

				// Move the currTile data to the previous location.
				prevTile = _tiles.Get( (int)prevGridPosition.x, (int)currGridPosition.y );
				_tiles.Set( currTile, (int)prevGridPosition.x, (int)currGridPosition.y );

				currGridPosition.x = prevLocationX;

				currTile = prevTile;
				players = GetPlayersAtGridPosition( currGridPosition );
			}
		}
	}
	
	public bool inBounds( Vector2 gridPosition ) {
		bool inBounds = true;
		
		if( gridPosition.x >= _size.x ) {
			inBounds = false;		
		}
		
		if( gridPosition.y >= _size.y ) {
			inBounds = false;		
		}
		
		return inBounds;
	}
	
	public Vector2 ConvertGridPosToLocalPos( Vector2 gridPosition ) {
		if( !inBounds(gridPosition) ) {
			return new Vector2( 0, 0 );
		}
		
		Vector2 localPosition = new Vector2( 0, 0 );
		
		localPosition.x = ( gridPosition.x * tileSize.x ) + ( tileSize.x / 2.0f );
		localPosition.y = ( ( _size.y - gridPosition.y ) * tileSize.y ) - ( tileSize.y / 2.0f );
		return localPosition;
	}
	
	public Vector2 ConvertLocalPosToGridPos( Vector2 localPos ) {
		Vector2 gridPosition = new Vector2( 0, 0 );

		// Calculate the grid position even though the local position is not
		// in the center of the tile.
		Vector2 remainder = new Vector2( localPos.x % tileSize.x, localPos.y % tileSize.y );
		Vector2 factor = new Vector2( remainder.x / tileSize.x, tileSize.y - ( remainder.y / tileSize.y ) );

		gridPosition.x = ( localPos.x - tileSize.x * factor.x ) / tileSize.x;
		gridPosition.y = ( _size.y - ( localPos.y + tileSize.y * factor.y ) ) / tileSize.y;
		return gridPosition;
	}
    
    public Directions GetDirectionFromDelta( Vector2 delta )
    {
		// Added ability to detect between deltas inorder to determine direction.
		// This is for characters that might not be standing in the center of a tile.
		Vector2 absDelta = new Vector2( Mathf.Abs( delta.x ), Mathf.Abs( delta.y ) );
		if ( ( absDelta.y > absDelta.x ) && ( delta.y >= -1 && delta.y < 0 ) )
        {
            return Directions.Up;
        }
		else if ( ( absDelta.y > absDelta.x ) && ( delta.y <= 1 && delta.y > 0 ) )
        {
            return Directions.Down;
        }
		else if ( ( delta.x <= 1 && delta.x > 0 ) && ( absDelta.x > absDelta.y ) )
        {
            return Directions.Right;
        }
		else if ( ( delta.x >= -1 && delta.x < 0 ) && ( absDelta.x > absDelta.y ) )
        {
            return Directions.Left;
        }
        return Directions.None;
    }
    
    public Vector2 GetDeltaFromDirection( Directions dir )
    {
        if ( dir == Directions.Up )
        {
            return new Vector2( 0, -1 );
        }
        else if ( dir == Directions.Down )
        {
            return new Vector2( 0, 1 );
        }
        else if ( dir == Directions.Left )
        {
            return new Vector2( -1, 0 );
        }
        else if ( dir == Directions.Right )
        {
            return new Vector2( 1, 0 );
        }
        return new Vector2( 0, 0 );
    }
    
    //this function does:
    // - gives the tiles either a trap or makes them clear (later adds treasure)
    // - assigns owners to the tiles randomly, which should sync with the other clients because they share the same seed
    // - reveals all the tiles owned by this player
    public void InitializeTiles( int playerID )
    {
		// Set Goal Tile.
		Vector2 goalPosition = new Vector2 ( UnityEngine.Random.Range ( 0, (int)_size.x), UnityEngine.Random.Range ( 0, (int)_size.y ) );
		Tile goalTile = _tiles.Get( (int)goalPosition.x, (int)goalPosition.y );
		goalTile.SetStatus( Tile.Status.Good );
		goalTile.SetOwnerID( 0 );
		goalTile.SetShowsContents( true );

		ArrayList possibleSpawnTiles = GetTilesDistanceAway ( goalPosition, startDistanceFromGoal );
		ArrayList spawnTiles = new ArrayList();
		
		int[] playerOwnedCount = new int[ _players.Count ];
        
        //the starting tiles are always owned
        for ( int playerIndex = 0; playerIndex < _players.Count; playerIndex++ )
        {
            playerOwnedCount[ playerIndex ]++;
        
			// get random tile
			Tile tile = (Tile) possibleSpawnTiles [ UnityEngine.Random.Range ( 0, possibleSpawnTiles.Count ) ];
			possibleSpawnTiles.Remove ( tile );
			spawnTiles.Add ( tile );

            //set up those owners
			Player owner = _players[ playerIndex ] as Player;
            int ownerID = owner.GetPlayerID();
            tile.SetOwnerID( ownerID );
            tile.SetShowsContents( ownerID == playerID );

			// Position owner
            Vector3 localPosition = tile.transform.localPosition;;
            owner.transform.localPosition = new Vector3( localPosition.x, localPosition.y, owner.transform.localPosition.z );
        }
        
        //we'll use this to track all tiles that are allowed to have traps in them
        ArrayList trappableTiles = new ArrayList();
        
        // go through all the tiles and assign them to players, and also add them to a list for making them traps later
        for ( int x = 0; x < GetWidth(); x++ )
        {
            for ( int y = 0; y < GetHeight(); y++ )
            {
                Tile t = _tiles.Get( x, y );

				if ( t == goalTile || spawnTiles.Contains ( t ) ) continue;
                
                //fill the possible owner list with any players that have the least tiles assigned, or tied for it
                ArrayList possibleOwners = new ArrayList();
                int leastIndex = 0;
                for ( int playerIndex = 1; playerIndex < _players.Count; playerIndex++ )
                {
                    if ( playerOwnedCount[ playerIndex ] < playerOwnedCount[ leastIndex ] )
                    {
                        leastIndex = playerIndex;
                    }
                }
                
                bool tileHasPlayer = false;
                for ( int playerIndex = 0; playerIndex < _players.Count; playerIndex++ )
                {
                    if ( playerOwnedCount[ playerIndex ] <= playerOwnedCount[ leastIndex ] )
                    {
                        possibleOwners.Add( playerIndex );
                        
                        if ( !tileHasPlayer )
                        {
                            Vector2 playerGridPos = ConvertLocalPosToGridPos( ( _players[ playerIndex ] as Player ).transform.localPosition );
                            Vector2 tileGridPos   = ConvertLocalPosToGridPos( t.transform.localPosition );
                            if ( playerGridPos.x == tileGridPos.x && playerGridPos.y == tileGridPos.y )
                            {
                                tileHasPlayer = true;
                            }
                        }
                    }
                }
                
                if ( !tileHasPlayer && t.GetStatus() != Tile.Status.Good && !spawnTiles.Contains ( t ) )
                {
                    //only add tiles that are not where the player is standing
                    trappableTiles.Add( t );
                }
                
                //now that we have that populated, choose a random index from it
                int ownerIndex = (int) possibleOwners[ Random.Range( 0, possibleOwners.Count ) ];
                playerOwnedCount[ ownerIndex ]++;
            
                //finally, we can tell that tile who owns it, and we can also show it if we own it
                int ownerID = ( _players[ ownerIndex ] as Player ).GetPlayerID();
                t.SetOwnerID( ownerID );
                t.SetShowsContents( ownerID == playerID );
            }
        }
        
        //finally, make some of the tiles traps
        int trapCount = (int) ( GetWidth() * GetHeight() * ( trapRatio / 2.0f ) );
        for ( int trapIndex = 0; trapIndex < trapCount; trapIndex++ )
        {
            int randomTrapIndex = Random.Range( 0, trappableTiles.Count );
            Tile t = trappableTiles[ randomTrapIndex ] as Tile;
            t.SetStatus( Tile.Status.Bad );
            trappableTiles.RemoveAt( randomTrapIndex );
        }
    }
    
    public bool GridPosLiesWithinShift( Vector2 pos, Vector2 shiftPos, Directions direction )
    {
        Vector2 delta = GetDeltaFromDirection( direction );
		Vector2 absDelta = new Vector2( Mathf.Abs( delta.x ), Mathf.Abs( delta.y ) );
		return pos.x >= shiftPos.x - absDelta.x * GetWidth () && pos.x <= shiftPos.x + absDelta.x * GetWidth () &&
			   pos.y >= shiftPos.y - absDelta.y * GetHeight() && pos.y <= shiftPos.y + absDelta.y * GetHeight();
    }
    
    public bool PlayerLiesWithinShift( Player player, Vector2 shiftPos, Directions direction )
    {
        Vector2 pos = ConvertLocalPosToGridPos( player.transform.localPosition );
        return GridPosLiesWithinShift( pos, shiftPos, direction );
    }
    
    public bool AnyPlayerLiesWithinShift( Vector2 shiftPos, Directions direction )
    {
        for ( int playerIndex = 0; playerIndex < _players.Count; playerIndex++ )
        {
            Player p = _players[ playerIndex ] as Player;
            if ( PlayerLiesWithinShift( p, shiftPos, direction ) )
            {
                return true;
            }
        }
        
        return false;
    }
    
    public void RedrawPlayerTiles( int playerID )
    {
        for ( int x = 0; x < GetWidth(); x++ )
        {
            for ( int y = 0; y < GetHeight(); y++ )
            {
                Tile t = _tiles.Get( x, y );
                
                if ( t.GetOwnerID() == playerID )
                {
                    t.SetStatus( t.GetStatus() );
                }
            }
        }
    }
    
    public void SetTileStatusForPlayer( int playerId, Tile.Status tileStatus )
    {
        for ( int x = 0; x < GetWidth(); x++ )
        {
            for ( int y = 0; y < GetHeight(); y++ )
            {
                Tile t = _tiles.Get( x, y );
                
                if ( t.GetOwnerID() == playerId )
                {
                    t.SetStatus( tileStatus );
                }
            }
        }
    }
}
