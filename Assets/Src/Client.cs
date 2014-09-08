using UnityEngine;
using System.Collections;

public class Client : MonoBehaviour
{
    protected string connectedIp;
    protected NetworkPlayerData playerData;
    protected bool isConnected;
    protected string playerName;
    
    public void Start()
    {
        isConnected = false;
    }
    
    public bool IsConnected()
    {
        return isConnected;
    }
    
    public void ConnectToServer( string ip )
    {
        isConnected = true;
        connectedIp = ip;
        Network.Connect( ip, Server.PORT );
        LobbyGUI.ShowOutput( "Connecting to server " + connectedIp + "..." );
    }
    
    public void DisconnectFromServer()
    {
        LobbyGUI.ShowOutput( "Disconnecting..." );
        Network.Disconnect();
    }
    
    public void OnConnectedToServer()
    {
        if ( !Network.isServer )
        {
            LobbyGUI.ShowOutput( "Successfully connected to " + connectedIp + ". Waiting for host to start." );
            Client.GetInstance().SetName( LobbyGUI.GetInstance().GetPlayerName() );
        }
    }
    
    public void OnFailedToConnect( NetworkConnectionError error )
    {
        if ( !Network.isServer )
        {
            LobbyGUI.ShowOutput( "Failed to connect! Reason: " + error );
        }
        
        isConnected = false;
    }
    
    public void OnDisconnectedToServer( NetworkDisconnection info )
    {
        if ( !Network.isServer )
        {
            LobbyGUI.ShowOutput( "Disconnected from server. Reason: " + info );
        }
        
        isConnected = false;
        
        if ( Game.GetInstance() != null )
        {
            Object.Destroy( Server.GetInstance().gameObject );
            Object.Destroy( gameObject );
            Application.LoadLevel( "Lobby" );
        }
    }
    
    public void SetPlayerData( NetworkPlayerData setPlayerData )
    {
        playerData = setPlayerData;
    }
    
    public NetworkPlayerData GetPlayerData()
    {
        return playerData;
    }
    
    public void SetName( string name )
    {
        playerName = name;
    }
    
    ///////////////////////Send functions, from client to server////////////////
    
    public void TellServerIAmReady()
    {
        if ( Network.isServer )
        {
            Server.GetInstance().ClientIsReady( CreateMyInfo() );
        }
        else
        {
            networkView.RPC( "ClientIsReady", RPCMode.Server );
        }
    }
    
    //send the player's name to the server
    public void SendName( string name )
    {
        if ( name == null || name.Length <= 0 )
        {
            name = "Sparticus";
        }
        
        if ( Network.isServer )
        {
            Server.GetInstance().ClientSentName( name, CreateMyInfo() );
        }
        else
        {
            networkView.RPC( "ClientSentName", RPCMode.Server, name );
        }
    }
    
    //do the action to ask for a space to be revealed
    public void PerformAsk( Vector2 gridLoc )
    {
        if ( Network.isServer )
        {
            Server.GetInstance().ClientPerformedAsk( (int) gridLoc.x, (int)gridLoc.y, CreateMyInfo() );
        }
        else
        {
            networkView.RPC( "ClientPerformedAsk", RPCMode.Server, (int) gridLoc.x, (int) gridLoc.y );
        }
    }
    
    //in response to an ask, reveal whether a tile is good or not
    public void PerformTell( Vector2 gridLoc, bool spaceIsGood )
    {
        if ( Network.isServer )
        {
            Server.GetInstance().ClientPerformedTell( (int) gridLoc.x, (int) gridLoc.y, spaceIsGood ? 1 : 0, CreateMyInfo() );
        }
        else
        {
            networkView.RPC( "ClientPerformedTell", RPCMode.Server, (int) gridLoc.x, (int) gridLoc.y, spaceIsGood ? 1 : 0 );
        }
    }
    
    //do the action to move your player in a given direction (this always refers to your player)
    public void PerformMove( Grid.Directions direction )
    {
        if ( Network.isServer )
        {
            Server.GetInstance().ClientPerformedMove( (int) direction, CreateMyInfo() );
        }
        else
        {
            networkView.RPC( "ClientPerformedMove", RPCMode.Server, (int) direction );
        }
    }
    
    //do the action to shift the maze
    public void PerformShift( Vector2 gridLoc, Grid.Directions direction )
    {
        if ( Network.isServer )
        {
            Server.GetInstance().ClientPerformedShift( (int) gridLoc.x, (int) gridLoc.y, (int) direction, CreateMyInfo() );
        }
        else
        {
            networkView.RPC( "ClientPerformedShift", RPCMode.Server, (int) gridLoc.x, (int) gridLoc.y, (int) direction );
        }
    }
    
    //do the action to push one player in a given direction
    public void PerformPush( int targetPlayerId, Grid.Directions direction )
    {
        if ( Network.isServer )
        {
            Server.GetInstance().ClientPerformedPush( targetPlayerId, (int) direction, CreateMyInfo() );
        }
        else
        {
            networkView.RPC( "ClientPerformedPush", RPCMode.Server, targetPlayerId, (int) direction );
        }
    }

