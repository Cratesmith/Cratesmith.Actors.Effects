using Cratesmith.ScriptExecutionOrder;
using Cratesmith.Utils;
using UnityEngine;

namespace Cratesmith.Actors.Effects
{
    [RequireComponent(typeof(LineRenderer))]
    [ScriptDependency(typeof(EffectLine))]
    public class SubEffectLineRenderer : EffectComponent<EffectLine> 
    {
        [SerializeField]		float m_startOffset = 0f;
        [SerializeField]		float m_endOffset = 0f;
        [SerializeField] private int m_steps = 2;
        private bool			m_isPlaying;
        private LineRenderer	m_lineRenderer;
        public override bool	isPlaying { get { return m_isPlaying; }}

        void Awake()
        {
            m_lineRenderer = gameObject.GetOrAddComponent<LineRenderer>();
        }

        public override void Play()
        {
            m_isPlaying = true;
        }

        void LateUpdate()
        {
            if (m_lineRenderer.positionCount < m_steps)
            {
                m_lineRenderer.positionCount = m_steps;
            }

            m_lineRenderer.useWorldSpace = true;
            var dir = (owner.toWorldPosition - owner.fromWorldPosition).normalized;
            var from = owner.fromWorldPosition + dir * m_startOffset;
            var to = owner.toWorldPosition - dir * m_endOffset;
            for (int i = 0; i < m_steps; i++)
            {
                m_lineRenderer.SetPosition(i, Vector3.Lerp(from, to, (float)i/(m_steps-1)));	
            }			
        }

        public override void Stop()
        {
            m_isPlaying = false;
        }
    }
}
