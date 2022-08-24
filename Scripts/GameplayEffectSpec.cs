﻿using UnityEngine;
using System.Linq;
using UnityEngine.Events;

namespace KimScor.GameplayTagSystem.Effect
{
    [System.Serializable]
    public abstract class GameplayEffectSpec
    {
        #region Events
        public delegate void GameplayEffectSpecState(GameplayEffectSpec gameplayEffectSpec);
        #endregion
        private GameplayEffect _GameplayEffect;
        private GameplayEffectSystem _Owner;
        protected float _ElapsedTime;
        private object _Data;
        private bool _Activate = false;
        private bool _Apply = false;

        public GameplayEffect GameplayEffect => _GameplayEffect;
        public GameplayEffectSystem Owner => _Owner;
        public GameplayTagSystem GameplayTagSystem => Owner.GameplayTagSystem;
        public float Duration => _GameplayEffect.Duration;
        public float ElapsedTime => _ElapsedTime;
        public object Data => _Data;
        public bool Activate => _Activate;
        public bool Apply => _Apply;

        public FGameplayEffectTags EffectTags => GameplayEffect.EffectTags;

        public bool IsInstant => GameplayEffect.DurationPolicy.Equals(EDurationPolicy.Instant);
        public bool IsDuration => GameplayEffect.DurationPolicy.Equals(EDurationPolicy.Duration);
        public bool IsInfinite => GameplayEffect.DurationPolicy.Equals(EDurationPolicy.Infinite);


        public event GameplayEffectSpecState OnFinishedGameplayEffect;
        public GameplayEffectSpec(GameplayEffect effect, GameplayEffectSystem owner)
        {
            _GameplayEffect = effect;
            _Owner = owner;
        }
        public void SetData(object data)
        {
            _Data = data;
        }

        public bool TryGameplayEffect()
        {
            if (CanActivateGameplayEffect())
            {
                OnGameplayEffect();

                return true;
            }

            return false;
        }
        public void OnGameplayEffect()
        {
            if (Activate)
            {
                return;
            }

            if (GameplayEffect.DebugMode)
                Debug.Log("Enter Effect : " + GameplayEffect.name);


            _Activate = true;
            _ElapsedTime = 0f;

            Owner.RemoveGameplayEffectWithTags(GameplayEffect.EffectTags.RemoveGameplayEffectsWithTags);

            Owner.AddGameplayEffectList(this);

            Owner.GameplayTagSystem.AddOwnedTags(GameplayEffect.EffectTags.ActivateGrantedTags);

            if (GameplayEffect.EffectTags.ApplyEffectRequiredTags.Length > 0 || GameplayEffect.EffectTags.ApplyEffectIgnoreTags.Length > 0)
            {
                Owner.GameplayTagSystem.OnNewAddOwnedTag += GameplayTagSystem_OnUpdateOwnedTag;
                Owner.GameplayTagSystem.OnRemoveOwnedTag += GameplayTagSystem_OnUpdateOwnedTag;
            }

            EnterEffect();

            if (!Activate)
                return;
                
            _Apply = CanApplyGameplayEffect();

            if (Apply)
                OnApplyEffect();


            if (IsInstant)
            {
                EndGameplayEffect();
            }
        }
        public void OnUpdateEffect(float deltaTime)
        {
            if (!_Apply)
                return;

            if (GameplayEffect.DurationPolicy.Equals(EDurationPolicy.Duration))
            {
                _ElapsedTime += deltaTime;

                if (_ElapsedTime >= Duration)
                {
                    EndGameplayEffect();

                    return;
                }
            }

            UpdateEffect(deltaTime);
        }
        public virtual void OnFixedUpdateEffect(float deltaTime) 
        {
            if (!_Apply)
                return;
        }


        private void GameplayTagSystem_OnUpdateOwnedTag(GameplayTagSystem gameplayTagSystem, GameplayTag changedTag)
        {
            if (EffectTags.ApplyEffectIgnoreTags.Contains(changedTag) 
                || EffectTags.ApplyEffectRequiredTags.Contains(changedTag))
            {
                TryApplyEffect();
            }
        }

