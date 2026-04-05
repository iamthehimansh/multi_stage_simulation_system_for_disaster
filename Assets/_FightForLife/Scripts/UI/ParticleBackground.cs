using UnityEngine;

namespace FightForLife.UI
{
    public class ParticleBackground : MonoBehaviour
    {
        [Header("Rain Simulation")]
        [SerializeField] private int rainDropCount = 200;
        [SerializeField] private float spawnWidth = 20f;
        [SerializeField] private float spawnHeight = 12f;
        [SerializeField] private float fallSpeed = 5f;
        [SerializeField] private float fallSpeedVariation = 2f;
        [SerializeField] private Color rainColor = new Color(0.6f, 0.75f, 0.9f, 0.3f);

        [Header("Lightning")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private float lightningInterval = 8f;
        [SerializeField] private float lightningVariation = 5f;
        [SerializeField] private float lightningDuration = 0.15f;
        [SerializeField] private float lightningIntensity = 3f;

        private float nextLightning;
        private float lightningTimer;
        private bool isLightning;
        private float baseLightIntensity;

        private struct RainDrop
        {
            public GameObject obj;
            public float speed;
        }

        private RainDrop[] rainDrops;

        private void Start()
        {
            if (directionalLight != null)
            {
                baseLightIntensity = directionalLight.intensity;
            }

            nextLightning = Time.time + lightningInterval + Random.Range(0f, lightningVariation);

            CreateRainDrops();
        }

        private void CreateRainDrops()
        {
            rainDrops = new RainDrop[rainDropCount];

            for (int i = 0; i < rainDropCount; i++)
            {
                GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                drop.name = "RainDrop";
                drop.transform.SetParent(transform);
                drop.transform.localScale = new Vector3(0.03f, 0.3f, 1f);
                drop.transform.position = new Vector3(
                    Random.Range(-spawnWidth, spawnWidth),
                    Random.Range(-spawnHeight, spawnHeight * 2f),
                    10f
                );

                // Remove collider
                var collider = drop.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                // Set material
                var renderer = drop.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    mat.color = rainColor;
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                    renderer.material = mat;
                }

                rainDrops[i] = new RainDrop
                {
                    obj = drop,
                    speed = fallSpeed + Random.Range(-fallSpeedVariation, fallSpeedVariation)
                };
            }
        }

        private void Update()
        {
            // Animate rain
            if (rainDrops != null)
            {
                for (int i = 0; i < rainDrops.Length; i++)
                {
                    if (rainDrops[i].obj == null) continue;

                    var pos = rainDrops[i].obj.transform.position;
                    pos.y -= rainDrops[i].speed * Time.deltaTime;
                    pos.x -= rainDrops[i].speed * 0.3f * Time.deltaTime; // Wind angle

                    if (pos.y < -spawnHeight)
                    {
                        pos.y = spawnHeight * 2f;
                        pos.x = Random.Range(-spawnWidth, spawnWidth);
                        rainDrops[i].speed = fallSpeed + Random.Range(-fallSpeedVariation, fallSpeedVariation);
                    }

                    rainDrops[i].obj.transform.position = pos;
                }
            }

            // Lightning
            if (directionalLight != null)
            {
                if (!isLightning && Time.time >= nextLightning)
                {
                    isLightning = true;
                    lightningTimer = 0f;
                }

                if (isLightning)
                {
                    lightningTimer += Time.deltaTime;
                    float t = lightningTimer / lightningDuration;

                    if (t < 0.3f)
                        directionalLight.intensity = Mathf.Lerp(baseLightIntensity, lightningIntensity, t / 0.3f);
                    else if (t < 0.5f)
                        directionalLight.intensity = Mathf.Lerp(lightningIntensity, baseLightIntensity, (t - 0.3f) / 0.2f);
                    else if (t < 0.7f)
                        directionalLight.intensity = Mathf.Lerp(baseLightIntensity, lightningIntensity * 0.5f, (t - 0.5f) / 0.2f);
                    else
                        directionalLight.intensity = Mathf.Lerp(lightningIntensity * 0.5f, baseLightIntensity, (t - 0.7f) / 0.3f);

                    if (t >= 1f)
                    {
                        isLightning = false;
                        directionalLight.intensity = baseLightIntensity;
                        nextLightning = Time.time + lightningInterval + Random.Range(-lightningVariation, lightningVariation);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (rainDrops != null)
            {
                foreach (var drop in rainDrops)
                {
                    if (drop.obj != null) Destroy(drop.obj);
                }
            }
        }
    }
}
