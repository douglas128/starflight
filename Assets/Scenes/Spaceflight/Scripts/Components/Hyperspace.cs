﻿
using UnityEngine;

public class Hyperspace : MonoBehaviour
{
	// our star template (that we will duplicate all over the place)
	public GameObject m_starTemplate;

	// our flux template (that we will duplicate all over the place)
	public GameObject m_fluxTemplate;

	// true if we are currently traveling through a flux
	bool m_travelingThroughFlux;

	// flux travel timer
	float m_timer;

	// flux travel duration
	float m_fluxTravelDuration;

	// the starting point
	Vector3 m_fluxTravelStartPosition;

	// the ending point
	Vector3 m_fluxTravelEndPosition;

	// convenient access to the spaceflight controller
	SpaceflightController m_spaceflightController;

	// unity awake
	private void Awake()
	{
		// get the spaceflight controller
		GameObject controllersGameObject = GameObject.FindWithTag( "Spaceflight Controllers" );
		m_spaceflightController = controllersGameObject.GetComponent<SpaceflightController>();
	}

	// unity start
	void Start()
	{
		// get to the game data
		GameData gameData = DataController.m_instance.m_gameData;

		// make copies of the star template
		foreach ( Star star in gameData.m_starList )
		{
			// clone the star
			GameObject clonedStar = Instantiate( m_starTemplate, star.m_worldCoordinates, Quaternion.identity, transform ) as GameObject;

			// get to the star quad mesh object
			Transform starQuad = clonedStar.transform.Find( "Star Quad" );

			// calculate the scale of the star quad
			float scale = ( 1.0f + star.m_scale / 0.6f ) * 128.0f;

			// scale the star image based on the class of the star system
			starQuad.localScale = new Vector3( scale, scale, 1.0f );
		}

		// hide the star template
		m_starTemplate.SetActive( false );

		// make copies of the flux template
		foreach ( Flux flux in gameData.m_fluxList )
		{
			// clone the flux
			Instantiate( m_fluxTemplate, flux.m_from, Quaternion.identity, transform );
		}

		// hide the flux template
		m_fluxTemplate.SetActive( false );

		// we are not travelling through a flux now
		m_travelingThroughFlux = false;
	}

	// unity update
	void Update()
	{
		// get to the player data
		PlayerData playerData = DataController.m_instance.m_playerData;

		// get to the game data
		GameData gameData = DataController.m_instance.m_gameData;

		// configure the infinite starfield system to become visible at lower speeds
		m_spaceflightController.m_player.SetStarfieldFullyVisibleSpeed( 3.5f );

		// are we travelling through a flux right now?
		if ( m_travelingThroughFlux )
		{
			// update the timer
			m_timer += Time.deltaTime;

			// travel through the flux
			float t = Mathf.Cos( ( m_timer / m_fluxTravelDuration ) * Mathf.PI ) * -0.5f + 0.5f;
			
			Vector3 newPosition = Vector3.Lerp( m_fluxTravelStartPosition, m_fluxTravelEndPosition, t );
			m_spaceflightController.UpdatePlayerPosition( newPosition );

			// rotate the skybox in the direction of the flux travel
			Vector3 direction = Vector3.Normalize( m_fluxTravelStartPosition - m_fluxTravelEndPosition );
			m_spaceflightController.m_player.RotateSkybox( -direction, Time.deltaTime * 5.0f );

			// have we arrived?
			if ( m_timer >= m_fluxTravelDuration )
			{
				// end the space warp effect
				m_spaceflightController.m_player.StopSpaceWarp();

				// let the player move the ship again
				m_spaceflightController.m_player.Unfreeze();

				// update the map coordinates
				m_spaceflightController.m_spaceflightUI.UpdateCoordinates();

				// play the exit warp sound
				SoundController.m_instance.PlaySound( SoundController.Sound.ExitWarp );

				// not travelling through the flux any more
				m_travelingThroughFlux = false;
			}
		}
		else
		{
			// go through each star in the game
			foreach ( Star star in gameData.m_starList )
			{
				// did we breach it?
				float distance = Vector3.Distance( playerData.m_starflight.m_hyperspaceCoordinates, star.m_worldCoordinates );

				if ( distance < star.GetBreachDistance() )
				{
					Debug.Log( "Entering star system at " + star.m_xCoordinate + " x " + star.m_yCoordinate + ", distance = " + distance );

					// change the system
					playerData.m_starflight.m_currentStarId = star.m_id;

					// update the player location
					playerData.m_starflight.m_location = Starflight.Location.StarSystem;

					// set the position of the player inside this system
					Vector3 starToShip = playerData.m_starflight.m_hyperspaceCoordinates - (Vector3) star.m_worldCoordinates;
					starToShip.Normalize();
					playerData.m_starflight.m_systemCoordinates = starToShip * ( 8192.0f - 16.0f );

					// switch to the star system mode
					m_spaceflightController.SwitchMode();
				}
			}

			// go through each flux in the game
			foreach ( Flux flux in gameData.m_fluxList )
			{
				// did we breach it?
				float distance = Vector3.Distance( playerData.m_starflight.m_hyperspaceCoordinates, flux.m_from );

				if ( distance < flux.GetBreachDistance() )
				{
					Debug.Log( "Entering flux at " + flux.m_x1 + " x " + flux.m_y1 + ", distance = " + distance );

					// prevent the player from maneuvering
					m_spaceflightController.m_player.Freeze();

					// reset the timer
					m_timer = 0.0f;

					// figure out how long we should take to travel through this flux
					m_fluxTravelDuration = Mathf.Max( 2.0f, Vector3.Distance( flux.m_from, flux.m_to ) / 2048.0f );

					// compute the starting and ending point of the flux travel
					m_fluxTravelStartPosition = playerData.m_starflight.m_hyperspaceCoordinates;
					m_fluxTravelEndPosition = flux.m_to + (Vector3) playerData.m_starflight.m_currentDirection * ( flux.GetBreachDistance() + 16.0f );

					// start the warp cinematics
					m_travelingThroughFlux = true;

					// start the warp effect
					m_spaceflightController.m_player.StartSpaceWarp();

					// play the enter warp sound
					SoundController.m_instance.PlaySound( SoundController.Sound.EnterWarp );
				}
			}
		}
	}

	// call this to hide the hyperspace stuff
	public void Hide()
	{
		// hide the hyperspace objects
		gameObject.SetActive( false );
	}

	// call this to show the hyperspace stuff
	public void Show()
	{
		// show the hyperspace objects
		gameObject.SetActive( true );

		// show the player (ship)
		m_spaceflightController.m_player.Show();

		// make sure the camera is at the right height above the zero plane
		m_spaceflightController.m_player.DollyCamera( 1024.0f );

		// get to the player data
		PlayerData playerData = DataController.m_instance.m_playerData;

		// move the ship to where we are in hyperspace
		m_spaceflightController.m_player.SetPosition( playerData.m_starflight.m_hyperspaceCoordinates );

		// calculate the new rotation of the player
		Quaternion newRotation = Quaternion.LookRotation( playerData.m_starflight.m_currentDirection, Vector3.up );

		// update the player rotation
		m_spaceflightController.m_player.SetRotation( newRotation );

		// show the status display
		m_spaceflightController.m_displayController.ChangeDisplay( m_spaceflightController.m_displayController.m_statusDisplay );

		// play the star system music track
		MusicController.m_instance.ChangeToTrack( MusicController.Track.Hyperspace );
	}
}