	public void TellServerPlayerWon( int playerID )
	{
		if ( Network.isServer )
		{
			Server.GetInstance().ClientNotifyPlayerWon( playerID, CreateMyInfo() );
		}
		else
		{
			networkView.RPC( "ClientNotifyPlayerWon", RPCMode.Server, (int)playerID );
		}
	}
	
	///////////////////////Receive functions, from server to client////////////////
    
    [RPC]
    //after we connect, the server will tell us to create all the players, and where they go
    public void CreatePlayer( int playerId, int x, int y )
    {
        Player p = Game.GetInstance().CreatePlayer( playerId, new Vector2( x, y ) );
        
        if ( Network.isServer )
        {
            Server.GetInstance().CreatedNewPlayer( p, playerId );
        }
    }
    
    [RPC]
    //after we created all the players, the server will tell us our player ID, then we'll choose maze tiles
    public void SetLocalPlayerId( int playerId )
    {
        playerData = new NetworkPlayerData( Network.player, playerId );
        playerData.SetName( playerName );
        Game.GetInstance().SetLocalPlayerID( playerId );
        playerData.SetPlayer( Game.GetInstance().GetPlayerForID( playerId ) );
        
        //now come back and tell the server our name
        SendName( playerName );
    }
    
    [RPC]
    //after we created the maze, the server will tell us what team we're on
    public void SetLocalPlayerInfo( int isSpy )
    {
        Game.GetInstance().SetLocalPlayerInfo( isSpy == 1 );
    }
    
    [RPC]
    //begins the game, and sets the random number seed
    public void GameBegin( int seed )
    {
        Random.seed = seed;
        
        Application.LoadLevel( "MainScene" );
    }
    
    [RPC]
    //notify the game result, if true then this player won
    public void GameFinish( bool won )
    {
        
    }
    
    [RPC]
    //update a player by moving them to this location
    public void MovePlayerTo( int playerId, int targetX, int targetY )
    {
        Game.GetInstance().MovePlayerTo( playerId, new Vector2( targetX, targetY ) );
    }
    
    [RPC]
    //if one player did an ask, the server decides who should receive it, and then sends them this call
    public void GivePlayerAsk( int targetX, int targetY )
    {
        Game.GetInstance().BeginTelling( new Vector2( targetX, targetY ) );
    }

	[RPC]
	public void UpdatePlayerStatusFlag ( int playerId, int flagStatusState )
	{
		Game.GetInstance ().UpdatePlayerStatusFlag ( playerId, (StatusFlag.StatusFlagState) flagStatusState );
	}
    
    [RPC]
    //update the grid by shifting from the location in the direction (typecast to the Grid.Directions enum)
    public void ShiftGrid( int originX, int originY, int direction )
    {
        Game.GetInstance().ShiftTiles( new Vector2( originX, originY ), (Grid.Directions) direction );
    }
    
    [RPC]
    //the passed player begins their turn
    public void PlayerBeginTurn( int playerId )
    {
        Game.GetInstance().BeginPlayerTurn( playerId );
    }
    
    [RPC]
    //set a given player's name
    public void SetPlayerName( int playerId, string name )
    {        
        if ( playerData != null && playerId == playerData.GetId() )
        {
            playerData.SetName( name );
        }
        
        Player p = Game.GetInstance().GetPlayerForID( playerId );
        p.SetPlayerName( name );
    }
    
    [RPC]
    public void ShowTellResult( int x, int y, int spaceIsGood )
    {
        Game.GetInstance().FlagTile( new Vector2( x, y ), spaceIsGood == 1 );
    }
    
    [RPC]
    public void PushPlayerTo( int playerId, int x, int y )
    {
        Game.GetInstance().PushPlayerTo( playerId, new Vector2( x, y ) );
    }
    
    [RPC]
    //this gets called if the server didn't like what you sent it
    public void ServerRejectedAction()
    {
        Game.GetInstance().ResetStates();
    }

	[RPC]
	public void PlayerWon( int won )
	{
		Game.GetInstance().PlayerWon( won == 1 ); 
	}
    
    [RPC]
    public void ShowUpdatedTileVisual( int tileX, int tileY, int isTrapped )
    {
        Game.GetInstance().UpdateVisualForTile( new Vector2( tileX, tileY ), isTrapped == 1 );
    }
    
    [RPC]
    public void SetTileStatusForPlayer( int playerId, int tileStatus )
    {
        Game.GetInstance().SetTileStatusForPlayer( playerId, (Tile.Status) tileStatus );
    }
    
    protected NetworkMessageInfo CreateMyInfo()
    {
        //I want to assign values to this like the sender being me, but no idea how it won't let me
        //apparently the sender defaults to 0 which I THINK is always me, so this SHOULD be okay?
        return new NetworkMessageInfo();
    }
    
    ///////////////////////Private/////////////////
    
    public static Client instance;
    public static Client GetInstance()
    {
        return instance;
    }
    public void Awake()
    {
        instance = this;
    }
}