﻿
using UnityEngine;
using UnityEngine.EventSystems;

public class ManeuverFunction : ButtonFunction
{
	public override string GetButtonLabel()
	{
		return "Maneuver";
	}

	public override void Execute()
	{
		if ( m_spaceflightController.m_inDockingBay )
		{
			m_spaceflightController.m_uiSoundController.Play( UISoundController.UISound.Error );

			m_spaceflightController.m_messages.text = "Standing by to launch.";

			m_spaceflightController.UpdateButtonSprites();
		}
	}
}
