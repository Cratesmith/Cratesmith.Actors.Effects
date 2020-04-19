using Cratesmith.Utils;
using UnityEngine;

namespace Cratesmith.Actors.Effects
{
    public interface IEffectComponent
    {
        // is this sub effect still going? 
        bool isPlaying { get; }

        // play this effect
        void Play();

        // stop this effect
        void Stop();

        // quick lookup for parent
        Transform parent { get; }
    }

    public abstract class EffectComponent : SubComponent<Effect>, IEffectComponent
    {
        // is this sub effect still going? 
        public abstract bool isPlaying { get; }

        // play this effect
        public abstract void Play();

        // stop this effect
        public abstract void Stop();

        // quick lookup for parent
        public Transform parent {get { return owner.parent; } }

        protected virtual void OnEnable()
        {
            owner.AddEffectComponent(this);
        }

        protected virtual void OnDisable()
        {
            owner.RemoveEffectComponent(this);
        }
    }

    public abstract class EffectComponent<T> : SubComponent<T>, IEffectComponent where T:Effect
    {
        // is this sub effect still going? 
        public abstract bool isPlaying { get; }

        // play this effect
        public abstract void Play();

        // stop this effect
        public abstract void Stop();

        // quick lookup for parent
        public Transform parent {get { return owner.parent; } }

        protected virtual void OnEnable()
        {
            owner.AddEffectComponent(this);
        }

        protected virtual void OnDisable()
        {
            if(owner) owner.RemoveEffectComponent(this);
        }
    }
}