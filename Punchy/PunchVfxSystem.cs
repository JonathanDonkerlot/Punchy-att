using System;
using Punchy.Core;
using UnityEngine;

namespace Punchy
{
    /// <summary>
    /// Pooled, zero-allocation procedural 3-layer kinetic VFX system (shockwave ring + stretched sparks + soft dust puff)
    /// for bare-handed punch impacts on non-headless servers/listen-hosts.
    /// </summary>
    public static class PunchVfxSystem
    {
        private const int PoolSize = 8;
        private const int TextureSize = 64;

        private sealed class PunchVfxRig
        {
            public GameObject Root;
            public ParticleSystem RingSystem;
            public ParticleSystem SparksSystem;
            public ParticleSystem DustSystem;
        }

        private static GameObject _poolRoot;
        private static PunchVfxRig[] _pool;
        private static int _nextIndex;

        private static Texture2D _softGlowTexture;
        private static Texture2D _ringTexture;
        private static Material _additiveSparkMaterial;
        private static Material _ringMaterial;
        private static Material _dustMaterial;

        public static void PlayImpactVfx(
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 punchDirection,
            in PunchVfxParams vfx)
        {
            if (ApplicationManager.IsHeadless || !vfx.ShouldPlay)
                return;

            try
            {
                EnsureInitialized();
                if (_pool == null || _pool.Length == 0)
                    return;

                PunchVfxRig rig = _pool[_nextIndex];
                _nextIndex = (_nextIndex + 1) % _pool.Length;

                if (rig == null || rig.Root == null)
                    return;

                Vector3 orientForward = hitNormal.sqrMagnitude > 1e-4f
                    ? hitNormal.normalized
                    : (punchDirection.sqrMagnitude > 1e-4f ? -punchDirection.normalized : Vector3.up);

                rig.Root.transform.position = hitPoint;
                rig.Root.transform.rotation = Quaternion.LookRotation(orientForward);

                float s = Mathf.Max(0.1f, vfx.Scale);

                if (vfx.ShowShockwaveRing && rig.RingSystem != null)
                {
                    ParticleSystem.MainModule ringMain = rig.RingSystem.main;
                    ringMain.startSize = 0.42f * s;
                    ringMain.startColor = vfx.IsHeavyImpact
                        ? new Color(1.0f, 0.85f, 0.35f, 0.95f)
                        : new Color(1.0f, 0.96f, 0.82f, 0.80f);
                    rig.RingSystem.Emit(1);
                }

                if (vfx.SparkCount > 0 && rig.SparksSystem != null)
                {
                    ParticleSystem.MainModule sparkMain = rig.SparksSystem.main;
                    sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(2.2f * s, 5.5f * s);
                    sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.032f * s, 0.068f * s);
                    sparkMain.startColor = vfx.IsHeavyImpact
                        ? new ParticleSystem.MinMaxGradient(
                            new Color(1.0f, 0.92f, 0.55f, 1.0f),
                            new Color(1.0f, 0.58f, 0.16f, 1.0f))
                        : new ParticleSystem.MinMaxGradient(
                            new Color(1.0f, 0.97f, 0.80f, 0.95f),
                            new Color(1.0f, 0.78f, 0.38f, 0.90f));
                    rig.SparksSystem.Emit(vfx.SparkCount);
                }

                if (vfx.DustCount > 0 && rig.DustSystem != null)
                {
                    ParticleSystem.MainModule dustMain = rig.DustSystem.main;
                    dustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * s, 1.8f * s);
                    dustMain.startSize = new ParticleSystem.MinMaxCurve(0.10f * s, 0.20f * s);
                    dustMain.startColor = new Color(0.88f, 0.85f, 0.80f, vfx.IsHeavyImpact ? 0.55f : 0.42f);
                    rig.DustSystem.Emit(vfx.DustCount);
                }
            }
            catch
            {
                // VFX is strictly cosmetic and must never disrupt combat hit processing.
            }
        }

        public static void Cleanup()
        {
            if (_poolRoot != null)
            {
                UnityEngine.Object.Destroy(_poolRoot);
                _poolRoot = null;
            }

            _pool = null;
            _nextIndex = 0;

            if (_additiveSparkMaterial != null)
            {
                UnityEngine.Object.Destroy(_additiveSparkMaterial);
                _additiveSparkMaterial = null;
            }

            if (_ringMaterial != null)
            {
                UnityEngine.Object.Destroy(_ringMaterial);
                _ringMaterial = null;
            }

            if (_dustMaterial != null)
            {
                UnityEngine.Object.Destroy(_dustMaterial);
                _dustMaterial = null;
            }

            if (_softGlowTexture != null)
            {
                UnityEngine.Object.Destroy(_softGlowTexture);
                _softGlowTexture = null;
            }

            if (_ringTexture != null)
            {
                UnityEngine.Object.Destroy(_ringTexture);
                _ringTexture = null;
            }
        }

        private static void EnsureInitialized()
        {
            if (_poolRoot != null && _pool != null)
                return;

            EnsureAssets();

            _poolRoot = new GameObject("[Punchy] VFX Pool");
            _poolRoot.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(_poolRoot);

            _pool = new PunchVfxRig[PoolSize];
            for (int i = 0; i < PoolSize; i++)
                _pool[i] = CreateRig(i);

            _nextIndex = 0;
        }

        private static void EnsureAssets()
        {
            if (_softGlowTexture == null)
                _softGlowTexture = CreateSoftGlowTexture();

            if (_ringTexture == null)
                _ringTexture = CreateRingTexture();

            if (_additiveSparkMaterial == null)
                _additiveSparkMaterial = CreateParticleMaterial(_softGlowTexture, isAdditive: true);

            if (_ringMaterial == null)
                _ringMaterial = CreateParticleMaterial(_ringTexture, isAdditive: true);

            if (_dustMaterial == null)
                _dustMaterial = CreateParticleMaterial(_softGlowTexture, isAdditive: false);
        }

        private static PunchVfxRig CreateRig(int index)
        {
            var rigRoot = new GameObject($"PunchVfxRig_{index}");
            rigRoot.transform.SetParent(_poolRoot.transform, false);

            var rig = new PunchVfxRig
            {
                Root = rigRoot,
                RingSystem = CreateRingSystem(rigRoot.transform),
                SparksSystem = CreateSparksSystem(rigRoot.transform),
                DustSystem = CreateDustSystem(rigRoot.transform)
            };

            return rig;
        }

        private static ParticleSystem CreateRingSystem(Transform parent)
        {
            var go = new GameObject("RingAndFlash");
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = 0.16f;
            main.startSpeed = 0f;
            main.startSize = 0.40f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 4;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;

            ParticleSystem.SizeOverLifetimeModule sizeMod = ps.sizeOverLifetime;
            sizeMod.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.25f),
                new Keyframe(1f, 1.0f));
            sizeMod.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystem.ColorOverLifetimeModule colMod = ps.colorOverLifetime;
            colMod.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.0f, 0.96f, 0.85f), 0f),
                    new GradientColorKey(new Color(1.0f, 0.72f, 0.28f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.90f, 0f),
                    new GradientAlphaKey(0.0f, 1f)
                });
            colMod.color = new ParticleSystem.MinMaxGradient(grad);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = _ringMaterial;

            return ps;
        }

        private static ParticleSystem CreateSparksSystem(Transform parent)
        {
            var go = new GameObject("KineticSparks");
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.30f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.065f);
            main.gravityModifier = 0.45f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.025f;

            ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.dampen = 0.25f;
            drag.limit = 1.8f;

            ParticleSystem.ColorOverLifetimeModule colMod = ps.colorOverLifetime;
            colMod.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.0f, 0.96f, 0.78f), 0f),
                    new GradientColorKey(new Color(1.0f, 0.55f, 0.14f), 0.65f),
                    new GradientColorKey(new Color(0.75f, 0.25f, 0.05f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1.0f, 0f),
                    new GradientAlphaKey(0.85f, 0.5f),
                    new GradientAlphaKey(0.0f, 1f)
                });
            colMod.color = new ParticleSystem.MinMaxGradient(grad);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.4f;
            renderer.velocityScale = 0.08f;
            renderer.sharedMaterial = _additiveSparkMaterial;

            return ps;
        }

        private static ParticleSystem CreateDustSystem(Transform parent)
        {
            var go = new GameObject("ImpactDust");
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 32;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.04f;

            ParticleSystem.SizeOverLifetimeModule sizeMod = ps.sizeOverLifetime;
            sizeMod.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(1f, 1.35f));
            sizeMod.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.dampen = 0.35f;
            drag.limit = 0.7f;

            ParticleSystem.ColorOverLifetimeModule colMod = ps.colorOverLifetime;
            colMod.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.95f, 0.92f, 0.86f), 0f),
                    new GradientColorKey(new Color(0.75f, 0.72f, 0.68f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.65f, 0f),
                    new GradientAlphaKey(0.0f, 1f)
                });
            colMod.color = new ParticleSystem.MinMaxGradient(grad);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = _dustMaterial;

            return ps;
        }

        private static Material CreateParticleMaterial(Texture2D texture, bool isAdditive)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null && isAdditive)
                shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null && !isAdditive)
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");

            var mat = new Material(shader)
            {
                mainTexture = texture,
                hideFlags = HideFlags.HideAndDontSave
            };

            if (shader != null && shader.name == "Particles/Standard Unlit")
            {
                mat.SetFloat("_Mode", isAdditive ? 3f : 2f);
                if (isAdditive)
                {
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                }
                else
                {
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }

                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }

            return mat;
        }

        private static Texture2D CreateSoftGlowTexture()
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[TextureSize * TextureSize];
            float center = (TextureSize - 1) * 0.5f;
            float invRadius = 1f / center;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float dx = (x - center) * invRadius;
                    float dy = (y - center) * invRadius;
                    float r2 = (dx * dx) + (dy * dy);
                    float alpha = r2 >= 1f ? 0f : (1f - r2) * (1f - r2);
                    pixels[(y * TextureSize) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D CreateRingTexture()
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[TextureSize * TextureSize];
            float center = (TextureSize - 1) * 0.5f;
            float invRadius = 1f / center;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float dx = (x - center) * invRadius;
                    float dy = (y - center) * invRadius;
                    float r = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float distFromRing = Mathf.Abs(r - 0.72f) / 0.22f;
                    float alpha = distFromRing >= 1f ? 0f : Mathf.Pow(1f - (distFromRing * distFromRing), 2f);
                    pixels[(y * TextureSize) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
