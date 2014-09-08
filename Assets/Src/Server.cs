using UnityEngine;
using System.Collections;

public class Server : MonoBehaviour
{
    public const int MAX_CONNECTIONS = 32;
    public const int PORT = 13375;
    public const bool USE_NAT = false;
    public const int PLAYER_COUNT_REQUIRED = 4;
    
    protected ArrayList connectedPlayers;
    protected ArrayList playerNames;
    protected ArrayList readyPlayers;
    
    public enum ConnectionPhase
    {
        WaitingForPlayers = 0,
        ReadyToStart,
        WaitingForReady,
        Playing
    };
    protected ConnectionPhase connectionPhase;
    
    public void Start()
    {
        connectionPhase = ConnectionPhase.WaitingForPlayers;
    }
    
    public void StartServer()
    {
        LobbyGUI.ShowOutput( "Starting server..." );
        Network.InitializeServer( MAX_CONNECTIONS, PORT, USE_NAT );
    }
    
    public void StopServer()
    {
        LobbyGUI.ShowOutput( "Shutting server down..." );
        Network.Disconnect();
    }
    
    public void OnServerInitialized()
    {
        if ( LobbyGUI.GetInstance() != null )
        {
            LobbyGUI.GetInstance().ShowIPAddress( Network.player.ipAddress );
        }
        
        //I'm already connected to myself as a player, so add myself
        NetworkPlayerData playerData = new NetworkPlayerData( Network.player, connectedPlayers.Count );
        connectedPlayers.Add( playerData );
        Client.GetInstance().SetPlayerData( playerData );
        
        if ( LobbyGUI.GetInstance() != null )
        {
            Client.GetInstance().SetName( LobbyGUI.GetInstance().GetPlayerName() );
        }
        
        RefreshWaitingString();
    }
    
    public void OnPlayerConnected( NetworkPlayer player )
    {
        if ( !connectedPlayers.Contains( player ) )
        {
            NetworkPlayerData playerData = new NetworkPlayerData( player, connectedPlayers.Count );
            connectedPlayers.Add( playerData );
            
            RefreshWaitingString();
            
            if ( connectedPlayers.Count >= PLAYER_COUNT_REQUIRED )
            {
                connectionPhase = ConnectionPhase.ReadyToStart;
            }
        }
    }
    
    public void OnPlayerDisconnected( NetworkPlayer player )
    {
        if ( connectedPlayers.Contains( player ) )
        {
            connectedPlayers.Remove( player );
            
            RefreshWaitingString();
            
            if ( connectedPlayers.Count < PLAYER_COUNT_REQUIRED )
            {
                connectionPhase = ConnectionPhase.WaitingForPlayers;
            }
        }
    }
    
    public void OnDisconnectedFromServer( NetworkDisconnection info )
    {
        connectedPlayers.Clear();
        LobbyGUI.ShowOutput( "The server has stopped. Reason: " + info );
    }
    
    public int GetConnectedPlayerCount()
    {
        return connectedPlayers.Count;
    }
    
    public NetworkPlayerData GetConnectedPlayerAt( int index )
    {
        return connectedPlayers[ index ] as NetworkPlayerData;
    }
    
    ///////////////////////Receive functions, from client to server (validation happens here)////////////////
    
    [RPC]
    //the client sends this when the main scene is fully loaded and ready to go
    public void ClientIsReady( NetworkMessageInfo info )
    {
        NetworkPlayerData data = GetNetworkPlayerData( info.sender );
        
        if ( !readyPlayers.Contains( data ) )
        {
            readyPlayers.Add( data );
        }
        
        //if all the players have loaded, then begin creating all the player objects
        if ( readyPlayers.Count >= connectedPlayers.Count )
        {
            AllPlayersReady();
        }
    }
    
    [RPC]
    public void ClientSentName( string name, NetworkMessageInfo info )
    {
        TellClientsPlayerName( GetNetworkPlayerId( info.sender ), name );
    }
    
