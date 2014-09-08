using UnityEngine;
using System.Collections;

//this focuses on speed, not safety, so don't call a function that
//uses a length bigger than what you passed in
public class FlattenedTileArray
{
	private Tile[] array;
	private int[] dimensionMultiples;
	private int[] dimensionLengths;
	private int totalLength;
	
	public FlattenedTileArray( int[] dimensions )
	{
		dimensionLengths = dimensions;
		
		totalLength = 1;
		for ( int i = 0; i < dimensions.Length; i++ )
		{
			totalLength *= dimensions[i];
		}
		array = new Tile[ totalLength ];
		
		dimensionMultiples = new int[ dimensions.Length ];
		FindDimensionMultiples();
	}
	
	public Tile Get( int i, int j )
	{
		return array[ i * dimensionMultiples[0] + j * dimensionMultiples[1] ];
	}
	
	public Tile Get( int i, int j, int k )
	{
		return array[ i * dimensionMultiples[0] + j * dimensionMultiples[1] + k * dimensionMultiples[2] ];
	}
	
	public Tile Get( int i, int j, int k, int l )
	{
		return array[ i * dimensionMultiples[0] + j * dimensionMultiples[1] + k * dimensionMultiples[2] + l * dimensionMultiples[3] ];
	}
	
	public Tile Get( int i, int j, int k, int l, int m )
	{
		return array[ i * dimensionMultiples[0] + j * dimensionMultiples[1] + k * dimensionMultiples[2] + l * dimensionMultiples[3] + m * dimensionMultiples[4] ];
	}
	
	public Tile Get( int[] indices )
	{
		int index = 0;
		for ( int i = 0; i < indices.Length; i++ )
		{
			index += indices[i] * dimensionMultiples[i];
		}
		return array[ index ];
	}
	
	public void Set( Tile v, int i, int j )
	{
		array[ i * dimensionMultiples[0] + j * dimensionMultiples[1] ] = v;
	}
	
	public void Set( Tile v, int i, int j, int k )
	{
		array[ i * dimensionMultiples[0] + j * dimensionMultiples[1] + k * dimensionMultiples[2] ] = v;
	}
	
	public void Set( Tile v, int i, int j, int k, int l )
	{
		array[ i * dimensionMultiples[0] + j * dimensionMultiples[1] + k * dimensionMultiples[2] + l * dimensionMultiples[3] ] = v;
	}
	
	public void Set( Tile v, int i, int j, int k, int l, int m )
	{
		array[ i * dimensionMultiples[0] + j * dimensionMultiples[1] + k * dimensionMultiples[2] + l * dimensionMultiples[3] + m * dimensionMultiples[4] ] = v;
	}
	
	public void Set( Tile v, int[] indices )
	{
		int index = 0;
		for ( int i = 0; i < indices.Length; i++ )
		{
			index += indices[i] * dimensionMultiples[i];
		}
		array[ index ] = v;
	}
	
	public int GetTotalLength()
	{
		return array.Length;
	}
	
	public int GetDimensionCount()
	{
		return dimensionMultiples.Length;
	}
	
	public int GetLength( int dimension )
	{
		return dimensionLengths[ dimension ];
	}
	
	public void SetLength( int dimension, int newSize )
	{
		int targetLength = 1;
		for ( int i = 0; i < dimensionLengths.Length; i++ )
		{
			if ( dimension == i )
			{
				targetLength *= newSize;
			}
			else
			{
				targetLength *= dimensionLengths[i];
			}
		}
		
		Tile[] newArray = new Tile[ targetLength ];
		int minLength = targetLength < array.Length ? targetLength : array.Length;
		for ( int i = 0; i < minLength; i++ )
		{
			newArray[i] = array[i];
		}
		array = newArray;
		dimensionMultiples[dimension] = newSize;
		dimensionLengths[dimension] = newSize;
		
		FindDimensionMultiples();
	}
	
	private void FindDimensionMultiples()
	{
		dimensionMultiples[ dimensionLengths.Length - 1 ] = 1;
		for ( int i = dimensionLengths.Length - 2; i >= 0; i-- )
		{
			dimensionMultiples[i] = dimensionLengths[i+1] * dimensionMultiples[i+1];
		}
	}
}
