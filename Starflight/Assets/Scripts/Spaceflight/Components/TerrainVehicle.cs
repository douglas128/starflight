﻿
using UnityEngine;
using System;

public class TerrainVehicle : MonoBehaviour
{
	// the terrain vehicle model (we apply body roll rotation to it instead of this object)
	public GameObject m_terrainVehicleModel;

	// the terrain vehicle wheels (fr, fl, mr, ml, rr, rl)
	public GameObject[] m_wheels;

	// the steering joints (fr, fl, mr, ml, rr, rl)
	public GameObject[] m_steeringJoints;

	// how fast to spin the wheels
	public float m_wheelTurnSpeed;

	// how fast the wheels steer
	public float m_wheelSteerSpeed;

	// the resting suspension height above the ground
	public float m_neutralSuspensionHeight;

	// the maximum speed of the player
	public float m_maximumSpeed;

	// the time to reach the maximum speed
	public float m_timeToReachMaximumSpeed;

	// the time to slow down (coast) to a stop
	public float m_timeToStop;

	// how much to sink the TV in water
	public float m_floatDepth;

	// how fast to bob the TV in water
	public float m_waterBobSpeed;

	// how much to bob the TV in water
	public float m_waterBobAmount;

	// the terrain grid elevation scale
	public float m_elevationScale = 100.0f;

	// the planet generator
	PlanetGenerator m_planetGenerator;

	// whether or not the engine is on or off
	bool m_enginesAreOn;

	// keep track of the last direction (for steering the wheels)
	Vector3 m_lastDirection;

	// the amount of wheel slip (reduces effiency)
	float m_wheelEfficiency;

	// how deep in water are we
	float m_waterEffectAmount;

	// for gizmo drawing
	Vector3[] m_debugVectors;

	TerrainVehicle()
	{
		m_debugVectors = new Vector3[ 12 ];
	}

	// unity awake
	void Awake()
	{
	}

	// unity start
	void Start()
	{
		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// get the planet controller
		var planetController = SpaceflightController.m_instance.m_starSystem.GetPlanetController( playerData.m_general.m_currentPlanetId );

		// save the planet generator
		m_planetGenerator = planetController.GetPlanetGenerator();

		// jump start the last direction
		m_lastDirection = playerData.m_general.m_currentDirection;
	}

