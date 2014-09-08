using UnityEngine;
using System.Collections;

public class Fader : MonoBehaviour
{
    public float defaultLength = 0.3f;
    public bool fadedByDefault = false;
    
    protected float _fadeTime;
    protected float _fadeLength;
    protected FadeType _fadeType;
        
    public enum FadeType
    {
        None = 0,
        Pulse,
        In,
        Out
    };
    
    public void Awake()
    {
        _fadeType = FadeType.None;
        
        if ( fadedByDefault )
        {
            ChangeAlpha( 0.0f );
        }
    }
    
    public void Update()
    {
        if ( _fadeType != FadeType.None )
        {
            _fadeTime += Time.deltaTime;
            float t = _fadeTime / _fadeLength;
            
            if ( _fadeType == FadeType.Pulse )
            {
                float halfTime = _fadeLength / 2.0f;
                t = ( _fadeTime > halfTime ? _fadeLength - _fadeTime : _fadeTime ) / halfTime;
            }
            else if ( _fadeType == FadeType.Out )
            {
                t = 1.0f - t;
            }
            
            t = Mathf.Clamp( t, 0.0f, 1.0f );
                        
            //stop if we are done
            if ( _fadeTime >= _fadeLength )
            {
                //disable the renderer unless we've been fading in
                if (  _fadeType != FadeType.In )
                {
                    ChangeAlpha( 0.0f );
                }
                else
                {
                    ChangeAlpha( 1.0f );
                }
                
                _fadeType = FadeType.None;
            }
            //change the alpha to fit the fade
            else
            {
                ChangeAlpha( t );
            }
        }
    }
    
    public void SetShowing( bool showing )
    {
        ChangeAlpha( showing ? 1.0f : 0.0f );
    }
    
    public bool IsShowing()
    {
        return renderer.enabled;
    }
    
    public bool IsFading()
    {
        float a = ( renderer as SpriteRenderer ).color.a;
        return IsShowing() &&  a != 0.0f && a != 1.0f;
    }
    
    public void Pulse()
    {
        Pulse( defaultLength );
    }
    
    public void Pulse( float time )
    {
        BeginFade( FadeType.Pulse, time );
    }
    
    public void FadeIn()
    {
        FadeIn( defaultLength / 2.0f );
    }
    
    public void FadeIn( float time )
    {
        BeginFade( FadeType.In, time );
    }
    
    public void FadeOut()
    {
        FadeOut( defaultLength / 2.0f );
    }
    
    public void FadeOut( float time )
    {
        BeginFade( FadeType.Out, time );
    }
    
    protected void BeginFade( FadeType type, float time )
    {
        _fadeTime = 0.0f;
        _fadeLength = time;
        _fadeType = type;
    }
    
    protected void ChangeAlpha( float a )
    {
        Color c = ( renderer as SpriteRenderer ).color;
        c.a = a;
        ( renderer as SpriteRenderer ).color = c;
        
        renderer.enabled = a > 0.0f;
    }
}