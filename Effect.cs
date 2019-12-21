using System.Collections.Generic;
using Cratesmith.Actors.Pool;
using Cratesmith.Collections.Temp;
using Cratesmith.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cratesmith.Actors.Effects
{
    public class Effect : Actor
    {
        private const float EFFECT_GRACE_TIME = 0.5f;

        private List<IEffectComponent>						m_effectComponents = new List<IEffectComponent>();			// custom parts of this effect
        private ParticleSystem[]							m_particleSystemsEnabledOnStart = new ParticleSystem[0];    // which particle systems are enabled on start? (many effects start with them disabled)
        private static Dictionary<int, HashSet<Effect>>		s_parentLookup = new Dictionary<int, HashSet<Effect>>();
        private static HashSet<Effect>						s_allEffects = new HashSet<Effect>();

        public ParticleSystem[]			particleSystems		{ get; private set; }	
        public AudioSource[]			audioSources		{ get; private set; }
        public Handle<Transform>		parent				{ get; private set; }
        public float					startTime			{ get; private set; }
        public Vector3					localPosition		{ get; protected set; }
        public Quaternion				localRotation		{ get; protected set; }
        public bool						hasParent			{ get; private set; }
        public Rigidbody				rigidBody			{ get; private set; }
        public bool						isStopping			{ get; private set; }
	
        /// <summary>
        /// Spawn an effect from an effect prefab
        /// </summary>
        public static Handle<T> Spawn<T>(T effectPrefab, Transform parent = null) where T : Effect
        {
            var pos = Vector3.zero;
            var rot = Quaternion.identity;
            if (parent != null)
            {
                pos = parent.position;
                rot = parent.rotation;
            }
            return Spawn<T>(effectPrefab, pos, rot, parent);
        }

        /// <summary>
        /// Spawn an effect from an effect prefab, it will follow parent and will automatically call Stop on itself if the parent is destroyed
        /// Note: Position and rotation are in worldSpace
        /// </summary>
        public static Handle<T> Spawn<T>(T effectPrefab, Vector3 position, Transform parent = null) where T : Effect
        {
            return Spawn<T>(effectPrefab, position, parent!=null?parent.rotation:Quaternion.identity, parent);
        }

        /// <summary>
        /// Spawn an effect from a prefab, it will follow parent and will automatically call Stop on itself if the parent is destroyed
        /// Note: Position and rotation are in worldSpace
        /// </summary>
        public static Handle<T> Spawn<T>(T effectPrefab, Vector3 position, Quaternion rotation, Transform parent=null) where T:Effect
        {
            if (effectPrefab == null)
            {
                return null;
            }

            var scene = parent != null ? parent.gameObject.scene : SceneManager.GetActiveScene();
            var effectRef = Pool.Pool.Get(scene).Spawn(effectPrefab, position, rotation);
            var effect = effectRef.value;
            effect.transform.parent = GetEffectRoot(effect.gameObject.scene);
		
            if (parent)
            {
                effect.parent = parent;
                effect.localPosition = parent.InverseTransformPoint(position);
                effect.localRotation = Quaternion.Inverse(parent.rotation) * rotation;
                effect.hasParent = true;
                effect.AddToLookup();

                effect.rigidBody = effect.GetComponent<Rigidbody>();
#if !UNITY_2018_1_OR_NEWER
			if (!effect.rigidBody)
			{
				effect.rigidBody = effect.gameObject.AddComponent<Rigidbody>();
				effect.rigidBody.mass = 0f;
				effect.rigidBody.isKinematic = true;
				effect.rigidBody.useGravity = false;
			}
#endif
            }

            foreach (var i in effect.m_particleSystemsEnabledOnStart)
            {
                if(i!=null)
                {
                    i.Play();
                }
            }

            effect.startTime = Time.time;
		
            return effectRef;
        }

        /// <summary>
        /// Stop this effect. Once it has finished stopping it will automatically despawn
        /// </summary>
        public void Stop()
        {
            if (isStopping) return;
            isStopping = true;

            foreach (var system in particleSystems)
            {
                var emmision = system.emission;
                emmision.enabled = false;
            }			

            foreach (var audioSource in audioSources)
            {
                audioSource.loop = false;
            }

            foreach (var effectComponent in m_effectComponents)
            {
                effectComponent.Stop();
            }
        }

        #region lifecycle
        void Awake()
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
            audioSources = GetComponentsInChildren<AudioSource>();		
            var enabledSystems = new List<ParticleSystem>();
            foreach (var system in particleSystems)
            {
                if (system != null && system.emission.enabled)
                {
                    enabledSystems.Add(system);
                }
            }
            m_particleSystemsEnabledOnStart = enabledSystems.ToArray();
            OnAwake();
        }

        protected virtual void OnAwake() {}

        void OnEnable()
        {
            s_allEffects.Add(this);
        }

        void OnDisable()
        {
            s_allEffects.Remove(this);
        }

        void LateUpdate()
        {
            if (parent)
            {
                var prevPosition = transform.position;
                var newPosition = transform.position = parent.value.TransformPoint(localPosition);
                transform.rotation = parent.value.rotation * localRotation;				

                if (rigidBody)
                {
                    // we just want to match position & rotation, 
                    // but we're using rigidbody velocity to get around a 2017.2 
                    // bug where particle systems can ONLY read velocity from rigidbodies
                    rigidBody.isKinematic = false;
                    rigidBody.useGravity = false;
                    rigidBody.velocity = (newPosition - prevPosition)/Time.deltaTime; 				
                } 			
            }
            else if (hasParent)
            {
                if (rigidBody)
                {
                    rigidBody.isKinematic = true;
                    rigidBody.velocity = Vector3.zero;				
                }

                Stop();
            }

            if (Time.time - startTime < EFFECT_GRACE_TIME && !isStopping)
            {
                return;
            }

            foreach (var system in particleSystems)
            {
                if ((system.emission.enabled && system.main.loop) || system.particleCount > 0)
                {
                    return;
                }
            }

            foreach (var audioSource in audioSources)
            {
                if (audioSource.isPlaying)
                {
                    return;
                }
            }

            foreach (var effectComponent in m_effectComponents)
            {
                if (effectComponent.isPlaying)
                {
                    return;
                }
            }

            Pool.Pool.Despawn(this);
        }

        // pool despawn event
        private void OnDespawn()
        {
            foreach (var audioSource in audioSources)
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                }
            }

            foreach (var i in particleSystems)
            {
                if (i != null)
                {
                    i.Clear();
                    i.Stop();
                }
            }

            foreach (var system in m_particleSystemsEnabledOnStart)
            {
                if (system == null) continue;
                var emission = system.emission;
                emission.enabled = true;
            }

            RemoveFromLookup();

            isStopping = false;
        }

        private void AddToLookup()
        {
            if (!parent.valid)
            {
                return;
            }

            HashSet<Effect> list;
            var id = parent.hashCode;
            if (!s_parentLookup.TryGetValue(id, out list))
            {
                list = s_parentLookup[id] = new HashSet<Effect>();
            }
            list.Add(this);
        }

        private void RemoveFromLookup()
        {
            if (parent.hashCode==0)
            {
                return;
            }

            HashSet<Effect> list;
            var id = parent.hashCode;
            if (!s_parentLookup.TryGetValue(id, out list))
            {
                return;
            }

            list.Remove(this);
            if (list.Count == 0)
            {
                s_parentLookup.Remove(id);
            }
        }
        #endregion

        #region internal
        private static Transform GetEffectRoot(Scene scene)
        {		
            return SceneRootEffect.Get(scene).transform;
        }

        /// <summary>
        /// Add a child effect component to this effect so that it effects lifecycle. (should only be used by EffectComponent)
        /// </summary>
        public void AddEffectComponent(IEffectComponent effectComponent)
        {
            m_effectComponents.Add(effectComponent);
            if (isStopping)
            {
                effectComponent.Stop();
            }
            else
            {
                effectComponent.Play();
            }
        }

        /// <summary>
        /// Remove a child effect component (should only be used by EffectComponent)
        /// </summary>
        public void RemoveEffectComponent(IEffectComponent effectComponent)
        {
            m_effectComponents.Remove(effectComponent);
        }
        #endregion


        public static void StopAll()
        {
            using (var templist = TempList<Effect>.Get(s_allEffects.GetEnumerator(), s_allEffects.Count))
                foreach (var effect in templist)
                {
                    if (effect != null)
                    {
                        Pool.Pool.Despawn(effect);					
                    }
                }
        }

        public static void StopAll(Handle<Transform> parent)
        {
            var id = parent.hashCode;
            HashSet<Effect> list;
            if (s_parentLookup.TryGetValue(id, out list))
            {
                using (var tempList = TempList<Effect>.Get(list.GetEnumerator(), list.Count))
                    foreach (var effect in tempList)
                    {
                        if (effect != null)
                        {
                            effect.Stop();
                        }
                    }
            }
        }
    }
}