        public void EndGameplayEffect()
        {
            if (!Activate)
            {
                return;
            }
            _Activate = false;

            if (Apply)
                OnIgnoreEffect();

            if (GameplayEffect.DebugMode)
                Debug.Log("Exit Effect : " + GameplayEffect.name);


            if (GameplayEffect.EffectTags.ApplyEffectRequiredTags.Length > 0 || GameplayEffect.EffectTags.ApplyEffectIgnoreTags.Length > 0)
            {
                Owner.GameplayTagSystem.OnNewAddOwnedTag -= GameplayTagSystem_OnUpdateOwnedTag;
                Owner.GameplayTagSystem.OnRemoveOwnedTag -= GameplayTagSystem_OnUpdateOwnedTag;
            }

            Owner.GameplayTagSystem.RemoveOwnedTags(GameplayEffect.EffectTags.ActivateGrantedTags);

            ExitEffect();

            Owner.RemoveGameplayEffectList(this);

            OnFinishedGameplayEffect?.Invoke(this);
        }
        /// <summary>
        /// 이펙트 발동 시작시 효과
        /// </summary>
        protected abstract void EnterEffect();

        /// <summary>
        /// 이펙트 발동 종료시 효과
        /// </summary>
        protected abstract void ExitEffect();

        /// <summary>
        /// 지속, 영속 효과의 매 틱 효과
        /// </summary>
        protected virtual void UpdateEffect(float deltaTime) { }



        /// <summary>
        /// 이펙트 적용 시작시 효과
        /// </summary>
        protected abstract void ApplyEffect();

        /// <summary>
        /// 이펙트 적용 무시시 효과
        /// </summary>
        protected abstract void IgnoreEffect();


        protected void TryApplyEffect()
        {
            bool canApplyEffect = CanApplyGameplayEffect();

            if (Apply != canApplyEffect)
            {
                _Apply = canApplyEffect;

                if (Apply)
                {
                    OnApplyEffect();
                }
                else
                {
                    OnIgnoreEffect();
                }
            }
        }
        protected void OnApplyEffect()
        {
            if (!Activate)
                return;

            if (GameplayEffect.DebugMode)
                Debug.Log("Apply Effect : " + GameplayEffect.name);

            GameplayTagSystem.AddOwnedTags(EffectTags.ApplyGrantedTags);

            ApplyEffect();
        }
        protected void OnIgnoreEffect()
        {
            if (GameplayEffect.DebugMode)
                Debug.Log("Ignore Effect : " + GameplayEffect.name);

            GameplayTagSystem.RemoveOwnedTags(EffectTags.ApplyGrantedTags);

            IgnoreEffect();
        }

        public virtual bool CanActivateGameplayEffect()
        {
            // 필요한 태그가 있으나 소유하고 있지 않다.
            if (GameplayEffect.EffectTags.ActivateEffectRequiredTags is not null
                && !Owner.GameplayTagSystem.ContainAllTagsInOwned(GameplayEffect.EffectTags.ActivateEffectRequiredTags))
                return false;

            // 방해 태그가 있고 방해 태그를 소유하고 있다.
            if (GameplayEffect.EffectTags.ActivateEffectIgnoreTags is not null
               && Owner.GameplayTagSystem.ContainOnceTagsInOwned(GameplayEffect.EffectTags.ActivateEffectIgnoreTags))
                return false;

            return true;
        }

        protected virtual bool CanApplyGameplayEffect()
        {
            // 필요한 태그가 있으나 소유하고 있지 않다.
            if (GameplayEffect.EffectTags.ApplyEffectRequiredTags is not null
                && !Owner.GameplayTagSystem.ContainAllTagsInOwned(GameplayEffect.EffectTags.ApplyEffectRequiredTags))
                return false;

            // 방해 태그가 있고 방해 태그를 소유하고 있다.
            if (GameplayEffect.EffectTags.ApplyEffectIgnoreTags is not null
               && Owner.GameplayTagSystem.ContainOnceTagsInOwned(GameplayEffect.EffectTags.ApplyEffectIgnoreTags))
                return false;

            return true;
        }
    }
}
