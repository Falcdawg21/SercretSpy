using UnityEngine;
using System.Collections;

public class NetworkPlayerData
{
    protected NetworkPlayer networkPlayer;
    protected Player playerObject;
    protected int playerId;
    protected string name;
    
    public NetworkPlayerData( NetworkPlayer setNetworkPlayer, int setId )
    {
        networkPlayer = setNetworkPlayer;
        playerId = setId;
    }
    
    public NetworkPlayer GetNetworkPlayer()
    {
        return networkPlayer;
    }
    
    public Player GetPlayer()
    {
        return playerObject;
    }
    
    public void SetPlayer( Player p )
    {
        playerObject = p;
    }
    
    public int GetId()
    {
        return playerId;
    }
    
    public string GetName()
    {
        return name;
    }

    public void SetName( string setName )
    {
        name = setName;
    }
}