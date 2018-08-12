﻿
using UnityEngine;
using UnityEngine.EventSystems;

public class SensorsFunction : ButtonFunction
{
	public override string GetButtonLabel()
	{
		return "Sensors";
	}

	public override bool Execute()
	{
		if ( m_spaceflightController.m_inDockingBay )
		{
			m_spaceflightController.m_uiSoundController.Play( UISoundController.UISound.Error );

			m_spaceflightController.m_messages.text = "We're in the docking bay.";

			m_spaceflightController.UpdateButtonSprites();

			return false;
		}

		return false;
	}
}
