using Rootbound.Core;
using UnityEngine;

namespace Rootbound.Visuals
{
    /// <summary>
    /// Numero di danno o testo feedback flottante nel mondo di gioco.
    /// </summary>
    public sealed class DamagePopup : MonoBehaviour
    {
        private TextMesh _textMesh;
        private float _lifetime = 0.6f;
        private float _maxLife = 0.6f;
        private Color _baseColor;

        public static void Create(Vector2 worldPos, string text, Color color, float scale = 1f)
        {
            var go = new GameObject("DamagePopup");
            go.transform.position = new Vector3(worldPos.x + Random.Range(-0.2f, 0.2f), worldPos.y + 0.5f, 0f);
            go.transform.localScale = Vector3.one * (0.08f * scale);

            var popup = go.AddComponent<DamagePopup>();
            popup._baseColor = color;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 36;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;
            popup._textMesh = tm;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = RootboundLayers.SortVfx;
                mr.sortingOrder = 25;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _lifetime -= dt;
            if (_lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * (1.5f * dt);
            if (_textMesh != null)
            {
                Color c = _baseColor;
                c.a = Mathf.Clamp01(_lifetime / _maxLife);
                _textMesh.color = c;
            }
        }
    }
}
