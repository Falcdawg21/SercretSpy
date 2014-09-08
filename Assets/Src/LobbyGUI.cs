using UnityEngine;
using System.Collections;

public class LobbyGUI : MonoBehaviour
{
    public GUISkin guiSkin;
    
    public float screenBuffer = 5.0f;
    public float buttonHeight = 88.0f;
    public float buttonWidth = 120.0f;
    public float bannerWidth = 480.0f;
    public float smallBannerWidth = 360.0f;
    public float designWidth = 470.0f;
    
    protected bool isHosting;
    protected bool isJoining;
    protected string joinedIp;
    protected string status;
    protected string serverIP;
    protected string playerName;
    
    protected float latestScale;
    
    public void Start()
    {
        isHosting = false;
        isJoining = false;
        status = "";
        serverIP = Network.player.ipAddress;
        joinedIp = PlayerPrefs.GetString( "LastJoinedIP", "127.0.0.1" );
        playerName = PlayerPrefs.GetString( "PlayerName", "Secret Agent Man" );
    }
    
    public void ShowIPAddress( string ip )
    {
        serverIP = ip;
    }
    
    public void ShowStatusMessage( string message )
    {
        status = message;
    }
    
    public void OnGUI()
    {
        GUI.skin = guiSkin;
        latestScale = Screen.width / designWidth;
        GUI.matrix = Matrix4x4.Scale( new Vector3( latestScale, latestScale, 1.0f ) );
        
        //title label
        GUI.Label( new Rect( screenBuffer, screenBuffer, 2.0f * buttonWidth, buttonHeight ), "SECRET SPY" );
        playerName = GUI.TextField( new Rect( screenBuffer + 2.0f * buttonWidth, screenBuffer, 2.0f * buttonWidth, buttonHeight ), playerName );
        
        //you have already clicked the host button
        if ( isHosting )
        {
            //if ( Server.GetInstance().GetConnectedPlayerCount() >= 4 )
            //{
                if ( GUI.Button( new Rect( screenBuffer, buttonHeight, bannerWidth, buttonHeight ), "Start Game" ) )
                {
                    Server.GetInstance().TellClientsToBeginGame();
                }
            //}
            if ( GUI.Button( new Rect( screenBuffer, buttonHeight * 2.0f, bannerWidth, buttonHeight ), "Stop Hosting" ) )
            {
                isHosting = false;
                isJoining = false;
                Server.GetInstance().StopServer();
            }
        }
        //you have already clicked the join button
        else if ( isJoining )
        {
            if ( GUI.Button( new Rect( screenBuffer, buttonHeight, bannerWidth, buttonHeight ), "Disconnect" ) || !Client.GetInstance().IsConnected() )
            {
                isHosting = false;
                isJoining = false;
                Client.GetInstance().DisconnectFromServer();
            }
        }
        //you haven't clicked anything yet
        else
        {
            joinedIp = GUI.TextField( new Rect( screenBuffer + 2.0f * buttonWidth, screenBuffer + 2.0f * buttonHeight, 2.0f * buttonWidth, buttonHeight ), joinedIp );
            
            if ( GUI.Button( new Rect( screenBuffer, screenBuffer + buttonHeight, 2.0f * buttonWidth, buttonHeight ), "Host" ) )
            {
                isHosting = true;
                isJoining = false;
                Server.GetInstance().StartServer();
                PlayerPrefs.SetString( "PlayerName", playerName );
            }
            else if ( GUI.Button( new Rect( screenBuffer, screenBuffer + 2.0f * buttonHeight, 2.0f * buttonWidth, buttonHeight ), "Join" ) )
            {
                isHosting = false;
                isJoining = true;
                PlayerPrefs.SetString( "LastJoinedIP", joinedIp );
                PlayerPrefs.SetString( "PlayerName", playerName );
                Client.GetInstance().ConnectToServer( joinedIp );
            }
        }
        
        if ( serverIP.Length > 0 )
        {
            GUI.Box( new Rect( screenBuffer, GetGUIHeight() - buttonHeight, bannerWidth, buttonHeight ), "Your IP Address is: " + serverIP );
        }
        
        if ( status.Length > 0 )
        {
            GUI.Label( new Rect( screenBuffer, GetGUIHeight() - 2.0f * buttonHeight, bannerWidth, buttonHeight ), status );
        }
    }
    
    public static void ShowOutput( string message )
    {
        if ( LobbyGUI.GetInstance() != null )
        {
            LobbyGUI.GetInstance().ShowStatusMessage( message );
        }
        else
        {
            Debug.Log( message );
        }
    }
    
    public string GetPlayerName()
    {
        return playerName;
    }
    
    public static LobbyGUI instance;
    public static LobbyGUI GetInstance()
    {
        return instance;
    }
    public void Awake()
    {
        instance = this;
    }
    
    public float GetGUIHeight()
    {
        return Screen.height / latestScale;
    }
    
    public float GetGUIWidth()
    {
        return Screen.height / latestScale;
    }
}