using UnityEngine;
using System.Collections;

public class GameGUI : MonoBehaviour
{
    public GUISkin skin;
    
    public float screenBuffer = 5.0f;
    public float buttonHeight = 88.0f;
    public float buttonWidth = 120.0f;
    public float bannerWidth = 480.0f;
    public float smallBannerWidth = 360.0f;
    public float designWidth = 470.0f;

	public Texture2D leftKeyboardArrowEnabledTexture;
	public Texture2D rightKeyboardArrowEnabledTexture;
	public Texture2D upKeyboardArrowEnabledTexture;
	public Texture2D downKeyboardArrowEnabledTexture;

	public Texture2D leftKeyboardArrowDisabledTexture;
	public Texture2D rightKeyboardArrowDisabledTexture;
	public Texture2D upKeyboardArrowDisabledTexture;
	public Texture2D downKeyboardArrowDisabledTexture;
    
    protected float latestScale;
    
    public void OnGUI()
    {
        GUI.skin = skin;
        latestScale = Screen.width / designWidth;
        GUI.matrix = Matrix4x4.Scale( new Vector3( latestScale, latestScale, 1.0f ) );
		GUIStyle style = new GUIStyle( GUI.skin.label );

		if ( Game.GetInstance().GetLocalPlayer() != null )
        {
            float teamY = GetGUIHeight() - 4.0f * buttonHeight;
            if ( Game.GetInstance().ShouldShowPlayerTeam() ||
                 GUI.RepeatButton( new Rect( GetGUIWidth() - screenBuffer - buttonWidth / 2.0f, teamY, buttonWidth / 2.0f, buttonHeight ), "?" ) )
            {
				GUI.Label( new Rect( GetGUIWidth() - screenBuffer - 2.5f * buttonWidth, teamY - buttonHeight, buttonWidth, buttonHeight ), ( Game.GetInstance().GetLocalPlayer().IsSpy() ? "Spy" : "Agent" ), style );
                
                //add extra spy ability buttons
/*                if ( Game.GetInstance().GetLocalPlayer().IsSpy() )
                {
                    if ( Button( new Rect( GetGUIWidth() - screenBuffer - 2.5f * buttonWidth, teamY, buttonWidth, buttonHeight ), "Steal", Game.GetInstance().CanSteal( Game.GetInstance().GetLocalPlayer() ) ) )
                    {
                        //TODO
                    }
                }*/
            }
            
            //show the current player's turn
            style.richText = true;
            Color playerColor = Game.GetInstance().GetCurrentPlayer().GetPlayerColor();
            string hex = "#" + GetHexColor( playerColor );
            GUI.Label( new Rect( screenBuffer, 0.0f, bannerWidth, buttonHeight - 5.0f ), "<color=" + hex + ">" + Game.GetInstance().GetCurrentPlayerPossessiveName() + "</color> Turn", style );
        }
        
		// show won GUI
		if( Game.GetInstance().GetState() == Game.State.Won )
		{
			GUI.Box( new Rect( screenBuffer, GetGUIHeight() - buttonHeight, 2.0f * buttonWidth, buttonHeight ), "You Win!" );
			if( Network.isServer )
			{
				if( GUI.Button( new Rect( screenBuffer + 2.0f * buttonWidth, GetGUIHeight() - buttonHeight, 2.0f * buttonWidth, buttonHeight ), "Restart" ) )
				{
					Game.GetInstance().Restart();
				}
			}
			return;
		}
		// show lose GUI
		else if( Game.GetInstance().GetState() == Game.State.Lost )
		{
			GUI.Box( new Rect( screenBuffer, GetGUIHeight() - buttonHeight, 2.0f * buttonWidth, buttonHeight ), "You Lose!" );
			if( Network.isServer )
			{
				if( GUI.Button( new Rect( screenBuffer + 2.0f * buttonWidth, GetGUIHeight() - buttonHeight, 2.0f * buttonWidth, buttonHeight ), "Restart" ) )
				{
					Game.GetInstance().Restart();
				}
			}
			return;
		}

        //show turn GUI
        if ( Game.GetInstance().IsMyTurn() )
        {
            if ( Game.GetInstance().GetTurnState() == Game.TurnState.WaitingForChoice )
            {
                //action menu
                if ( Button( new Rect( screenBuffer, GetGUIHeight() - buttonHeight, buttonWidth, buttonHeight ), "Ask", Game.GetInstance().CanPerformAsk() ) )
                {
                    Game.GetInstance().SetTurnState( Game.TurnState.SelectingAsk );
                }
                if ( GUI.Button( new Rect( screenBuffer + buttonWidth, GetGUIHeight() - buttonHeight, buttonWidth, buttonHeight ), "Move" ) )
                {
                    Game.GetInstance().SetTurnState( Game.TurnState.SelectingMove );
                }
				if ( Button( new Rect( screenBuffer + buttonWidth * 2.0f, GetGUIHeight() - buttonHeight, buttonWidth, buttonHeight ), "Shift",  Game.GetInstance().CanPerformShift() ) )
				{
					Game.GetInstance().SetTurnState( Game.TurnState.SelectingShiftTile );
					Game.GetInstance().HighlightShiftableTiles();
                }
                if ( Button( new Rect( screenBuffer + buttonWidth * 3.0f, GetGUIHeight() - buttonHeight, buttonWidth, buttonHeight ), "Push", Game.GetInstance().OtherPlayerIsAtMyPosition() && Game.GetInstance().CanPerformPush() ) )
                {
                    Game.GetInstance().SetTurnState( Game.TurnState.ChoosingPushedPlayer );
                }
            }
            else
            {
                string boxText = "";
                float boxOffset = buttonWidth;
                
                if ( Game.GetInstance().GetTurnState() == Game.TurnState.SelectingMove )
                {
                    boxText = "Select tile to move to";
                }
                else if ( Game.GetInstance().GetTurnState() == Game.TurnState.SelectingShiftTile )
                {
                    boxText = "Select tile to shift";
                }
                else if ( Game.GetInstance().GetTurnState() == Game.TurnState.SelectingShiftDirection )
                {
					if ( Button( new Rect( screenBuffer + buttonWidth * 0.5f, GetGUIHeight() - buttonHeight * 2, buttonWidth, buttonHeight ), leftKeyboardArrowEnabledTexture, leftKeyboardArrowDisabledTexture, Game.GetInstance().CanShiftInDirection( Grid.Directions.Left ) ) )
					{
						Game.GetInstance().PerformShiftOnSelectedTile( Grid.Directions.Left );
					}
					if ( Button( new Rect( screenBuffer + buttonWidth * 2.5f, GetGUIHeight() - buttonHeight * 2, buttonWidth, buttonHeight ), rightKeyboardArrowEnabledTexture, rightKeyboardArrowDisabledTexture, Game.GetInstance().CanShiftInDirection( Grid.Directions.Right ) ) )
					{
						Game.GetInstance().PerformShiftOnSelectedTile( Grid.Directions.Right );
					}
					if ( Button( new Rect( screenBuffer + buttonWidth * 1.5f, GetGUIHeight() - buttonHeight * 3, buttonWidth, buttonHeight ), upKeyboardArrowEnabledTexture, upKeyboardArrowDisabledTexture, Game.GetInstance().CanShiftInDirection( Grid.Directions.Up ) ) )
					{
						Game.GetInstance().PerformShiftOnSelectedTile( Grid.Directions.Up );
					}
					if ( Button( new Rect( screenBuffer + buttonWidth * 1.5f, GetGUIHeight() - buttonHeight * 2, buttonWidth, buttonHeight ), downKeyboardArrowEnabledTexture, downKeyboardArrowDisabledTexture, Game.GetInstance().CanShiftInDirection( Grid.Directions.Down ) ) )
					{
						Game.GetInstance().PerformShiftOnSelectedTile( Grid.Directions.Down );
					}

					boxText = "Select direction to shift";
				}
				else if ( Game.GetInstance().GetTurnState() == Game.TurnState.SelectingAsk )
                {
                    boxText = "Select tile to ask about";
                }
                else if ( Game.GetInstance().GetTurnState() == Game.TurnState.ChoosingPushedPlayer )
                {
                    boxText = "Select player to push";
                }
                else if ( Game.GetInstance().GetTurnState() == Game.TurnState.SelectingPushDirection )
                {
                    boxText = "Choose tile to push to";
                }
                
                if ( Game.GetInstance().GetTurnState() != Game.TurnState.WaitingForTell )
                {
                    if ( GUI.Button( new Rect( screenBuffer, GetGUIHeight() - buttonHeight, buttonWidth, buttonHeight ), "Cancel" ) )
                    {
                        Game.GetInstance().ClearSelectedTiles();
						Game.GetInstance().ClearHighlightedTiles();
                        Game.GetInstance().SetTurnState( Game.TurnState.WaitingForChoice );
                    }
                }
                else
                {
                    boxText = "Waiting for teller...";
                    boxOffset = 0.0f;
                }
                
                GUI.Box( new Rect( screenBuffer + boxOffset, GetGUIHeight() - buttonHeight, smallBannerWidth, buttonHeight ), boxText );
            }
        }
        //show tell GUI
        else if ( Game.GetInstance().GetState() == Game.State.Telling )
        {
            GUI.Box( new Rect( screenBuffer, GetGUIHeight() - buttonHeight * 2.0f, bannerWidth, buttonHeight ), "Is this tile:" );
            
            if ( GUI.Button( new Rect( screenBuffer, GetGUIHeight() - buttonHeight, buttonWidth, buttonHeight ), "Bad" ) )
            {
                Game.GetInstance().DoTell( false );
            }
            if ( GUI.Button( new Rect( screenBuffer + buttonWidth, GetGUIHeight() - buttonHeight, buttonWidth, buttonHeight ), "Good" ) )
            {
                Game.GetInstance().DoTell( true );
            }
        }
        //show waiting for server dialog
        else if ( Game.GetInstance().GetState() == Game.State.WaitingForServer )
        {
            GUI.Box( new Rect( screenBuffer, GetGUIHeight() - buttonHeight, bannerWidth, buttonHeight ), "Waiting for server..." );
        }
	}
    
