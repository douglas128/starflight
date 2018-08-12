﻿
using UnityEngine;
using UnityEngine.EventSystems;

public class CommunicationsFunction : ButtonFunction
{
	private readonly ButtonFunction[] m_buttonFunctions = { new HailFunction(), new DistressFunction(), new BridgeFunction() };

	public override string GetButtonLabel()
	{
		return "Communications";
	}

	public override bool Execute()
	{
		// change the button functions and labels
		m_spaceflightController.UpdateButtonFunctions( m_buttonFunctions );

		// get to the player data
		PlayerData playerData = PersistentController.m_instance.m_playerData;

		// get the personnel file on this officer
		Personnel.PersonnelFile personnelFile = playerData.m_crewAssignment.GetPersonnelFile( CrewAssignment.Role.CommunicationsOfficer );

		// set the name of the officer
		m_spaceflightController.m_currentOfficer.text = "Officer " + personnelFile.m_name;

		return true;
	}

	public override void Cancel()
	{
		// play the deactivate sound
		m_spaceflightController.m_uiSoundController.Play( UISoundController.UISound.Deactivate );

		// return to the bridge
		m_spaceflightController.RestoreBridgeButtons();
	}
}
