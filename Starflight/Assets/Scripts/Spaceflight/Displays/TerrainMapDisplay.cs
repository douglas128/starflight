﻿
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerrainMapDisplay : ShipDisplay
{
	// the planet
	public Image m_mapImage;
	public Image m_legendImage;

	bool m_materialSwapped;

	// unity start
	public override void Start()
	{
	}

	// unity update
	public override void Update()
	{
	}

	// the display label
	public override string GetLabel()
	{
		return "Terrain Map";
	}

	// show
	public override void Show()
	{
		// call base show
		base.Show();

		// get to the player data
		var playerData = DataController.m_instance.m_playerData;

		// get the planet controller
		var planetController = m_spaceflightController.m_starSystem.GetPlanetController( playerData.m_general.m_currentPlanetId );

		// create a new material for the map
		var material = planetController.GetMaterial();

		if ( !m_materialSwapped )
		{
			m_mapImage.material = new Material( m_mapImage.material );

			m_materialSwapped = true;
		}

		m_mapImage.material.SetTexture( "AlbedoMap", material.GetTexture( "AlbedoMap" ) );
		m_mapImage.material.SetTexture( "SpecularMap", material.GetTexture( "SpecularMap" ) );
		m_mapImage.material.SetTexture( "NormalMap", material.GetTexture( "NormalMap" ) );
		m_mapImage.material.SetTexture( "WaterMaskMap", material.GetTexture( "WaterMaskMap" ) );

		// create a new material for the legend
		var legendTexture = planetController.GetLegendTexture();

		var sprite = Sprite.Create( legendTexture, new Rect( 0.0f, 0.0f, legendTexture.width, legendTexture.height ), new Vector2( 0.5f, 0.5f ) );

		m_legendImage.sprite = sprite;
	}

	// hide
	public override void Hide()
	{
		// call base hide
		base.Hide();
	}
}
