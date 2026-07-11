using System;

namespace DigitalHuman.Core
{
    /// <summary>与后端 emotion 字段取值严格一致。</summary>
    public enum Emotion
    {
        Unknown,
        Neutral,
        Happy,
        Sad,
        Anxious,
        Angry,
        Tired,
    }

    /// <summary>与后端 expression 字段取值严格一致。</summary>
    public enum Expression
    {
        Unknown,
        Neutral,
        Smile,
        Concerned,
        Worried,
        Calm,
        Gentle,
    }

    /// <summary>与后端 action 字段取值严格一致。命名为 AvatarAction 以避免与 System.Action 冲突。</summary>
    public enum AvatarAction
    {
        Unknown,
        Idle,
        Wave,
        Comfort,
        Encourage,
        CalmDown,
    }

    public static class EnumMapping
    {
        public static Emotion ParseEmotion(string s)
        {
            if (string.IsNullOrEmpty(s)) return Emotion.Unknown;
            return (Emotion)Enum.Parse(typeof(Emotion), Capitalize(s), true);
        }

        public static Expression ParseExpression(string s)
        {
            if (string.IsNullOrEmpty(s)) return Expression.Unknown;
            return (Expression)Enum.Parse(typeof(Expression), Capitalize(s), true);
        }

        public static AvatarAction ParseAvatarAction(string s)
        {
            if (string.IsNullOrEmpty(s)) return AvatarAction.Unknown;
            return (AvatarAction)Enum.Parse(typeof(AvatarAction), (s ?? "").Replace("_", ""), true);
        }

        public static string ToWireString(this Emotion e) => e == Emotion.Unknown ? "neutral" : e.ToString().ToLowerInvariant();
        public static string ToWireString(this Expression e) => e == Expression.Unknown ? "neutral" : e.ToString().ToLowerInvariant();
        public static string ToWireString(this AvatarAction a) => a == AvatarAction.Unknown ? "idle" : a.ToString().ToLowerInvariant();

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length == 1) return s.ToUpperInvariant();
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
