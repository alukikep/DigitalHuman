using DigitalHuman.Core;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.Motion;
using UnityEngine;
using Logger = DigitalHuman.Core.Logger;

namespace DigitalHuman.Live2D
{
    /// <summary>
    /// 把后端 emotion/expression/action 映射到 Live2D 模型。
    /// 默认 mapping 是骨架版本，后续动画师手动调 emotionClips[] / expressionIndices[] / idleStateName。
    /// </summary>
    [AddComponentMenu("DigitalHuman/Hiyori Driver")]
    public class HiyoriDriver : MonoBehaviour
    {
        [Header("Live2D refs")]
        public Animator animator;
        public CubismMotionController motionController;
        public CubismExpressionController expressionController;

        [Header("Motion clips (assigned by animator later)")]
        public AnimationClip[] emotionClips = new AnimationClip[6];
        public string emotionStatePrefix = "M_";

        [Header("Expression indices (Cubism Expression List)")]
        public int[] expressionIndices = new int[] { 0, 1, 2, 3, 4, 5 };

        [Header("Idle")]
        public string idleStateName = "M_Idle";

        private int _lastEmotionIndex = -1;

        public void Inject(Animator anim, CubismMotionController motion, CubismExpressionController expr)
        { animator = anim; motionController = motion; expressionController = expr; }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (motionController == null) motionController = GetComponentInChildren<CubismMotionController>(true);
            if (expressionController == null) expressionController = GetComponentInChildren<CubismExpressionController>(true);
        }

        public void ApplyEmotion(EmotionEventData ev)
        {
            var emo = EnumMapping.ParseEmotion(ev?.emotion);
            if (emo == Emotion.Unknown || emo == Emotion.Neutral) emo = Emotion.Neutral;
            int idx = (int)emo - 1;
            if (idx < 0) idx = 0;
            ApplyExpression(EnumMapping.ParseExpression(ev?.expression));
            ApplyAction(EnumMapping.ParseAvatarAction(ev?.action), idx);
            _lastEmotionIndex = idx;
        }

        public void ApplyExpression(Expression expr)
        {
            if (expressionController == null) return;
            int target = -1;
            switch (expr)
            {
                case Expression.Neutral: target = SafeGet(0); break;
                case Expression.Smile: target = SafeGet(1); break;
                case Expression.Concerned: target = SafeGet(2); break;
                case Expression.Worried: target = SafeGet(3); break;
                case Expression.Calm: target = SafeGet(4); break;
                case Expression.Gentle: target = SafeGet(5); break;
                default: target = -1; break;
            }
            if (target < 0) target = SafeGet(0);
            try { expressionController.CurrentExpressionIndex = target; }
            catch (System.Exception e) { Logger.Warn("live2d", $"set expression: {e.Message}"); }
        }

        public void ApplyAction(AvatarAction act, int lastEmotionIndex)
        {
            AnimationClip clip = null;
            if (lastEmotionIndex >= 0 && lastEmotionIndex < emotionClips.Length) clip = emotionClips[lastEmotionIndex];

            string stateName = emotionStatePrefix + (lastEmotionIndex >= 0 ? EmotionFromIndex(lastEmotionIndex).ToString() : "Idle");
            bool played = false;
            if (animator != null)
            {
                animator.Play(stateName, 0, 0f);
                played = true;
            }
            if (!played && motionController != null && clip != null)
            {
                try { motionController.PlayAnimation(clip, 0, CubismMotionPriority.PriorityNormal, false, 1f); played = true; }
                catch (System.Exception e) { Logger.Warn("live2d", $"motion ctrl: {e.Message}"); }
            }
            if (!played && animator != null)
            {
                animator.Play(idleStateName, 0, 0f);
            }
        }

        public void ResetToIdle()
        {
            ApplyExpression(Expression.Neutral);
            if (animator != null) animator.Play(idleStateName, 0, 0f);
        }

        private int SafeGet(int i) => (i < 0 || i >= expressionIndices.Length) ? 0 : expressionIndices[i];

        private static Emotion EmotionFromIndex(int idx)
        {
            switch (idx)
            {
                case 0: return Emotion.Neutral;
                case 1: return Emotion.Happy;
                case 2: return Emotion.Sad;
                case 3: return Emotion.Anxious;
                case 4: return Emotion.Angry;
                case 5: return Emotion.Tired;
                default: return Emotion.Neutral;
            }
        }
    }
}