	// unity update
	void Update()
	{
		// don't do anything if the game is paused
		if ( SpaceflightController.m_instance.m_gameIsPaused )
		{
			return;
		}

		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// are the engines turned on?
		if ( m_enginesAreOn )
		{
			// calculate the acceleration
			var acceleration = Time.deltaTime * playerData.m_playerShip.m_acceleration / ( m_timeToReachMaximumSpeed * 25.0f );

			// increase the current speed
			playerData.m_general.m_currentSpeed = Mathf.Lerp( playerData.m_general.m_currentSpeed, m_maximumSpeed, acceleration );
		}
		else
		{
			// slow the ship to a stop
			playerData.m_general.m_currentSpeed = Mathf.Lerp( playerData.m_general.m_currentSpeed, 0.0f, Time.deltaTime / m_timeToStop );
		}

		// check if the ship is moving
		if ( playerData.m_general.m_currentSpeed >= 0.1f )
		{
			// calculate the new position of the player
			var newPosition = transform.localPosition + (Vector3) playerData.m_general.m_currentDirection * m_wheelEfficiency * playerData.m_general.m_currentSpeed * Time.deltaTime;

			// update the player position
			transform.localPosition = newPosition;

			// update the player data (it will save out to disk eventually)
			playerData.m_general.m_coordinates = newPosition;

			// update the last known disembarked coordinates
			playerData.m_general.m_lastDisembarkedCoordinates = playerData.m_general.m_coordinates;

			// turn the wheels
			foreach ( var wheel in m_wheels )
			{
				wheel.transform.localRotation *= Quaternion.Euler( 0.0f, 0.0f, playerData.m_general.m_currentSpeed * m_wheelTurnSpeed * Time.deltaTime );
			}

			// update the map coordinates
			SpaceflightController.m_instance.m_viewport.UpdateCoordinates();
		}

		// set the rotation of the terrain vehicle
		m_terrainVehicleModel.transform.localRotation = Quaternion.LookRotation( playerData.m_general.m_currentDirection, Vector3.up ) * Quaternion.Euler( -90.0f, 0.0f, 0.0f );

		// add a random bob if in water
		var bobX = ( Mathf.PerlinNoise( Time.time * m_waterBobSpeed, 20.0f ) * 2.0f - 1.0f ) * m_waterBobAmount * m_waterEffectAmount;
		var bobY = ( Mathf.PerlinNoise( Time.time * m_waterBobSpeed, 80.0f ) * 2.0f - 1.0f ) * m_waterBobAmount * m_waterEffectAmount;

		m_terrainVehicleModel.transform.rotation *= Quaternion.Euler( bobX, bobY, 0.0f );

		// get the number of degrees we are turning the terrain vehicle (compared to the last frame)
		var steeringAngle = Vector3.SignedAngle( playerData.m_general.m_currentDirection, m_lastDirection, Vector3.up );

		// scale the angle enough so we actually see the wheels turning (but max it out at 45 degrees in either direction)
		steeringAngle = Mathf.Max( -45.0f, Mathf.Min( 45.0f, steeringAngle * 12.0f ) );

		// steer the wheels
		m_steeringJoints[ 0 ].transform.localRotation = Quaternion.Slerp( m_steeringJoints[ 0 ].transform.localRotation, Quaternion.Euler( 0.0f, -steeringAngle, 0.0f ), Time.deltaTime * m_wheelSteerSpeed );
		m_steeringJoints[ 1 ].transform.localRotation = Quaternion.Slerp( m_steeringJoints[ 1 ].transform.localRotation, Quaternion.Euler( 0.0f, -steeringAngle, 0.0f ), Time.deltaTime * m_wheelSteerSpeed );
		m_steeringJoints[ 4 ].transform.localRotation = Quaternion.Slerp( m_steeringJoints[ 4 ].transform.localRotation, Quaternion.Euler( 0.0f, steeringAngle, 0.0f ), Time.deltaTime * m_wheelSteerSpeed );
		m_steeringJoints[ 5 ].transform.localRotation = Quaternion.Slerp( m_steeringJoints[ 5 ].transform.localRotation, Quaternion.Euler( 0.0f, steeringAngle, 0.0f ), Time.deltaTime * m_wheelSteerSpeed );

		// update the last direction
		m_lastDirection = playerData.m_general.m_currentDirection;

		// get the current position of the terrain vehicle
		var tvPosition = ApplyElevation( playerData.m_general.m_coordinates, true );

		// update the tv game object position
		transform.localPosition = tvPosition;

		// reset the steering joints
		foreach ( var steeringJoint in m_steeringJoints )
		{
			steeringJoint.transform.localPosition = Vector3.zero;
		}

		// calculate the normal of the terrain between the center and the front wheels
		var wheel1 = ApplyElevation( m_wheels[ 1 ].transform.position, false );
		var wheel2 = ApplyElevation( m_wheels[ 0 ].transform.position, false );

		var side1 = wheel1 - tvPosition;
		var side2 = wheel2 - tvPosition;

		var normal1 = Vector3.Cross( side1, side2 );

		m_debugVectors[ 0 ] = tvPosition;
		m_debugVectors[ 1 ] = wheel1;
		m_debugVectors[ 2 ] = tvPosition;
		m_debugVectors[ 3 ] = wheel2;
		m_debugVectors[ 4 ] = ( wheel1 + wheel2 ) * 0.5f;
		m_debugVectors[ 5 ] = m_debugVectors[ 4 ] + normal1;

		wheel1 = ApplyElevation( m_wheels[ 4 ].transform.position, false );
		wheel2 = ApplyElevation( m_wheels[ 5 ].transform.position, false );

		side1 = wheel1 - tvPosition;
		side2 = wheel2 - tvPosition;

		var normal2 = Vector3.Cross( side1, side2 );

		m_debugVectors[ 6 ] = tvPosition;
		m_debugVectors[ 7 ] = wheel1;
		m_debugVectors[ 8 ] = tvPosition;
		m_debugVectors[ 9 ] = wheel2;
		m_debugVectors[ 10 ] = ( wheel1 + wheel2 ) * 0.5f;
		m_debugVectors[ 11 ] = m_debugVectors[ 10 ] + normal2;

		var averagedNormal = Vector3.Normalize( normal1 + normal2 );

		// update the attitude of the body based on the average normal
		m_terrainVehicleModel.transform.rotation = Quaternion.FromToRotation( Vector3.up, averagedNormal ) * m_terrainVehicleModel.transform.rotation;

		// move the tv wheels up and down depending on the height of the terrain under the wheels
		foreach ( var steeringJoint in m_steeringJoints )
		{
			var wheelPosition = ApplyElevation( steeringJoint.transform.position, false );

			var offset = wheelPosition.y - steeringJoint.transform.position.y;

			wheelPosition.x = 0.0f;
			wheelPosition.y = offset + m_neutralSuspensionHeight;
			wheelPosition.z = 0.0f;

			steeringJoint.transform.localPosition = wheelPosition;
		}
	}

	Vector3 ApplyElevation( Vector3 worldCoordinates, bool updateWheelEfficiency )
	{
		var x = worldCoordinates.x * 0.25f + m_planetGenerator.m_textureMapWidth * 0.5f - 0.5f;
		var y = worldCoordinates.z * 0.25f + m_planetGenerator.m_textureMapHeight * 0.5f - 0.5f;

		var groundElevation = m_planetGenerator.GetBicubicSmoothedElevation( x, y ) * m_elevationScale;
		var waterElevation = m_planetGenerator.m_waterHeight * m_elevationScale;
		var floatElevation = waterElevation - m_floatDepth;

		worldCoordinates.y = Mathf.Max( floatElevation, groundElevation );

		if ( updateWheelEfficiency )
		{
			m_wheelEfficiency = Mathf.Clamp( ( worldCoordinates.y - floatElevation ) / m_floatDepth, 0.0f, 1.0f );
			m_waterEffectAmount = Mathf.Pow( 1.0f - m_wheelEfficiency, 2.0f );
			m_wheelEfficiency = m_wheelEfficiency * 0.5f + 0.5f;
		}

		return worldCoordinates;
	}

	// call this to turn on the engines (accelerate)
	public void TurnOnEngines()
	{
		m_enginesAreOn = true;
	}

	// call this to turn off the engines (brake)
	public void TurnOffEngines()
	{
		m_enginesAreOn = false;
	}

#if UNITY_EDITOR

	// draw gizmos to help debug the game
	void OnDrawGizmos()
	{
		Gizmos.color = Color.green;

		for ( var i = 0; i < m_debugVectors.Length; i += 2 )
		{
			Gizmos.DrawLine( m_debugVectors[ i ], m_debugVectors[ i + 1 ] );
		}
	}

#endif
}
