using Cratesmith.Utils;
using UnityEngine;

namespace Cratesmith.Actors.Effects
{
    public class SubEffectMeshRenderer : EffectComponent
    {
        private bool			m_isPlaying;
        private MeshRenderer	m_meshRenderer;
        public override bool	isPlaying { get { return m_isPlaying; } }

        void Awake()
        {
            m_meshRenderer = gameObject.GetOrAddComponent<MeshRenderer>();
        }

        public override void Play()
        {
            m_isPlaying = true;
            m_meshRenderer.enabled = true;
        }

        public override void Stop()
        {
            m_isPlaying = false;
            m_meshRenderer.enabled = false;
        }
    }
}