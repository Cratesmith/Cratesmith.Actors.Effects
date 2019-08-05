using Cratesmith;
using Cratesmith.Utils;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EffectComponentAnimation : EffectComponent
{
	private Animator m_animator;

	void Awake()
	{
		m_animator = gameObject.GetOrAddComponent<Animator>();
	}

	public override bool isPlaying
	{
		get
		{
			return m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1 || m_animator.IsInTransition(0);			
		}
	}

	public override void Play()
	{
	}

	public override void Stop()
	{
	}
}