    public float GetGUIWidth()
    {
        return Screen.width / latestScale;
    }
    
    public float GetGUIHeight()
    {
        return Screen.height / latestScale;
    }
    
	public bool Button( Rect rect, Texture2D enabledImage, Texture2D disabledImage, bool enabled )
	{
		if ( enabled )
		{
			return GUI.Button( rect, enabledImage );
		}
		else
		{
			GUI.Box( rect, disabledImage );
			return false;
		}
	}

	public bool Button( Rect rect, string text, bool enabled )
	{
		if ( enabled )
        {
            return GUI.Button( rect, text );
        }
        else
        {
            GUI.Box( rect, text );
            return false;
        }
    }
    
    protected string[] HEX = new string[]
    {
        "0",
        "1",
        "2",
        "3",
        "4",
        "5",
        "6",
        "7",
        "8",
        "9",
        "a",
        "b",
        "c",
        "d",
        "e",
        "f"
    };

    protected string GetHexPart( float value )
    {
        value = Mathf.Clamp( value, 0.0f, 1.0f );
        int value255 = (int) ( 255 * value );
        return GetHexPart( value255 );
    }
    
    protected string GetHexPart( int value255 )
    {
        int leftPart = value255 / 16;
        int rightPart = value255 - leftPart * 16;
        return HEX[ leftPart ] + HEX[ rightPart ];
    }

    public string GetHexColor( Color color )
    {
        return GetHexColor( (int) ( 255 * color.r ), (int) ( 255 * color.g ), (int) ( 255 * color.b ) );
    }

    public string GetHexColor( int r, int g, int b )
    {
        return GetHexPart( r ) + GetHexPart( g ) + GetHexPart( b );
    }
}