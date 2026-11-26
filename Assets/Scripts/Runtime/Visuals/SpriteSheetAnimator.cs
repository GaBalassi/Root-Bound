using UnityEngine;

namespace Rootbound.Visuals
{
    /// <summary>
    /// Riproduttore leggero di sequenze di sprite pixel art.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteSheetAnimator : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private float _fps = 8f;
        private float _timer;
        private int _index;
        private bool _loop = true;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void PlayLoop(Sprite[] frames, float fps = 8f)
        {
            if (frames == null || frames.Length == 0) return;
            if (_frames == frames && _loop)
            {
                _fps = fps;
                return;
            }

            _frames = frames;
            _fps = Mathf.Max(0.1f, fps);
            _loop = true;
            _index = 0;
            _timer = 0f;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = _frames[0];
        }

        public void PlaySingle(Sprite sprite)
        {
            if (sprite == null) return;
            _frames = null;
            _loop = false;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
        }

        private void Update()
        {
            if (_frames == null || _frames.Length <= 1) return;

            _timer += Time.deltaTime * _fps;
            if (_timer >= 1f)
            {
                int advance = Mathf.FloorToInt(_timer);
                _timer -= advance;
                if (_loop)
                {
                    _index = (_index + advance) % _frames.Length;
                }
                else
                {
                    _index = Mathf.Min(_frames.Length - 1, _index + advance);
                }

                if (_renderer != null)
                {
                    _renderer.sprite = _frames[_index];
                }
            }
        }
    }
}
