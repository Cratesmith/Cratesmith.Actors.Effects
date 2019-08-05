using System.Collections;
using System.Collections.Generic;
using Cratesmith;
using Cratesmith.Utils;
using UnityEngine;

public class SubEffectLight : EffectComponent
{
	[SerializeField] Light m_lightSource = null;
	public Light lightSource {get { return m_lightSource; }}

	public override void Play()
	{
		if (m_lightSource == null)
		{
			m_lightSource = gameObject.GetOrAddComponent<Light>();
		}

		if (m_lightSource)
		{
			m_lightSource.enabled = true;
		}
	}

	public override void Stop()
	{
		if(m_lightSource!=null)
		{
			m_lightSource.enabled = false;
		}
	}

	public override bool isPlaying
	{
		get { return false; }
	}
}