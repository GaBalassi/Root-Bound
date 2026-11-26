using Rootbound.Core;
using UnityEngine;

namespace Rootbound.Visuals
{
    internal sealed class TransientVfx : MonoBehaviour
    {
        public Vector3 velocity;
        public float gravity;
        public float lifetime;
        public float maxLife;
        public bool shrink;
        public bool rotateSpin;

        private SpriteRenderer _sr;
        private Vector3 _startScale;
        private Color _startColor;

        public void Begin(SpriteRenderer sr, Vector3 vel, float grav, float life, bool doShrink, bool spin)
        {
            _sr = sr;
            velocity = vel;
            gravity = grav;
            lifetime = life;
            maxLife = Mathf.Max(0.01f, life);
            shrink = doShrink;
            rotateSpin = spin;
            _startScale = transform.localScale;
            _startColor = sr.color;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            lifetime -= dt;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            velocity.y -= gravity * dt;
            transform.position += velocity * dt;
            if (rotateSpin)
            {
                transform.Rotate(0f, 0f, 240f * dt);
            }

            float t = Mathf.Clamp01(lifetime / maxLife);
            if (_sr != null)
            {
                Color c = _startColor;
                c.a = _startColor.a * t;
                _sr.color = c;
            }

            if (shrink)
            {
                transform.localScale = _startScale * (0.4f + 0.6f * t);
            }
        }
    }

    /// <summary>
    /// Fabbrica leggera per effetti 2D (polvere, foglie, afterimage, archi fendenti, pop-up danni).
    /// </summary>
    public static class VfxFactory
    {
        private static RootboundArtLibrary _art;
        private static Material _unlitMat;
        private static Sprite _fallbackDot;

        public static void SetArtLibrary(RootboundArtLibrary art)
        {
            _art = art;
        }

        private static Material GetUnlitMaterial()
        {
            if (_unlitMat == null)
            {
                Shader sh = Shader.Find("Sprites/Default");
                if (sh != null) _unlitMat = new Material(sh);
            }
            return _unlitMat;
        }

        private static Sprite GetCircleSprite()
        {
            if (_art != null && _art.particleCircle != null) return _art.particleCircle;
            if (_fallbackDot == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var cols = new Color[16];
                for (int i = 0; i < 16; i++) cols[i] = Color.white;
                tex.SetPixels(cols);
                tex.Apply();
                _fallbackDot = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);
            }
            return _fallbackDot;
        }

        public static void SpawnDustBurst(Vector2 position, Color color, int count = 6)
        {
            if (!Application.isPlaying) return;
            Sprite sprite = GetCircleSprite();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("VfxDust");
                go.transform.position = position + Random.insideUnitCircle * 0.2f;
                float sc = Random.Range(0.55f, 1.05f);
                go.transform.localScale = new Vector3(sc, sc, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                sr.sortingLayerName = RootboundLayers.SortVfx;
                sr.sortingOrder = 10;
                sr.material = GetUnlitMaterial();

                Vector3 vel = new Vector3(Random.Range(-2.6f, 2.6f), Random.Range(1.2f, 4.2f), 0f);
                var tv = go.AddComponent<TransientVfx>();
                tv.Begin(sr, vel, 7.5f, Random.Range(0.22f, 0.4f), true, false);
            }
        }

        public static void SpawnLeafBurst(Vector2 position, int count = 6)
        {
            if (!Application.isPlaying) return;
            Sprite sprite = (_art != null && _art.particleLeaf != null) ? _art.particleLeaf : GetCircleSprite();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("VfxLeaf");
                go.transform.position = position;
                float sc = Random.Range(0.8f, 1.25f);
                go.transform.localScale = new Vector3(sc, sc, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(0.5f, 1f, 0.65f, 0.95f);
                sr.sortingLayerName = RootboundLayers.SortVfx;
                sr.sortingOrder = 11;
                sr.material = GetUnlitMaterial();

                Vector3 vel = new Vector3(Random.Range(-3.2f, 3.2f), Random.Range(1.5f, 4.5f), 0f);
                var tv = go.AddComponent<TransientVfx>();
                tv.Begin(sr, vel, 4.5f, Random.Range(0.32f, 0.52f), false, true);
            }
        }

        public static void SpawnAfterimage(SpriteRenderer source, Color tint)
        {
            if (!Application.isPlaying || source == null || source.sprite == null) return;
            var go = new GameObject("DashAfterimage");
            go.transform.position = source.transform.position;
            go.transform.localScale = source.transform.lossyScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.flipX = source.flipX;
            sr.color = tint;
            sr.sortingLayerName = RootboundLayers.SortVfx;
            sr.sortingOrder = 2;
            sr.material = GetUnlitMaterial();

            var tv = go.AddComponent<TransientVfx>();
            tv.Begin(sr, Vector3.zero, 0f, 0.18f, false, false);
        }

        public static void SpawnSlashArc(Vector2 position, int facing, int comboIndex, WeaponType weapon, bool charged)
        {
            if (!Application.isPlaying || _art == null) return;

            Sprite arcSprite = null;
            Color col = Color.white;
            float scale = charged ? 1.45f : 1.05f;

            switch (weapon)
            {
                case WeaponType.MartelloDiCorteccia:
                    arcSprite = _art.hammerSmash;
                    col = charged ? new Color(1f, 0.85f, 0.32f, 0.95f) : new Color(1f, 0.72f, 0.35f, 0.9f);
                    scale *= 1.15f;
                    break;
                case WeaponType.FrustaDiLiana:
                    arcSprite = _art.whipArc;
                    col = charged ? new Color(0.55f, 1f, 0.72f, 0.95f) : new Color(0.38f, 0.96f, 0.6f, 0.9f);
                    scale *= 1.2f;
                    break;
                default:
                    arcSprite = _art.slashArcs != null && _art.slashArcs.Length > 0
                        ? _art.slashArcs[Mathf.Clamp(comboIndex, 0, _art.slashArcs.Length - 1)]
                        : GetCircleSprite();
                    col = charged ? new Color(1f, 0.9f, 0.4f, 0.95f) : new Color(0.65f, 1f, 0.82f, 0.92f);
                    break;
            }

            var go = new GameObject("SlashVfx");
            go.transform.position = position;
            go.transform.localScale = new Vector3(facing * scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = arcSprite;
            sr.color = col;
            sr.sortingLayerName = RootboundLayers.SortVfx;
            sr.sortingOrder = 14;
            sr.material = GetUnlitMaterial();

            var tv = go.AddComponent<TransientVfx>();
            tv.Begin(sr, new Vector3(facing * 1.4f, 0f, 0f), 0f, 0.14f, false, false);
        }

        public static void SpawnDamageText(Vector2 worldPos, string text, Color color, float scale = 1f)
        {
            if (!Application.isPlaying) return;
            DamagePopup.Create(worldPos, text, color, scale);
        }
    }
}
