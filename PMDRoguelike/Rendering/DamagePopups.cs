using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using System.Collections.Generic;

namespace PMDRoguelike.Rendering
{
    /// <summary>Floating combat text: numbers rise from the hit tile and fade out.</summary>
    public class DamagePopups
    {
        private class Popup
        {
            public string Text;
            public Vector2 Position;
            public float LifeMs;
            public Color Color;
            public float Scale;
        }

        private const float LifetimeMs = 700f;
        private const float RiseSpeed = 26f; // px/s

        private readonly List<Popup> _popups = new();

        public void Spawn(PopupKind kind, string text, Point tile)
        {
            int tileSize = GameConstants.Instance.TileSize;
            (Color color, float scale) = kind switch
            {
                PopupKind.DamageCrit => (new Color(255, 220, 90), 0.95f),
                PopupKind.DamagePlayer => (new Color(255, 110, 110), 0.8f),
                PopupKind.Miss => (new Color(170, 175, 190), 0.7f),
                _ => (Color.White, 0.75f)
            };
            _popups.Add(new Popup
            {
                Text = text,
                Position = new Vector2(tile.X * tileSize + tileSize / 2f, tile.Y * tileSize - 2),
                LifeMs = LifetimeMs,
                Color = color,
                Scale = scale
            });
        }

        public void Update(float deltaMs)
        {
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                Popup p = _popups[i];
                p.LifeMs -= deltaMs;
                p.Position.Y -= RiseSpeed * deltaMs / 1000f;
                if (p.LifeMs <= 0) _popups.RemoveAt(i);
            }
        }

        /// <summary>Draw in world space (inside the camera batch).</summary>
        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (font == null) return;
            foreach (Popup p in _popups)
            {
                float alpha = MathHelper.Clamp(p.LifeMs / (LifetimeMs * 0.6f), 0f, 1f);
                Vector2 size = font.MeasureString(p.Text) * p.Scale;
                TextRenderer.DrawShadowed(spriteBatch, font, p.Text,
                    p.Position - new Vector2(size.X / 2f, 0), p.Color * alpha, p.Scale);
            }
        }
    }
}
