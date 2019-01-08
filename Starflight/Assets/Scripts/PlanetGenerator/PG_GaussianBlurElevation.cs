﻿
using UnityEngine;

using System.Diagnostics;
using System.Threading.Tasks;

public class PG_GaussianBlurElevation
{
	public float[,] Process( float[,] sourceElevation, int xBlurRadius, int yBlurRadius )
	{
		// UnityEngine.Debug.Log( "*** Gaussian Blur Elevation Process ***" );

		// var stopwatch = new Stopwatch();

		// stopwatch.Start();

		var outputElevationWidth = sourceElevation.GetLength( 1 );
		var outputElevationHeight = sourceElevation.GetLength( 0 );

		var xBlurBuffer = new float[ outputElevationHeight, outputElevationWidth ];
		var yBlurBuffer = new float[ outputElevationHeight, outputElevationWidth ];

		var m = outputElevationWidth - 1;

		var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = -1 };

		var numParallelThreads = 32;
		var rowsPerThread = outputElevationHeight / numParallelThreads;

		var kernelWidth = xBlurRadius * 2 + 1;

		var kernel = new float[ kernelWidth ];
		var scale = 0.0f;

		for ( var i = 0; i < kernelWidth; i++ )
		{
			scale += kernel[ i ] = 1.0f * Mathf.Exp( -Mathf.Pow( i - xBlurRadius, 2.0f ) / ( 2.0f * Mathf.Pow( xBlurRadius / 3.2f, 2.0f ) ) );
		}

		scale = 1.0f / scale;

		Parallel.For( 0, numParallelThreads, parallelOptions, j =>
		{
			for ( var row = 0; row < rowsPerThread; row++ )
			{
				var y = j * rowsPerThread + row;

				for ( var x = 0; x < outputElevationWidth; x++ )
				{
					for ( var i = 0; i < kernelWidth; i++ )
					{
						var x2 = ( Mathf.RoundToInt( x + i - xBlurRadius ) + m ) & m;

						xBlurBuffer[ y, x ] += sourceElevation[ y, x2 ] * kernel[ i ];
					}

					xBlurBuffer[ y, x ] *= scale;
				}
			}
		} );

		// UnityEngine.Debug.Log( "Blur X - " + stopwatch.ElapsedMilliseconds + " milliseconds" );

		// stopwatch.Restart();

		kernelWidth = yBlurRadius * 2 + 1;

		kernel = new float[ kernelWidth ];
		scale = 0.0f;

		for ( var i = 0; i < kernelWidth; i++ )
		{
			scale += kernel[ i ] = 1.0f * Mathf.Exp( -Mathf.Pow( i - yBlurRadius, 2.0f ) / ( 2.0f * Mathf.Pow( yBlurRadius / 3.2f, 2.0f ) ) );
		}

		scale = 1.0f / scale;

		var columnsPerThread = outputElevationWidth / numParallelThreads;

		Parallel.For( 0, numParallelThreads, parallelOptions, j =>
		{
			for ( var column = 0; column < columnsPerThread; column++ )
			{
				var x = j * columnsPerThread + column;

				for ( var y = 0; y < outputElevationHeight; y++ )
				{
					for ( var i = 0; i < kernelWidth; i++ )
					{
						var y2 = Mathf.RoundToInt( y + i - yBlurRadius );

						if ( y2 < 0 )
						{
							y2 = 0;
						}

						if ( y2 >= outputElevationHeight )
						{
							y2 = ( outputElevationHeight - 1 );
						}

						yBlurBuffer[ y, x ] += xBlurBuffer[ y2, x ] * kernel[ i ];
					}

					yBlurBuffer[ y, x ] *= scale;
				}
			}
		} );

		// UnityEngine.Debug.Log( "Blur Y - " + stopwatch.ElapsedMilliseconds + " milliseconds" );

		return yBlurBuffer;
	}
}