    [RPC]
    public void ClientPerformedAsk( int x, int y, NetworkMessageInfo info )
    {
        if ( !IsNetworkPlayerTurn( info.sender ) )
        {
            Debug.LogWarning( "Refusing to perform an ask, because it's not this player's turn: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }
        
        if ( !Game.GetInstance().CanPerformAsk( GetNetworkPlayerId( info.sender ) ) )
        {
            Debug.LogWarning( "Refusing to perform an ask, because the passed player cannot do any more asks: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }
        
        if ( !Game.GetInstance().CanAskAboutTile( GetNetworkPlayerId( info.sender ), new Vector2( x, y ) ) )
        {
            Debug.LogWarning( "Refusing to perform an ask, because the tile chosen is not valid to ask about: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }
        
        //the ask will be performed, so we need to track how many that user has done.
        //this is kinda weak since we also track it locally, meaning that we DON'T want
        //to increment this if it's ourself since the client already did that
        if ( info.sender != Network.player )
        {
            GetPlayerFromNetworkPlayer( info.sender ).AddAsk();
        }
        
        //find the correct person to ask, and ask them only
        int ownerId = Game.GetInstance().GetGrid().GetTileAt( new Vector2( x, y ) ).GetOwnerID();
        NetworkPlayerData data = GetNetworkPlayerData( ownerId );
        SendAskToClient( data.GetNetworkPlayer(), x, y );
		SendVisualAskToAllClients( GetNetworkPlayerId( info.sender ) );
    }
    
    [RPC]
    //in response to an ask, reveal whether a tile is good or not
    public void ClientPerformedTell( int x, int y, int spaceIsGood, NetworkMessageInfo info )
    {
        //double check that the person performing the tell actually owns this tile
        int ownerId = Game.GetInstance().GetGrid().GetTileAt( new Vector2( x, y ) ).GetOwnerID();
        if ( ownerId != GetNetworkPlayerId( info.sender ) )
        {
            Debug.LogWarning( "Refusing to perform a tell, because this player is not the owner of the tile: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }
        
        SendTellToClients( x, y, spaceIsGood );
        
        if ( Game.GetInstance().askUsesTurn )
        {
            TellClientsToGoToNextTurn();
        }
    }
    
    [RPC]
    //do the action to move your player in a given direction (this always refers to your player)
    public void ClientPerformedMove( int direction, NetworkMessageInfo info )
    {
        if ( !IsNetworkPlayerTurn( info.sender ) )
        {
            Debug.LogWarning( "Refusing to perform a move, because it's not this player's turn: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }
        
        NetworkPlayerData data = GetNetworkPlayerData( info.sender );
        Vector2 resultPos = GetResultOfDirection( data.GetPlayer(), direction );
        
        // Evaluates the players move to determine what happens to the player after he/she moves.
		Game.GetInstance().EvaluatePlayerMove( data.GetId(), resultPos );
        
        SendMoveToClients( data.GetId(), (int) resultPos.x, (int) resultPos.y );
        TellClientsToGoToNextTurn();
    }
    
    [RPC]
    //do the action to shift the maze
    public void ClientPerformedShift( int x, int y, int direction, NetworkMessageInfo info )
    {
        if ( !IsNetworkPlayerTurn( info.sender ) )
        {
            Debug.LogWarning( "Refusing to perform a shift, because it's not this player's turn: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }

		if ( !Game.GetInstance().CanPerformShift( GetNetworkPlayerId( info.sender ) ) )
		{
			Debug.LogWarning( "Refusing to perform an shift, because the passed player cannot do any more shifts: " + info.sender );
			TellClientItsActionWasRejected( info.sender );
			return;
		}
		
		if ( !Game.GetInstance().IsValidShiftTile( GetNetworkPlayerId( info.sender ), new Vector2( x, y ) ) )
        {
            Debug.LogWarning( "Refusing to perform a shift, because the tile chosen to shift is not valid: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }
        
        if ( !Game.GetInstance().IsValidShiftDirection( GetNetworkPlayerId( info.sender ), new Vector2( x, y ), (Grid.Directions)direction ) )
        {
            Debug.LogWarning( "Refusing to perform a shift, because the direction chosen to shift is not valid: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }

		if ( info.sender != Network.player )
		{
			GetPlayerFromNetworkPlayer( info.sender ).AddShift();
		}
		
		SendShiftToClients( x, y, direction );

		if (Game.GetInstance ().shiftUsesTurn) 
		{
			TellClientsToGoToNextTurn ();
		}
    }
    
    [RPC]
    //do the action to push one player in a given direction
    public void ClientPerformedPush( int targetPlayerId, int direction, NetworkMessageInfo info )
    {
        if ( !IsNetworkPlayerTurn( info.sender ) )
        {
            Debug.LogWarning( "Refusing to perform a push, because it's not this player's turn: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }
        
        if ( !Game.GetInstance().CanPerformPush( GetNetworkPlayerId( info.sender ) ) )
        {
            Debug.LogWarning( "Refusing to perform a push, because the pusher has no more pushes: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }
        
        Player pusher = GetPlayerFromNetworkPlayer( info.sender );
        Player target = GetPlayerFromPlayerId( targetPlayerId );
        if ( !Game.GetInstance().DoesPlayerShareGridPositionWithPlayer( pusher, target ) )
        {
            Debug.LogWarning( "Refusing to perform a push, because the pusher and the target do not share a space: " + info.sender );
            TellClientItsActionWasRejected( info.sender );
            return;
        }
        
        //the push will be performed, so we need to track how many that user has done.
        //this is kinda weak since we also track it locally, meaning that we DON'T want
        //to increment this if it's ourself since the client already did that
        if ( info.sender != Network.player )
        {
            GetPlayerFromNetworkPlayer( info.sender ).AddPush();
        }
        
        Vector2 resultPos = GetResultOfDirection( target, direction );
        
        //is this a bad space? if so, that player needs to lose their next turn
        Game.GetInstance().DoMissedTurnCheck( target.GetPlayerID(), resultPos );
        
        SendPushToClients( targetPlayerId, (int) resultPos.x, (int) resultPos.y );
        
        if ( Game.GetInstance().pushUsesTurn )
        {
            TellClientsToGoToNextTurn();
        }
    }
    
    //this is purposefully not an RPC, the client only ever calls this if the client is the server, and calls it directly
    public void CreatedNewPlayer( Player p, int playerId )
    {
        NetworkPlayerData data = GetNetworkPlayerData( playerId );
        data.SetPlayer( p );
    }

	public void ClientNotifyPlayerWon( int playeID, NetworkMessageInfo info )
	{
		TellClientsPlayerWon( playeID );
	}
	
	///////////////////////Send functions, from server to client////////////////
        
    //tells the server to tell all the clients to begin the game
    public void TellClientsToBeginGame()
    {
        networkView.RPC( "GameBegin", RPCMode.All, Random.Range( 0, 999999999 ) );
    }
    
    public void TellClientsToCreateNewPlayer( int playerId, int x, int y )
    {
        networkView.RPC( "CreatePlayer", RPCMode.All, playerId, x, y );
    }
    
    public void TellClientItsPlayerId( NetworkPlayer player, int playerId )
    {
        if ( player == Network.player )
        {
            Client.GetInstance().SetLocalPlayerId( playerId );
        }
        else
        {
            networkView.RPC( "SetLocalPlayerId", player, playerId );
        }
    }
    
    public void TellClientItsPlayerInfo( NetworkPlayer player, int isSpy )
    {
        if ( player == Network.player )
        {
            Client.GetInstance().SetLocalPlayerInfo( isSpy );
        }
        else
        {
            networkView.RPC( "SetLocalPlayerInfo", player, isSpy );
        }
    }
    
    //send the player's name to the server
    public void TellClientsPlayerName( int playerId, string name )
    {
        networkView.RPC( "SetPlayerName", RPCMode.All, playerId, name );
    }
    
    //do the action to ask for a space to be revealed
    public void SendAskToClient( NetworkPlayer player, int x, int y )
    {        
        if ( player == Network.player )
        {
            Client.GetInstance().GivePlayerAsk( x, y );
        }
        else
        {
            networkView.RPC( "GivePlayerAsk", player, x, y );
        }
    }

	public void SendVisualAskToAllClients( int playerId )
	{
		networkView.RPC( "UpdatePlayerStatusFlag", RPCMode.All, playerId, (int) StatusFlag.StatusFlagState.Ask );
	}

	public void SendUpdatePlayerStatusFlag( int playerId, StatusFlag.StatusFlagState statusFlagState )
	{
		networkView.RPC( "UpdatePlayerStatusFlag", RPCMode.All, playerId, (int) statusFlagState );
	}
    
    //in response to an ask, reveal whether a tile is good or not
    public void SendTellToClients( int x, int y, int spaceIsGood )
    {
        networkView.RPC( "ShowTellResult", RPCMode.All, x, y, spaceIsGood );
    }
    
    //do the action to move your player in a given direction (this always refers to your player)
    public void SendMoveToClients( int playerId, int targetX, int targetY )
    {
        networkView.RPC( "MovePlayerTo", RPCMode.All, playerId, targetX, targetY );
       // GoToNextTurn();
    }
    
    //do the action to shift the maze
    public void SendShiftToClients( int x, int y, int direction  )
    {
        networkView.RPC( "ShiftGrid", RPCMode.All, x, y, direction );
        //GoToNextTurn();
    }
    
    //do the action to push one player in a given direction
    public void SendPushToClients( int targetPlayerId, int x, int y )
    {
        networkView.RPC( "PushPlayerTo", RPCMode.All, targetPlayerId, x, y );
    }
    
    public void TellClientsToGoToNextTurn()
    {
        //skip turns as necessary
        int nextPlayerId = ( Game.GetInstance().GetCurrentPlayerID() + 1 ) % Game.GetInstance().GetPlayerCount();
        while ( Game.GetInstance().WillPlayerMissTurn( nextPlayerId ) )
        {
            Game.GetInstance().PlayerMissedTurn( nextPlayerId );
            nextPlayerId = ( nextPlayerId + 1 ) % Game.GetInstance().GetPlayerCount();
        }
        
        networkView.RPC( "PlayerBeginTurn", RPCMode.All, nextPlayerId );
    }
    
    public void TellClientItsActionWasRejected( NetworkPlayer player )
    {
        if ( player == Network.player )
        {
            Client.GetInstance().ServerRejectedAction();
        }
        else
        {
            networkView.RPC( "ServerRejectedAction", player );
        }
    }

	public void TellClientsPlayerWon( int playerID )
	{
		// Find and store the winning player.
		Player winningPlayer = GetPlayerFromPlayerId( playerID );

		// Loop through the players and determine who else won or lost.
		for( int playerIndex = 0; playerIndex < connectedPlayers.Count; playerIndex++ )
		{
			int won = 0;
			NetworkPlayerData playerData = connectedPlayers[ playerIndex ] as NetworkPlayerData;
			NetworkPlayer networkPlayer = playerData.GetNetworkPlayer();
			Player player = playerData.GetPlayer();

			// I am the winning player.
			if( winningPlayer == player )
			{
				won = 1;
			}

			// A spy did not win and I am not a spy either, so I win!
			if( !player.IsSpy() && !winningPlayer.IsSpy() )
			{
				won = 1;
			}

			if( player == GetPlayerFromNetworkPlayer( Network.player ) )
			{
				Client.GetInstance().PlayerWon( won );
			}
			else
			{
				networkView.RPC( "PlayerWon", networkPlayer, won );
			}
		}
	}
    
    public void TellNonSpyClientsToUpdateTileVisual( Vector2 pos, bool isTrapped )
    {
        TellNonSpyClientsToUpdateTileVisual( (int) pos.x, (int) pos.y, isTrapped ? 1 : 0 );
    }
    
    public void TellNonSpyClientsToUpdateTileVisual( int x, int y, int isTrapped )
    {
        for ( int playerIndex = 0; playerIndex < connectedPlayers.Count; playerIndex++ )
        {
            NetworkPlayerData data = connectedPlayers[ playerIndex ] as NetworkPlayerData;
            if ( !data.GetPlayer().IsSpy() )
            {
                if ( data.GetNetworkPlayer() == Network.player )
                {
                    Client.GetInstance().ShowUpdatedTileVisual( x, y, isTrapped );
                }
                else
                {
                    networkView.RPC( "ShowUpdatedTileVisual", data.GetNetworkPlayer(), x, y, isTrapped );
                }
            }
        }
    }
    
    public void TellAllPlayersToSetTileStatusForPlayer( int playerId, Tile.Status tileStatus )
    {
        networkView.RPC( "SetTileStatusForPlayer", RPCMode.All, playerId, (int) tileStatus );
    }
	
	///////////////////////Private/////////////////
	
	protected void AllPlayersReady()
	{
		//create everyone
		for ( int playerIndex = 0; playerIndex < connectedPlayers.Count; playerIndex++ )
        {
            NetworkPlayerData playerData = connectedPlayers[ playerIndex ] as NetworkPlayerData;
            
            int createX = 0;
            int createY = 0;
            
            if ( playerIndex == 1 || playerIndex == 2 )
            {
                createX = Game.GetInstance().GetGrid().GetWidth() - 1;
            }
            if ( playerIndex == 2 || playerIndex == 3 )
            {
                createY = Game.GetInstance().GetGrid().GetHeight() - 1;
            }
            
            TellClientsToCreateNewPlayer( playerData.GetId(), createX, createY );
        }
        
        //tell everyone who their player IDs - this will also have them all generate their grids
        for ( int playerIndex = 0; playerIndex < connectedPlayers.Count; playerIndex++ )
        {
            NetworkPlayerData playerData = connectedPlayers[ playerIndex ] as NetworkPlayerData;
            
            TellClientItsPlayerId( playerData.GetNetworkPlayer(), playerData.GetId() );
        }
        
        //now we choose the spy. This puts the server's random seed out of sync with the clients, so it must happen last
        int spyIndex = Random.Range( 0, connectedPlayers.Count );
        int spyId = ( connectedPlayers[ spyIndex ] as NetworkPlayerData ).GetId();
        for ( int playerIndex = 0; playerIndex < connectedPlayers.Count; playerIndex++ )
        {
            NetworkPlayerData playerData = connectedPlayers[ playerIndex ] as NetworkPlayerData;
			bool isSpy = playerData.GetId() == spyId;
			TellClientItsPlayerInfo( playerData.GetNetworkPlayer(), isSpy ? 1 : 0 );
			playerData.GetPlayer().SetIsSpy( isSpy );
        }

        //now that we have the spy, set all their tiles to bad ones
        TellAllPlayersToSetTileStatusForPlayer( spyId, Tile.Status.Bad );

		// Clearing out the ready players list.
		readyPlayers = new ArrayList();

        //all ready to go!
        connectionPhase = ConnectionPhase.Playing;
        
        networkView.RPC( "PlayerBeginTurn", RPCMode.All, ( connectedPlayers[ 0 ] as NetworkPlayerData ).GetId() );
    }
    
    protected void RefreshWaitingString()
    {
        string message = "";
        if ( connectedPlayers.Count >= PLAYER_COUNT_REQUIRED )
        {
            message = "Ready to start!";
        }
        else
        {
            message = "Server running. Need more players to start. (" + connectedPlayers.Count + "/" + PLAYER_COUNT_REQUIRED + ")";
        }
        
        LobbyGUI.ShowOutput( message );
    }
    
    protected int GetNetworkPlayerId( NetworkPlayer p )
    {
        NetworkPlayerData d = GetNetworkPlayerData( p );
        if ( d != null )
        {
            return d.GetId();
        }
        return -1;
    }
    
    protected NetworkPlayerData GetNetworkPlayerData( NetworkPlayer p )
    {
        for ( int playerIndex = 0; playerIndex < connectedPlayers.Count; playerIndex++ )
        {
            NetworkPlayerData d = connectedPlayers[ playerIndex ] as NetworkPlayerData;
            
            if ( d.GetNetworkPlayer() == p )
            {
                return d;
            }
        }
        
        return null;
    }
    
    protected NetworkPlayerData GetNetworkPlayerData( int playerId )
    {
        for ( int playerIndex = 0; playerIndex < connectedPlayers.Count; playerIndex++ )
        {
            NetworkPlayerData d = connectedPlayers[ playerIndex ] as NetworkPlayerData;
            
            if ( d.GetId() == playerId )
            {
                return d;
            }
        }
        
        return null;
    }
    
    protected Player GetPlayerFromNetworkPlayer( NetworkPlayer p )
    {
        NetworkPlayerData d = GetNetworkPlayerData( p );
        
        if ( d != null )
        {
            return d.GetPlayer();
        }
        
        return null;
    }
    
    protected Player GetPlayerFromPlayerId( int playerId )
    {
        NetworkPlayerData d = GetNetworkPlayerData( playerId );
        
        if ( d != null )
        {
            return d.GetPlayer();
        }
        
        return null;
    }
    
    protected bool IsNetworkPlayerTurn( NetworkPlayer networkPlayer )
    {
        NetworkPlayerData d = GetNetworkPlayerData( networkPlayer );
        
        if ( d != null )
        {
            return Game.GetInstance().GetCurrentPlayerID() == d.GetId();
        }
        
        return false;
    }
    
    protected Vector2 GetResultOfDirection( Player p, int direction )
    {
        Vector2 playerPos = Game.GetInstance().GetGrid().ConvertLocalPosToGridPos( p.transform.localPosition );
        Vector2 delta = Game.GetInstance().GetGrid().GetDeltaFromDirection( (Grid.Directions) direction );
        return new Vector2( playerPos.x + delta.x, playerPos.y + delta.y );
    }
    
    public static Server instance;
    public static Server GetInstance()
    {
        return instance;
    }
    public void Awake()
    {
        instance = this;
        connectedPlayers = new ArrayList();
        readyPlayers = new ArrayList();
        Object.DontDestroyOnLoad( gameObject );
    }
}