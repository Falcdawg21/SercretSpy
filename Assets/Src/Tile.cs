using UnityEngine;
using System.Collections;

public class Tile : MonoBehaviour {

	public enum Status { Good = 1, Bad, Neutral };
	public GameObject selection;
	public GameObject hightlight;
    public GameObject contents;
	public GameObject trap;
    public GameObject cover;
    public GameObject flag;
	public float defaultPulseTime = 0.3f;
    public Sprite goodContentsSprite;
    public Sprite badContentsSprite;
    public Sprite goodFlagSprite;
    public Sprite badFlagSprite;
    public Color badContentsNormalColor = new Color( 0.1f, 0.1f, 0.1f, 1.0f );
	
	private Status _status;
    private Status _visualStatus; //sometimes you are showing something different than what's actually there
    private bool _showsContents;
	public bool isGoal;
    
    private int _ownerID;

    private Fader selectionFader;
	private Fader highlightFader;

	void Awake () {
		_ownerID = 0;
        _showsContents = false;
        ( flag.GetComponent( typeof( Fader ) ) as Fader ).SetShowing( false );
        SetStatus( Status.Neutral );
		selectionFader = selection.GetComponent( typeof( Fader ) ) as Fader;
		highlightFader = hightlight.GetComponent( typeof( Fader ) ) as Fader;
	}

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update() {

	}

	public void SetStatus( Status status )
    {
		_status = status;
        
        SetVisualStatus( status );
	}
    
    public Status GetStatus()
    {
        return _status;
    }
    
    public void SetVisualStatus( Status status )
    {        
        _visualStatus = status;
        
        Fader fader = ( contents.GetComponent( typeof( Fader ) ) as Fader );
        SpriteRenderer rend = ( contents.renderer as SpriteRenderer );
        
        if ( status == Status.Neutral )
        {
            fader.SetShowing( false );
            trap.renderer.enabled = false;
        }
        else if ( status == Status.Bad )
        {
			fader.SetShowing( false );

			trap.renderer.enabled = true;

			Color color;
			Player owner = Game.GetInstance().GetPlayerForID( _ownerID );
			rend.sprite = badContentsSprite;
			
			if ( owner == Game.GetInstance().GetLocalPlayer() )
			{
				color = owner.GetPlayerColor();
			}
			else
			{
				color = badContentsNormalColor;
			}
            
            trap.renderer.material.color = color;
        }
        else if ( status == Status.Good )
        {
            rend.sprite = goodContentsSprite;
            rend.color = Color.white;
            
			trap.renderer.enabled = false;
            
            fader.SetShowing( true );
        }
    }
    
    public Status GetVisualStatus()
    {
        return _visualStatus;
    }
    
    //for convenience
    public Fader GetSelectionFader()
    {
        return selectionFader;
    }

	public Fader GetHighlightFader()
	{
		return highlightFader;
	}

	public void MoveToPosition( Vector2 position )
	{
		Mover mover = GetComponent( typeof( Mover ) ) as Mover;
		mover.SetTargetPos( position );
	}

    public int GetOwnerID()
    {
        return _ownerID;
    }
    
    public void SetOwnerID( int ownerID )
    {
        _ownerID = ownerID;
    }
    
    public void SetFogOfWarShowing( bool fogOn )
	{
		if ( _showsContents )
        {
            return;
        }
        
        Fader coverFader = ( cover.GetComponent( typeof( Fader ) ) as Fader );
        
        if ( fogOn && !coverFader.IsShowing() )
        {
            coverFader.FadeIn();
        }
        else if ( !fogOn && coverFader.IsShowing() )
        {
            coverFader.FadeOut();
        }
    }
    
    public bool IsFadingFogOfWar()
    {
        if ( _showsContents )
        {
            return false;
        }
        
        Fader coverFader = ( cover.GetComponent( typeof( Fader ) ) as Fader );
        return coverFader.IsFading();
    }
    
    public void SetShowsContents( bool showsContents )
    {
        _showsContents = showsContents;
        ( cover.GetComponent( typeof( Fader ) ) as Fader ).SetShowing( !showsContents );
    }
    
    public bool ShowsContents()
    {
        return _showsContents;
    }
    
    public void Flag( bool isGood )
    {
        ( flag.renderer as SpriteRenderer ).sprite = isGood ? goodFlagSprite : badFlagSprite;
        ( flag.GetComponent( typeof( Fader ) ) as Fader ).FadeIn();
    }
}
