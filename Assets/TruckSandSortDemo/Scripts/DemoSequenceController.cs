using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TruckSandSortDemo
{
    /// <summary>
    /// Deterministic, presentation-first 60 second demo. The scene contains only this
    /// component; it creates a self-contained portrait game board at runtime so every
    /// replay begins from an identical state.
    /// </summary>
    public sealed class DemoSequenceController : MonoBehaviour
    {
        [Header("Existing project assets (assigned by the editor builder)")]
        public GameObject hopperPrefab;
        public GameObject conveyorPrefab;
        public GameObject bucketPrefab;
        public GameObject conePrefab;
        public GameObject plantPrefab;
        public GameObject lampPrefab;

        [Header("Playback")]
        [Range(0.25f, 2f)] public float playbackSpeed = 1f;
        public bool autoReplay = true;
        [Min(0f)] public float finalHoldSeconds = 3f;

        readonly Color purple = new Color(0.22f, 0.08f, 0.48f);
        readonly Color purpleLight = new Color(0.49f, 0.19f, 0.86f);
        readonly Color purpleDark = new Color(0.10f, 0.025f, 0.25f);
        readonly Color blue = new Color(0.02f, 0.55f, 1f);
        readonly Color yellow = new Color(1f, 0.72f, 0.015f);
        readonly Color red = new Color(1f, 0.12f, 0.17f);
        readonly Color green = new Color(0.20f, 0.88f, 0.08f);
        readonly Color orange = new Color(1f, 0.36f, 0.025f);
        readonly Color pink = new Color(1f, 0.16f, 0.62f);

        readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        readonly List<TruckVisual> trucks = new List<TruckVisual>();
        readonly List<BucketVisual> buckets = new List<BucketVisual>();
        readonly List<ConveyorBead> conveyorBeads = new List<ConveyorBead>();
        readonly List<ParticleSystem> ambientParticles = new List<ParticleSystem>();
        readonly System.Random rng = new System.Random(20261102);

        Camera demoCamera;
        Canvas canvas;
        Text conveyorCounter;
        Text eventCaption;
        Text timerText;
        Image progressFill;
        Transform hopperSandRoot;
        Transform hopperPulse;
        Transform beltRoot;
        Transform finalSparkleRoot;
        ParticleSystem hopperDischargeBlue;
        ParticleSystem hopperDischargeYellow;
        ParticleSystem hopperDischargeRed;
        float demoTime;
        int eventIndex;
        bool built;
        bool finished;

        static readonly float[] EventTimes =
        {
            0f, 5f, 9.8f, 16f, 23f, 30f, 36f, 38.2f, 40.4f, 42.6f,
            44f, 46.2f, 50f, 55f, 59.2f, 60f
        };

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            gameObject.AddComponent<FactoryPortrait>();
            enabled = false;
        }

        void Start()
        {
            ResetDemo();
        }

        void Update()
        {
            if (!built || finished) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f) * playbackSpeed;
            demoTime += dt;
            UpdateAmbient(dt);
            UpdateConveyor(dt);
            UpdateUI();

            while (eventIndex < EventTimes.Length && demoTime >= EventTimes[eventIndex])
            {
                FireEvent(eventIndex);
                eventIndex++;
            }
        }

        void ResetDemo()
        {
            StopAllCoroutines();
            demoTime = 0f;
            eventIndex = 0;
            finished = false;
            eventCaption.text = "TRUCK & SAND SORT";
            conveyorCounter.text = "0 / 30";
            progressFill.fillAmount = 0f;

            foreach (var truck in trucks) truck.ResetVisual();
            foreach (var bucket in buckets) bucket.ResetVisual();
            foreach (Transform child in hopperSandRoot) child.gameObject.SetActive(false);
            foreach (var bead in conveyorBeads) bead.go.SetActive(false);
            SetDischarge(false);
            finalSparkleRoot.gameObject.SetActive(false);
        }

        void FireEvent(int index)
        {
            switch (index)
            {
                case 0:
                    eventCaption.text = "LEVEL 20  -  READY";
                    StartCoroutine(CaptionAfter("SORT THE TRUCKS", 2.0f));
                    break;
                case 1:
                    eventCaption.text = "BLUE TRUCK SELECTED";
                    StartCoroutine(SelectAndRoute(trucks[0], new[]
                    {
                        trucks[0].startPosition,
                        new Vector3(0f, 0.66f, 2.05f),
                        new Vector3(0f, 0.66f, 1.35f),
                        new Vector3(0f, 0.66f, 0.85f)
                    }, 4.0f));
                    break;
                case 2:
                    eventCaption.text = "BLUE SAND POUR";
                    StartCoroutine(UnloadTruck(trucks[0], blue, 5.1f, 0));
                    break;
                case 3:
                    eventCaption.text = "YELLOW JOINS THE MIX";
                    StartCoroutine(RouteThenUnload(trucks[1], yellow, 1, 6.4f));
                    break;
                case 4:
                    eventCaption.text = "RED SAND  -  COLORS STAY DISTINCT";
                    StartCoroutine(RouteThenUnload(trucks[2], red, 2, 6.4f));
                    break;
                case 5:
                    eventCaption.text = "HOPPER FLOW  >  CONVEYOR";
                    SetDischarge(true);
                    conveyorCounter.text = "15 / 30";
                    StartCoroutine(Pulse(hopperPulse, 1.08f, 0.7f));
                    foreach (var bead in conveyorBeads) bead.go.SetActive(true);
                    break;
                case 6:
                    eventCaption.text = "MATCHING COLORS";
                    conveyorCounter.text = "24 / 30";
                    StartBucketFlow(0);
                    break;
                case 7: SetBucketStage(0, 1); StartBucketFlow(1); break;
                case 8: SetBucketStage(1, 1); SetBucketStage(0, 2); StartBucketFlow(2); break;
                case 9: SetBucketStage(2, 2); SetBucketStage(1, 2); StartBucketFlow(3); break;
                case 10:
                    eventCaption.text = "RED BUCKET COMPLETE!";
                    SetBucketStage(2, 3);
                    StartCoroutine(CompleteBucket(buckets[2]));
                    conveyorCounter.text = "12 / 30";
                    break;
                case 11:
                    SetBucketStage(3, 1);
                    SetBucketStage(0, 2);
                    eventCaption.text = "NEXT BUCKET ADVANCES";
                    break;
                case 12:
                    eventCaption.text = "BLOCKED PATH";
                    StartCoroutine(BlockedAttempt(trucks[6]));
                    break;
                case 13:
                    eventCaption.text = "SMOOTH, SATISFYING FLOW";
                    SetBucketStage(0, 3);
                    SetBucketStage(1, 3);
                    StartCoroutine(Pulse(buckets[0].root, 1.08f, 0.8f));
                    StartCoroutine(Pulse(buckets[1].root, 1.08f, 0.8f));
                    finalSparkleRoot.gameObject.SetActive(true);
                    break;
                case 14:
                    eventCaption.text = "LEVEL COMPLETE";
                    StartCoroutine(FinalPayoff());
                    break;
                case 15:
                    finished = true;
                    timerText.text = "60.0s";
                    eventCaption.text = "DEMO COMPLETE";
                    if (autoReplay) StartCoroutine(ReplayAfterHold());
                    break;
            }
        }

        IEnumerator ReplayAfterHold()
        {
            yield return new WaitForSecondsRealtime(finalHoldSeconds);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        IEnumerator CaptionAfter(string message, float delay)
        {
            yield return Wait(delay);
            eventCaption.text = message;
        }

        IEnumerator SelectAndRoute(TruckVisual truck, Vector3[] points, float duration)
        {
            yield return Pulse(truck.highlight, 1.15f, 0.45f);
            truck.highlight.gameObject.SetActive(true);
            yield return MovePath(truck, points, duration);
            truck.highlight.gameObject.SetActive(false);
        }

        IEnumerator RouteThenUnload(TruckVisual truck, Color color, int sandGroup, float totalDuration)
        {
            truck.highlight.gameObject.SetActive(true);
            yield return Pulse(truck.highlight, 1.12f, 0.65f);
            Vector3 start = truck.root.position;
            var points = new[]
            {
                start,
                new Vector3(4.8f, 0.66f, start.z),
                new Vector3(6.2f, 0.66f, 3.6f),
                new Vector3(4.8f, 0.66f, 1.1f),
                new Vector3(0f, 0.66f, 0.85f)
            };
            yield return MovePath(truck, points, 2.35f);
            truck.highlight.gameObject.SetActive(false);
            yield return UnloadTruck(truck, color, 3.5f, sandGroup);
            float remaining = Mathf.Max(0.2f, totalDuration - 5.85f);
            yield return Wait(remaining);
        }

        IEnumerator MovePath(TruckVisual truck, Vector3[] points, float duration)
        {
            float segmentDuration = duration / (points.Length - 1);
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                Vector3 flatDirection = b - a;
                if (flatDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion target = Quaternion.LookRotation(flatDirection.normalized, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
                    yield return RotateTo(truck.root, target, Mathf.Min(0.35f, segmentDuration * 0.3f));
                }

                float t = 0f;
                while (t < 1f)
                {
                    t += Delta() / segmentDuration;
                    float eased = Smooth01(t);
                    truck.root.position = Vector3.LerpUnclamped(a, b, eased);
                    SpinWheels(truck, Delta() * 520f);
                    yield return null;
                }
                truck.root.position = b;
            }
        }

        IEnumerator RotateTo(Transform target, Quaternion end, float duration)
        {
            Quaternion start = target.rotation;
            float t = 0f;
            while (t < 1f)
            {
                t += Delta() / Mathf.Max(0.01f, duration);
                target.rotation = Quaternion.Slerp(start, end, Smooth01(t));
                yield return null;
            }
        }

        IEnumerator UnloadTruck(TruckVisual truck, Color color, float duration, int sandGroup)
        {
            yield return RotateTo(truck.root, Quaternion.Euler(0f, 0f, 0f), 0.35f);
            yield return TiltBed(truck, -28f, 0.45f);
            ParticleSystem stream = CreateSandStream(truck.sandNozzle, color, "TruckSandStream");
            stream.Play();

            float pourDuration = Mathf.Max(1.2f, duration - 1.8f);
            float t = 0f;
            while (t < 1f)
            {
                t += Delta() / pourDuration;
                truck.sandLoad.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.06f, Smooth01(t)), 1f);
                RevealHopperSand(sandGroup, t);
                if (Mathf.FloorToInt(t * 8f) != Mathf.FloorToInt((t - Delta() / pourDuration) * 8f))
                    StartCoroutine(Pulse(hopperPulse, 1.025f, 0.16f));
                yield return null;
            }

            stream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            yield return Wait(0.25f);
            yield return TiltBed(truck, 0f, 0.42f);
            StartCoroutine(ExitTruck(truck));
        }

        IEnumerator TiltBed(TruckVisual truck, float zAngle, float duration)
        {
            Quaternion start = truck.bedPivot.localRotation;
            Quaternion end = Quaternion.Euler(0f, 0f, zAngle);
            float t = 0f;
            while (t < 1f)
            {
                t += Delta() / duration;
                truck.bedPivot.localRotation = Quaternion.Slerp(start, end, Smooth01(t));
                yield return null;
            }
        }

        IEnumerator ExitTruck(TruckVisual truck)
        {
            yield return Wait(0.15f);
            Vector3 start = truck.root.position;
            yield return MovePath(truck, new[]
            {
                start,
                new Vector3(4.9f, 0.66f, 0.75f),
                new Vector3(6.65f, 0.66f, -0.4f),
                new Vector3(7.8f, 0.66f, -1.8f)
            }, 2.1f);
            truck.root.gameObject.SetActive(false);
        }

        IEnumerator BlockedAttempt(TruckVisual truck)
        {
            truck.highlight.gameObject.SetActive(true);
            yield return Pulse(truck.highlight, 1.12f, 0.45f);
            Vector3 start = truck.root.position;
            Vector3 hit = start + new Vector3(1.15f, 0f, 0f);
            yield return MoveSimple(truck.root, start, hit, 0.55f);
            yield return Squash(truck.root, new Vector3(0.86f, 1.05f, 1.05f), 0.16f);
            yield return MoveSimple(truck.root, hit, start - new Vector3(0.18f, 0f, 0f), 0.32f);
            yield return MoveSimple(truck.root, start - new Vector3(0.18f, 0f, 0f), start, 0.25f);
            truck.root.position = start;
            truck.highlight.gameObject.SetActive(false);
            eventCaption.text = "BLOCKED TRUCK RETURNS";
        }

        IEnumerator MoveSimple(Transform target, Vector3 a, Vector3 b, float duration)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Delta() / duration;
                target.position = Vector3.LerpUnclamped(a, b, Smooth01(t));
                yield return null;
            }
            target.position = b;
        }

        IEnumerator Squash(Transform target, Vector3 squash, float duration)
        {
            Vector3 start = target.localScale;
            float half = duration * 0.5f;
            float t = 0f;
            while (t < 1f)
            {
                t += Delta() / half;
                target.localScale = Vector3.Lerp(start, Vector3.Scale(start, squash), Smooth01(t));
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t += Delta() / half;
                target.localScale = Vector3.Lerp(Vector3.Scale(start, squash), start, EaseOutBack(t));
                yield return null;
            }
            target.localScale = start;
        }

        IEnumerator Pulse(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 start = target.localScale;
            float half = duration * 0.5f;
            float t = 0f;
            while (t < 1f)
            {
                t += Delta() / half;
                target.localScale = Vector3.Lerp(start, start * scale, EaseOutBack(t));
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t += Delta() / half;
                target.localScale = Vector3.Lerp(start * scale, start, Smooth01(t));
                yield return null;
            }
            target.localScale = start;
        }

        IEnumerator CompleteBucket(BucketVisual bucket)
        {
            bucket.glow.SetActive(true);
            bucket.check.SetActive(true);
            bucket.burst.Play();
            yield return Pulse(bucket.root, 1.16f, 0.8f);
            bucket.lid.gameObject.SetActive(true);
            yield return RotateTo(bucket.lid, Quaternion.identity, 0.35f);
            yield return Wait(0.6f);
            Vector3 start = bucket.root.position;
            yield return MoveSimple(bucket.root, start, start + new Vector3(0f, 0f, -2.3f), 0.65f);
            bucket.root.gameObject.SetActive(false);

            // A queued replacement rises into the active slot.
            bucket.queueRoot.gameObject.SetActive(true);
            Vector3 qStart = bucket.queueRoot.position;
            yield return MoveSimple(bucket.queueRoot, qStart, start, 0.7f);
        }

        IEnumerator FinalPayoff()
        {
            foreach (var bucket in buckets)
            {
                if (bucket.root.gameObject.activeSelf) StartCoroutine(Pulse(bucket.root, 1.06f, 0.55f));
            }
            yield return Wait(0.4f);
            StartCoroutine(Pulse(hopperPulse, 1.09f, 0.65f));
        }

        void SetBucketStage(int index, int value)
        {
            if (index < 0 || index >= buckets.Count) return;
            BucketVisual bucket = buckets[index];
            bucket.capacity = Mathf.Clamp(value, 0, 3);
            bucket.label.text = bucket.capacity + " / 3";
            bucket.fill.localScale = new Vector3(1f, Mathf.Lerp(0.04f, 0.95f, bucket.capacity / 3f), 1f);
            bucket.fill.gameObject.SetActive(bucket.capacity > 0);
            if (bucket.capacity == 3)
            {
                bucket.glow.SetActive(true);
                bucket.check.SetActive(true);
            }
        }

        void StartBucketFlow(int index)
        {
            if (index < 0 || index >= buckets.Count) return;
            buckets[index].stream.Play();
            StartCoroutine(StopParticleAfter(buckets[index].stream, 7.5f));
        }

        IEnumerator StopParticleAfter(ParticleSystem ps, float seconds)
        {
            yield return Wait(seconds);
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void SetDischarge(bool enabled)
        {
            var systems = new[] { hopperDischargeBlue, hopperDischargeYellow, hopperDischargeRed };
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                if (enabled) ps.Play();
                else ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        void RevealHopperSand(int group, float progress)
        {
            int groupSize = 28;
            int first = group * groupSize;
            int visible = Mathf.Clamp(Mathf.CeilToInt(progress * groupSize), 0, groupSize);
            for (int i = 0; i < visible && first + i < hopperSandRoot.childCount; i++)
                hopperSandRoot.GetChild(first + i).gameObject.SetActive(true);
        }

        void UpdateAmbient(float dt)
        {
            if (hopperPulse != null)
                hopperPulse.localPosition = new Vector3(0f, 0.04f + Mathf.Sin(demoTime * 2.2f) * 0.015f, 0f);
            if (beltRoot != null)
                beltRoot.localPosition = new Vector3(Mathf.Repeat(demoTime * 0.45f, 0.6f) - 0.3f, 0f, 0f);
        }

        void UpdateConveyor(float dt)
        {
            if (demoTime < 30f) return;
            for (int i = 0; i < conveyorBeads.Count; i++)
            {
                ConveyorBead bead = conveyorBeads[i];
                bead.offset = Mathf.Repeat(bead.offset + dt * 1.65f, 1f);
                float x = Mathf.Lerp(-4.25f, 4.25f, bead.offset);
                float z = -3.48f + Mathf.Sin((bead.offset + i * 0.37f) * Mathf.PI * 2f) * 0.11f;
                bead.go.transform.position = new Vector3(x, 0.72f, z);
                bead.go.transform.Rotate(Vector3.right, dt * 110f, Space.Self);
            }
        }

        void UpdateUI()
        {
            timerText.text = Mathf.Min(60f, demoTime).ToString("00.0") + "s";
            progressFill.fillAmount = Mathf.Clamp01(demoTime / 60f);
        }

        float Delta() => Mathf.Min(Time.unscaledDeltaTime, 0.05f) * playbackSpeed;

        WaitForSecondsRealtime Wait(float seconds)
        {
            return new WaitForSecondsRealtime(seconds / Mathf.Max(0.01f, playbackSpeed));
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            const float c = 1.70158f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }

        static void SpinWheels(TruckVisual truck, float degrees)
        {
            foreach (var wheel in truck.wheels) wheel.Rotate(Vector3.forward, degrees, Space.Self);
        }

        // ---------------------------------------------------------------------
        // Scene construction
        // ---------------------------------------------------------------------

        void BuildWorld()
        {
            if (built) return;
            built = true;
            CreateMaterials();
            CreateCameraAndLighting();
            CreateEnvironment();
            CreateParkingArea();
            CreateProductionLine();
            CreateTrucks();
            CreateUI();
            CreatePostProcessing();
        }

        void CreateMaterials()
        {
            mats["Purple"] = MakeMaterial("Purple", purple, 0.62f);
            mats["PurpleLight"] = MakeMaterial("PurpleLight", purpleLight, 0.72f);
            mats["PurpleHighlight"] = MakeMaterial("PurpleHighlight", new Color(0.73f, 0.48f, 1f), 0.82f);
            mats["PurpleDark"] = MakeMaterial("PurpleDark", purpleDark, 0.38f);
            mats["White"] = MakeMaterial("White", Color.white, 0.65f);
            mats["Black"] = MakeMaterial("Black", new Color(0.025f, 0.02f, 0.045f), 0.35f);
            mats["Glass"] = MakeMaterial("Glass", new Color(0.05f, 0.12f, 0.28f), 0.85f);
            mats["Yellow"] = MakeMaterial("Yellow", yellow, 0.72f);
            mats["Blue"] = MakeMaterial("Blue", blue, 0.72f);
            mats["Red"] = MakeMaterial("Red", red, 0.72f);
            mats["Green"] = MakeMaterial("Green", green, 0.72f);
            mats["Orange"] = MakeMaterial("Orange", orange, 0.72f);
            mats["Pink"] = MakeMaterial("Pink", pink, 0.72f);
            mats["Glow"] = MakeMaterial("Glow", new Color(1f, 0.46f, 0.04f), 0.9f, true);
            mats["GreenGlow"] = MakeMaterial("GreenGlow", new Color(0.18f, 1f, 0.12f), 0.9f, true);
        }

        Material MakeMaterial(string name, Color color, float smoothness, bool emissive = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "Demo_" + name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (emissive && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 3.3f);
            }
            return mat;
        }

        void CreateCameraAndLighting()
        {
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(camera.gameObject);
            demoCamera = new GameObject("Main Camera").AddComponent<Camera>();
            demoCamera.tag = "MainCamera";
            demoCamera.orthographic = false;
            // The reference is perspective, not orthographic: the lower buckets are
            // visibly larger than the parking trucks and the front faces stay readable.
            // This is a locked 0-degree yaw / ~56-degree downward pitch product camera.
            demoCamera.fieldOfView = 28f;
            demoCamera.nearClipPlane = 0.1f;
            demoCamera.farClipPlane = 120f;
            demoCamera.transform.position = new Vector3(0f, 39f, -25.5f);
            demoCamera.transform.LookAt(new Vector3(0f, 0.1f, 0.65f));
            demoCamera.clearFlags = CameraClearFlags.SolidColor;
            demoCamera.backgroundColor = purpleDark;
            demoCamera.allowHDR = true;
            var cameraData = demoCamera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var key = new GameObject("Key Light").AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.84f, 0.70f);
            key.intensity = 1.75f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.58f;
            key.transform.rotation = Quaternion.Euler(58f, -38f, 0f);

            var rim = new GameObject("Purple Rim Light").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.58f, 0.32f, 1f);
            rim.intensity = 1.35f;
            rim.shadows = LightShadows.None;
            rim.transform.rotation = Quaternion.Euler(28f, 150f, 0f);

            var warmFill = new GameObject("Warm Hopper Fill").AddComponent<Light>();
            warmFill.type = LightType.Point;
            warmFill.color = new Color(1f, 0.48f, 0.16f);
            warmFill.intensity = 3.1f;
            warmFill.range = 7.5f;
            warmFill.shadows = LightShadows.None;
            warmFill.transform.position = new Vector3(-4.9f, 4.8f, -1.4f);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.32f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.23f, 0.12f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.018f, 0.13f);
        }

        void CreateEnvironment()
        {
            CreatePrimitive("Backdrop", PrimitiveType.Cube, new Vector3(0f, -0.72f, 0.4f), new Vector3(16f, 0.65f, 26f), mats["PurpleDark"]);
            CreatePrimitive("Board", PrimitiveType.Cube, new Vector3(0f, -0.16f, 0.6f), new Vector3(13.7f, 0.45f, 22.4f), mats["Purple"]);

            // Subtle side decorations from the supplied project assets.
            PlacePrefab(plantPrefab, "Plant Decoration", new Vector3(-6.0f, 0.18f, 8.0f), 1.05f, Quaternion.Euler(0f, 20f, 0f));
            PlacePrefab(lampPrefab, "Lamp Decoration", new Vector3(-5.95f, 0.18f, -3.4f), 1.25f, Quaternion.identity);
            PlacePrefab(conePrefab, "Exit Cone", new Vector3(5.85f, 0.18f, 0.85f), 0.82f, Quaternion.Euler(0f, -15f, 0f));
        }

        void CreateParkingArea()
        {
            Transform parking = new GameObject("01 Parking Arena").transform;
            CreatePrimitive("Parking Floor", PrimitiveType.Cube, new Vector3(0f, 0.11f, 6.55f), new Vector3(11.2f, 0.32f, 7.65f), mats["PurpleDark"], parking);
            CreateRail(new Vector3(0f, 0.43f, 10.35f), new Vector3(11.45f, 0.55f, 0.48f), parking);
            CreateRail(new Vector3(-5.55f, 0.43f, 6.55f), new Vector3(0.48f, 0.55f, 7.8f), parking);
            CreateRail(new Vector3(5.55f, 0.43f, 7.2f), new Vector3(0.48f, 0.55f, 6.5f), parking);
            CreateRail(new Vector3(-3.25f, 0.43f, 2.75f), new Vector3(4.65f, 0.55f, 0.48f), parking);
            CreateRail(new Vector3(3.25f, 0.43f, 2.75f), new Vector3(4.65f, 0.55f, 0.48f), parking);
            CreateRoundedCorner(new Vector3(-5.55f, 0.43f, 10.35f), parking);
            CreateRoundedCorner(new Vector3(5.55f, 0.43f, 10.35f), parking);
            CreateRoundedCorner(new Vector3(-5.55f, 0.43f, 2.75f), parking);

            // Glowing exit route.
            CreatePrimitive("Exit Route Horizontal", PrimitiveType.Cube, new Vector3(5.72f, 0.19f, 2.0f), new Vector3(0.88f, 0.11f, 0.62f), mats["PurpleLight"], parking);
            CreatePrimitive("Exit Route Vertical", PrimitiveType.Cube, new Vector3(5.95f, 0.19f, 1.15f), new Vector3(0.62f, 0.11f, 2.2f), mats["PurpleLight"], parking);
            CreatePrimitive("Exit Glow Horizontal", PrimitiveType.Cube, new Vector3(5.72f, 0.16f, 2.0f), new Vector3(1.0f, 0.05f, 0.74f), mats["Glow"], parking);
            CreatePrimitive("Exit Glow Vertical", PrimitiveType.Cube, new Vector3(5.95f, 0.16f, 1.15f), new Vector3(0.74f, 0.05f, 2.35f), mats["Glow"], parking);
            for (int i = 0; i < 2; i++)
                CreateChevron(new Vector3(5.95f, 0.34f, 1.55f - i * 0.7f), Quaternion.Euler(0f, -90f, 0f), 0.38f, parking);
            CreateBarrier(new Vector3(5.9f, 0.72f, 0.02f), parking);
        }

        void CreateProductionLine()
        {
            Transform production = new GameObject("02 Sand Production Line").transform;

            // Supplied hopper asset, backed by a stylized readable rim and visible sand pile.
            GameObject hopper = PlacePrefab(hopperPrefab, "Hopper (Supplied Asset)", new Vector3(0f, 0.35f, -0.15f), 5.15f, Quaternion.identity, production);
            if (hopper == null)
                hopper = CreatePrimitive("Hopper Fallback", PrimitiveType.Cube, new Vector3(0f, 0.45f, -0.35f), new Vector3(5f, 1.1f, 2.2f), mats["PurpleLight"], production);

            hopperPulse = new GameObject("Hopper Pulse").transform;
            hopperPulse.SetParent(production, false);
            hopperPulse.position = new Vector3(0f, 0.55f, 0.02f);
            CreatePrimitive("Yellow Hopper Rim", PrimitiveType.Cube, Vector3.zero, new Vector3(5.55f, 0.18f, 2.55f), mats["Yellow"], hopperPulse);
            CreatePrimitive("Hopper Mouth", PrimitiveType.Cube, new Vector3(0f, 0.11f, 0f), new Vector3(4.95f, 0.15f, 1.92f), mats["PurpleDark"], hopperPulse);
            CreatePrimitive("Hopper Rim Left", PrimitiveType.Cylinder, new Vector3(-2.65f, 0.03f, 0f), new Vector3(0.34f, 0.17f, 0.34f), mats["Yellow"], hopperPulse);
            CreatePrimitive("Hopper Rim Right", PrimitiveType.Cylinder, new Vector3(2.65f, 0.03f, 0f), new Vector3(0.34f, 0.17f, 0.34f), mats["Yellow"], hopperPulse);

            hopperSandRoot = new GameObject("Mixed Sand In Hopper").transform;
            hopperSandRoot.SetParent(production, false);
            hopperSandRoot.position = new Vector3(0f, 0.82f, 0.02f);
            CreateHopperSandBeads();

            GameObject conveyor = PlacePrefab(conveyorPrefab, "Conveyor (Supplied Asset)", new Vector3(0f, 0.25f, -3.45f), 8.9f, Quaternion.identity, production);
            if (conveyor == null)
                conveyor = CreatePrimitive("Conveyor Fallback", PrimitiveType.Cube, new Vector3(0f, 0.35f, -3.45f), new Vector3(9.3f, 0.75f, 1.65f), mats["PurpleLight"], production);

            beltRoot = new GameObject("Animated Belt Highlights").transform;
            beltRoot.SetParent(production, false);
            for (int i = -7; i <= 7; i++)
                CreateChevron(new Vector3(i * 0.62f, 0.74f, -3.45f), Quaternion.identity, 0.33f, beltRoot);

            CreateConveyorBeads(production);
            CreateHopperDischarge(production);
            CreateBucketColumns(production);
            CreatePipework(production);
        }

        void CreateTrucks()
        {
            // Ten-truck composition follows the reference: two top horizontals,
            // a dense middle, two vertical blockers and a selected center exit truck.
            trucks.Add(CreateTruck("Blue Success Truck", blue, new Vector3(0f, 0.66f, 3.15f), 90f, 90f));
            trucks.Add(CreateTruck("Yellow Success Truck", yellow, new Vector3(2.45f, 0.66f, 8.75f), 180f, 180f));
            trucks.Add(CreateTruck("Red Success Truck", red, new Vector3(-2.45f, 0.66f, 8.75f), 180f, 180f));
            trucks.Add(CreateTruck("Green Right Truck", green, new Vector3(4.45f, 0.66f, 7.0f), 90f, 90f));
            trucks.Add(CreateTruck("Orange Center Truck", orange, new Vector3(-0.65f, 0.66f, 4.95f), 180f, 180f));
            trucks.Add(CreateTruck("Pink Vertical Truck", pink, new Vector3(3.05f, 0.66f, 4.55f), 90f, 90f));
            trucks.Add(CreateTruck("Purple Blocked Truck", purpleLight, new Vector3(-2.7f, 0.66f, 6.65f), 0f, 0f));
            trucks.Add(CreateTruck("Green Left Truck", green, new Vector3(-4.45f, 0.66f, 5.1f), 90f, -90f));
            trucks.Add(CreateTruck("Yellow Lower Truck", yellow, new Vector3(-3.45f, 0.66f, 3.35f), 0f, 0f));
            trucks.Add(CreateTruck("Blue Middle Truck", blue, new Vector3(1.3f, 0.66f, 6.55f), 0f, 0f));
        }

        TruckVisual CreateTruck(string name, Color color, Vector3 position, float yaw, float arrowWorldYaw)
        {
            Material body = MakeMaterial(name + " Body", color, 0.72f);
            Material sand = MakeMaterial(name + " Sand", color, 0.42f);
            Transform root = new GameObject(name).transform;
            root.position = position;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            CreatePrimitive("Chassis", PrimitiveType.Cube, new Vector3(-0.05f, 0.35f, 0f), new Vector3(2.35f, 0.28f, 1.0f), mats["Black"], root);
            Transform cab = CreatePrimitive("Cab", PrimitiveType.Cube, new Vector3(0.78f, 0.78f, 0f), new Vector3(0.9f, 0.85f, 0.92f), body, root).transform;
            cab.localRotation = Quaternion.Euler(0f, 0f, -3f);
            CreatePrimitive("Rounded Cab Nose", PrimitiveType.Capsule, new Vector3(1.05f, 0.72f, 0f), new Vector3(0.42f, 0.48f, 0.44f), body, root).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            CreatePrimitive("Windshield", PrimitiveType.Cube, new Vector3(1.23f, 0.88f, 0f), new Vector3(0.035f, 0.38f, 0.62f), mats["Glass"], root);
            CreatePrimitive("Headlight L", PrimitiveType.Sphere, new Vector3(1.27f, 0.56f, -0.29f), new Vector3(0.1f, 0.1f, 0.1f), mats["White"], root);
            CreatePrimitive("Headlight R", PrimitiveType.Sphere, new Vector3(1.27f, 0.56f, 0.29f), new Vector3(0.1f, 0.1f, 0.1f), mats["White"], root);

            Transform bedPivot = new GameObject("Tilting Cargo Bed").transform;
            bedPivot.SetParent(root, false);
            bedPivot.localPosition = new Vector3(-0.72f, 0.52f, 0f);
            CreatePrimitive("Bed Base", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(1.48f, 0.25f, 1.05f), body, bedPivot);
            CreatePrimitive("Bed Side L", PrimitiveType.Cube, new Vector3(0f, 0.42f, -0.48f), new Vector3(1.48f, 0.55f, 0.12f), body, bedPivot);
            CreatePrimitive("Bed Side R", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0.48f), new Vector3(1.48f, 0.55f, 0.12f), body, bedPivot);
            CreatePrimitive("Bed Tail", PrimitiveType.Cube, new Vector3(-0.69f, 0.42f, 0f), new Vector3(0.12f, 0.55f, 1f), body, bedPivot);
            Transform sandLoad = new GameObject("Sand Load").transform;
            sandLoad.SetParent(bedPivot, false);
            sandLoad.localPosition = new Vector3(0f, 0.34f, 0f);
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 6; col++)
            {
                float beadX = -0.52f + col * 0.21f + (row % 2) * 0.05f;
                float beadZ = -0.34f + row * 0.22f;
                float beadY = 0.08f + Mathf.Sin((col + row) * 1.7f) * 0.025f;
                CreatePrimitive("Cargo Sand Bead", PrimitiveType.Sphere, new Vector3(beadX, beadY, beadZ), Vector3.one * 0.18f, sand, sandLoad);
            }
            Transform nozzle = new GameObject("Sand Nozzle").transform;
            nozzle.SetParent(bedPivot, false);
            nozzle.localPosition = new Vector3(-0.78f, 0.25f, 0f);

            var wheels = new List<Transform>();
            foreach (float x in new[] { -0.75f, 0.78f })
            foreach (float z in new[] { -0.55f, 0.55f })
            {
                Transform wheel = CreatePrimitive("Wheel", PrimitiveType.Cylinder, new Vector3(x, 0.28f, z), new Vector3(0.36f, 0.16f, 0.36f), mats["Black"], root).transform;
                wheel.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheels.Add(wheel);
            }

            CreateArrow(new Vector3(-0.72f, 1.24f, 0f), Quaternion.Euler(0f, arrowWorldYaw - yaw, 0f), 0.88f, root);
            Transform highlight = CreateHighlight(root);
            highlight.gameObject.SetActive(false);

            return new TruckVisual
            {
                root = root,
                bedPivot = bedPivot,
                sandLoad = sandLoad,
                sandNozzle = nozzle,
                highlight = highlight,
                wheels = wheels,
                startPosition = position,
                startRotation = root.rotation,
                startScale = root.localScale
            };
        }

        Transform CreateHighlight(Transform parent)
        {
            Transform ring = new GameObject("Selection Glow").transform;
            ring.SetParent(parent, false);
            ring.localPosition = new Vector3(0f, 0.08f, 0f);
            CreatePrimitive("Glow Front", PrimitiveType.Cube, new Vector3(0f, 0f, 0.78f), new Vector3(2.85f, 0.07f, 0.08f), mats["Glow"], ring);
            CreatePrimitive("Glow Back", PrimitiveType.Cube, new Vector3(0f, 0f, -0.78f), new Vector3(2.85f, 0.07f, 0.08f), mats["Glow"], ring);
            CreatePrimitive("Glow Left", PrimitiveType.Cube, new Vector3(-1.39f, 0f, 0f), new Vector3(0.08f, 0.07f, 1.55f), mats["Glow"], ring);
            CreatePrimitive("Glow Right", PrimitiveType.Cube, new Vector3(1.39f, 0f, 0f), new Vector3(0.08f, 0.07f, 1.55f), mats["Glow"], ring);
            return ring;
        }

        void CreateHopperSandBeads()
        {
            Color[] colors = { blue, yellow, red };
            for (int group = 0; group < 3; group++)
            {
                Material material = MakeMaterial("Hopper Sand " + group, colors[group], 0.34f);
                for (int i = 0; i < 28; i++)
                {
                    float x = Mathf.Lerp(-1.9f, 1.9f, (float)rng.NextDouble());
                    float z = Mathf.Lerp(-0.65f, 0.65f, (float)rng.NextDouble());
                    float y = Mathf.Lerp(0f, 0.35f, (float)rng.NextDouble()) + Mathf.Max(0f, 0.34f - Mathf.Abs(x) * 0.08f);
                    GameObject bead = CreatePrimitive("Sand Bead", PrimitiveType.Sphere, new Vector3(x, y, z), Vector3.one * Mathf.Lerp(0.16f, 0.25f, (float)rng.NextDouble()), material, hopperSandRoot);
                    bead.SetActive(false);
                }
            }
        }

        void CreateConveyorBeads(Transform parent)
        {
            Color[] colors = { blue, yellow, red };
            for (int i = 0; i < 42; i++)
            {
                Material material = MakeMaterial("Belt Bead " + i, colors[i % 3], 0.38f);
                GameObject bead = CreatePrimitive("Conveyor Sand", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.18f, material, parent);
                bead.SetActive(false);
                conveyorBeads.Add(new ConveyorBead { go = bead, offset = i / 42f });
            }
        }

        void CreateHopperDischarge(Transform parent)
        {
            Vector3 outlet = new Vector3(0f, 1.2f, -2.55f);
            hopperDischargeBlue = CreateSandStreamAt(outlet + new Vector3(-0.18f, 0f, 0f), blue, "Blue Hopper Discharge", parent, 60f, 0.85f);
            hopperDischargeYellow = CreateSandStreamAt(outlet, yellow, "Yellow Hopper Discharge", parent, 60f, 0.85f);
            hopperDischargeRed = CreateSandStreamAt(outlet + new Vector3(0.18f, 0f, 0f), red, "Red Hopper Discharge", parent, 60f, 0.85f);
        }

        void CreateBucketColumns(Transform parent)
        {
            Color[] colors = { blue, yellow, red, green };
            string[] names = { "Blue", "Yellow", "Red", "Green" };
            float[] xs = { -4.65f, -1.55f, 1.55f, 4.65f };
            for (int i = 0; i < 4; i++)
            {
                Material mat = MakeMaterial(names[i] + " Bucket", colors[i], 0.68f);
                Transform root = new GameObject(names[i] + " Bucket Column").transform;
                root.SetParent(parent, false);
                root.position = new Vector3(xs[i], 0.35f, -8.55f);

                GameObject supplied = PlacePrefab(bucketPrefab, "Bucket (Supplied Asset)", Vector3.zero, 1.9f, Quaternion.identity, root);
                if (supplied != null)
                {
                    supplied.transform.localPosition = Vector3.zero;
                    TintRenderers(supplied, mat);
                }
                else
                {
                    CreatePrimitive("Bucket Body", PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.45f, 1.15f, 1.45f), mat, root);
                }

                Transform fill = CreatePrimitive("Visible Sand Fill", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(1.18f, 0.08f, 1.18f), MakeMaterial(names[i] + " Fill", colors[i], 0.3f), root).transform;
                fill.gameObject.SetActive(false);
                Transform lid = CreatePrimitive("Lid", PrimitiveType.Cylinder, new Vector3(0f, 1.15f, -0.65f), new Vector3(1.55f, 0.12f, 1.55f), mat, root).transform;
                lid.localRotation = Quaternion.Euler(-75f, 0f, 0f);
                lid.gameObject.SetActive(false);

                GameObject glow = CreatePrimitive("Completion Glow", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(1.85f, 0.05f, 1.85f), mats["Glow"], root);
                glow.SetActive(false);
                GameObject check = CreateWorldLabel("CHECK", new Vector3(0f, 1.55f, 0f), 0.28f, Color.white, root);
                check.SetActive(false);
                Text capacity = CreateWorldText("0 / 3", new Vector3(0f, 0.48f, -0.92f), 0.39f, Color.white, root);

                Transform queue = new GameObject(names[i] + " Queued Bucket").transform;
                queue.SetParent(parent, false);
                queue.position = root.position + new Vector3(0f, 0f, 1.3f);
                GameObject queuedModel = PlacePrefab(bucketPrefab, "Queued Supplied Bucket", Vector3.zero, 1.55f, Quaternion.identity, queue);
                if (queuedModel != null) { queuedModel.transform.localPosition = Vector3.zero; TintRenderers(queuedModel, mat); }
                else CreatePrimitive("Queued Bucket", PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.2f, 0.95f, 1.2f), mat, queue);

                ParticleSystem stream = CreateSandStreamAt(new Vector3(xs[i], 2.65f, -7.95f), colors[i], names[i] + " Bucket Stream", parent, 80f, 1.0f);
                ParticleSystem burst = CreateCelebration(root, colors[i]);

                buckets.Add(new BucketVisual
                {
                    root = root,
                    fill = fill,
                    lid = lid,
                    glow = glow,
                    check = check,
                    label = capacity,
                    queueRoot = queue,
                    stream = stream,
                    burst = burst,
                    startPosition = root.position,
                    queueStart = queue.position
                });
            }
        }

        void CreatePipework(Transform parent)
        {
            CreatePrimitive("Pipe Header", PrimitiveType.Cube, new Vector3(0f, 2.55f, -7.15f), new Vector3(10.9f, 0.4f, 0.5f), mats["PurpleLight"], parent);
            float[] xs = { -4.65f, -1.55f, 1.55f, 4.65f };
            foreach (float x in xs)
            {
                CreatePrimitive("Pipe Drop", PrimitiveType.Cube, new Vector3(x, 2.05f, -7.45f), new Vector3(0.45f, 1.2f, 0.45f), mats["PurpleLight"], parent);
                CreatePrimitive("Pipe Cuff", PrimitiveType.Cylinder, new Vector3(x, 1.5f, -7.45f), new Vector3(0.62f, 0.18f, 0.62f), mats["Yellow"], parent);
            }
            CreatePrimitive("Bucket Platform", PrimitiveType.Cube, new Vector3(0f, 0.0f, -8.85f), new Vector3(12f, 0.35f, 2.35f), mats["PurpleLight"], parent);
        }

        void CreateUI()
        {
            GameObject canvasGo = new GameObject("Demo UI");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);
            canvasGo.AddComponent<GraphicRaycaster>();

            MakePanel("Gear Panel", new Vector2(0.085f, 0.955f), new Vector2(118f, 102f), new Color(0.49f, 0.20f, 0.88f, 0.96f));
            MakeText("Gear", "GEAR", new Vector2(0.085f, 0.955f), new Vector2(118f, 70f), 25, TextAnchor.MiddleCenter, Color.white);
            MakePanel("Level Pill", new Vector2(0.49f, 0.955f), new Vector2(350f, 94f), new Color(0.55f, 0.21f, 0.92f, 0.98f));
            MakeText("Level", "LEVEL 20", new Vector2(0.49f, 0.955f), new Vector2(330f, 76f), 47, TextAnchor.MiddleCenter, Color.white);
            MakePanel("Coin Pill", new Vector2(0.80f, 0.955f), new Vector2(250f, 94f), new Color(0.32f, 0.12f, 0.63f, 0.98f));
            MakeText("Coins", "●  1,050", new Vector2(0.80f, 0.955f), new Vector2(230f, 72f), 38, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.25f));
            MakePanel("Plus Panel", new Vector2(0.94f, 0.955f), new Vector2(90f, 94f), new Color(1f, 0.62f, 0.03f, 1f));
            MakeText("Plus", "+", new Vector2(0.94f, 0.957f), new Vector2(80f, 80f), 57, TextAnchor.MiddleCenter, Color.white);

            eventCaption = MakeText("Event Caption", "TRUCK & SAND SORT", new Vector2(0.5f, 0.903f), new Vector2(760f, 56f), 27, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.35f));
            timerText = MakeText("Timer", "00.0s", new Vector2(0.89f, 0.903f), new Vector2(150f, 50f), 22, TextAnchor.MiddleCenter, Color.white);

            MakePanel("Conveyor Counter Panel", new Vector2(0.76f, 0.39f), new Vector2(250f, 90f), new Color(0.19f, 0.05f, 0.42f, 0.92f));
            conveyorCounter = MakeText("Conveyor Counter", "0 / 30", new Vector2(0.76f, 0.39f), new Vector2(220f, 70f), 34, TextAnchor.MiddleCenter, Color.white);

            GameObject progressBg = MakePanel("Timeline", new Vector2(0.5f, 0.018f), new Vector2(880f, 18f), new Color(0.08f, 0.02f, 0.18f, 0.9f));
            GameObject fill = MakePanel("Timeline Fill", new Vector2(0f, 0.5f), Vector2.one, new Color(1f, 0.55f, 0.02f, 1f), progressBg.transform);
            RectTransform fr = (RectTransform)fill.transform;
            fr.anchorMin = new Vector2(0f, 0f);
            fr.anchorMax = new Vector2(1f, 1f);
            fr.offsetMin = Vector2.zero;
            fr.offsetMax = Vector2.zero;
            progressFill = fill.GetComponent<Image>();
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;

            finalSparkleRoot = new GameObject("Final Sparkles").transform;
            finalSparkleRoot.SetParent(transform, false);
            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * 5.2f, 1.1f, -5.7f + Mathf.Sin(angle) * 2.6f);
                CreatePrimitive("Sparkle", PrimitiveType.Sphere, p, Vector3.one * 0.13f, i % 2 == 0 ? mats["Glow"] : mats["GreenGlow"], finalSparkleRoot);
            }
            finalSparkleRoot.gameObject.SetActive(false);
        }

        void CreatePostProcessing()
        {
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(0.28f);
            colorAdjustments.contrast.Override(18f);
            colorAdjustments.saturation.Override(27f);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.67f);
            bloom.intensity.Override(0.78f);
            bloom.scatter.Override(0.68f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.20f);
            vignette.smoothness.Override(0.56f);
        }

        // ---------------------------------------------------------------------
        // Construction helpers
        // ---------------------------------------------------------------------

        GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (go.TryGetComponent<Collider>(out var collider)) Destroy(collider);
            if (go.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return go;
        }

        void CreateRail(Vector3 position, Vector3 scale, Transform parent)
        {
            GameObject rail = CreatePrimitive("Arena Rail", PrimitiveType.Cube, position, scale, mats["PurpleLight"], parent);
            rail.transform.localRotation = Quaternion.identity;
            CreatePrimitive("Rail Highlight", PrimitiveType.Cube, position + new Vector3(0f, 0.29f, 0f), new Vector3(scale.x * 0.92f, 0.07f, scale.z * 0.92f), mats["PurpleHighlight"], parent).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        void CreateRoundedCorner(Vector3 position, Transform parent)
        {
            CreatePrimitive("Rounded Arena Corner", PrimitiveType.Sphere, position, new Vector3(0.58f, 0.55f, 0.58f), mats["PurpleLight"], parent);
        }

        void CreateBarrier(Vector3 position, Transform parent)
        {
            Transform root = new GameObject("Striped Exit Barrier").transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            for (int i = 0; i < 6; i++)
                CreatePrimitive("Stripe", PrimitiveType.Cube, new Vector3((i - 2.5f) * 0.28f, 0f, 0f), new Vector3(0.25f, 0.18f, 0.22f), i % 2 == 0 ? mats["White"] : mats["Red"], root);
            CreatePrimitive("Barrier Pole", PrimitiveType.Cube, new Vector3(0.8f, -0.45f, 0f), new Vector3(0.13f, 0.9f, 0.13f), mats["White"], root);
        }

        void CreateArrow(Vector3 position, Quaternion rotation, float size, Transform parent)
        {
            Transform arrow = new GameObject("Direction Arrow").transform;
            arrow.SetParent(parent, false);
            arrow.localPosition = position;
            arrow.localRotation = rotation;
            CreatePrimitive("Arrow Shaft", PrimitiveType.Cube, Vector3.zero, new Vector3(size, 0.055f, size * 0.24f), mats["White"], arrow);
            Transform head = CreatePrimitive("Arrow Head", PrimitiveType.Cube, new Vector3(size * 0.46f, 0f, 0f), new Vector3(size * 0.42f, 0.055f, size * 0.42f), mats["White"], arrow).transform;
            head.localRotation = Quaternion.Euler(0f, 45f, 0f);
        }

        void CreateChevron(Vector3 position, Quaternion rotation, float size, Transform parent)
        {
            Transform root = new GameObject("Route Chevron").transform;
            root.SetParent(parent, false);
            root.position = position;
            root.rotation = rotation;
            Transform a = CreatePrimitive("Chevron A", PrimitiveType.Cube, new Vector3(-0.12f, 0f, 0.13f), new Vector3(size, 0.06f, size * 0.16f), mats["White"], root).transform;
            Transform b = CreatePrimitive("Chevron B", PrimitiveType.Cube, new Vector3(-0.12f, 0f, -0.13f), new Vector3(size, 0.06f, size * 0.16f), mats["White"], root).transform;
            a.localRotation = Quaternion.Euler(0f, 35f, 0f);
            b.localRotation = Quaternion.Euler(0f, -35f, 0f);
        }

        ParticleSystem CreateSandStream(Transform parent, Color color, string name)
        {
            return CreateSandStreamAt(Vector3.zero, color, name, parent, 125f, 1.25f);
        }

        ParticleSystem CreateSandStreamAt(Vector3 position, Color color, string name, Transform parent, float rate, float lifetime)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = parent == null ? position : (position == Vector3.zero ? parent.position : position);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = 1.45f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.075f, 0.13f);
            main.startColor = color;
            main.gravityModifier = 1.15f;
            main.maxParticles = 420;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 4f;
            shape.radius = 0.1f;
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 1.2f;
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            renderer.sharedMaterial = MakeMaterial(name + " Material", color, 0.28f);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        ParticleSystem CreateCelebration(Transform parent, Color color)
        {
            GameObject go = new GameObject("Completion Burst");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.7f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.gravityModifier = 0.45f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.3f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            renderer.sharedMaterial = mats["Glow"];
            return ps;
        }

        GameObject PlacePrefab(GameObject prefab, string name, Vector3 position, float targetSize, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, position, rotation, parent);
            go.name = name;
            Bounds bounds = GetBounds(go);
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (max > 0.001f) go.transform.localScale *= targetSize / max;
            bounds = GetBounds(go);
            Vector3 correction = go.transform.position - bounds.center;
            go.transform.position = position + correction;
            foreach (var collider in go.GetComponentsInChildren<Collider>()) Destroy(collider);
            return go;
        }

        static Bounds GetBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        static void TintRenderers(GameObject go, Material material)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                Material[] copy = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < copy.Length; i++) copy[i] = material;
                renderer.sharedMaterials = copy;
            }
        }

        Text CreateWorldText(string value, Vector3 localPosition, float size, Color color, Transform parent)
        {
            GameObject go = new GameObject("World Label " + value);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.rotation = demoCamera.transform.rotation;
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = value;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 72;
            mesh.characterSize = size * 0.1f;
            mesh.color = color;
            mesh.fontStyle = FontStyle.Bold;
            return CreateWorldTextProxy(mesh);
        }

        Text CreateWorldTextProxy(TextMesh source)
        {
            // Capacity labels are updated through a tiny hidden UI Text proxy while the
            // visible TextMesh is synchronized each frame by WorldTextProxy.
            var proxy = source.gameObject.AddComponent<WorldTextProxy>();
            proxy.target = source;
            GameObject hidden = new GameObject("Text Value");
            hidden.transform.SetParent(source.transform, false);
            Text text = hidden.AddComponent<Text>();
            text.text = source.text;
            text.enabled = false;
            proxy.source = text;
            return text;
        }

        GameObject CreateWorldLabel(string value, Vector3 localPosition, float size, Color color, Transform parent)
        {
            Text hidden = CreateWorldText(value, localPosition, size, color, parent);
            return hidden.transform.parent.gameObject;
        }

        GameObject MakePanel(string name, Vector2 anchor, Vector2 size, Color color, Transform parent = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent == null ? canvas.transform : parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = color;
            return go;
        }

        Text MakeText(string name, string value, Vector2 anchor, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        [Serializable]
        sealed class TruckVisual
        {
            public Transform root;
            public Transform bedPivot;
            public Transform sandLoad;
            public Transform sandNozzle;
            public Transform highlight;
            public List<Transform> wheels;
            public Vector3 startPosition;
            public Quaternion startRotation;
            public Vector3 startScale;

            public void ResetVisual()
            {
                root.gameObject.SetActive(true);
                root.position = startPosition;
                root.rotation = startRotation;
                root.localScale = startScale;
                bedPivot.localRotation = Quaternion.identity;
                sandLoad.localScale = Vector3.one;
                highlight.gameObject.SetActive(false);
            }
        }

        [Serializable]
        sealed class BucketVisual
        {
            public Transform root;
            public Transform fill;
            public Transform lid;
            public GameObject glow;
            public GameObject check;
            public Text label;
            public Transform queueRoot;
            public ParticleSystem stream;
            public ParticleSystem burst;
            public int capacity;
            public Vector3 startPosition;
            public Vector3 queueStart;

            public void ResetVisual()
            {
                root.gameObject.SetActive(true);
                root.position = startPosition;
                root.localScale = Vector3.one;
                fill.gameObject.SetActive(false);
                fill.localScale = new Vector3(1f, 0.04f, 1f);
                lid.gameObject.SetActive(false);
                lid.localRotation = Quaternion.Euler(-75f, 0f, 0f);
                glow.SetActive(false);
                check.SetActive(false);
                capacity = 0;
                label.text = "0 / 3";
                queueRoot.gameObject.SetActive(true);
                queueRoot.position = queueStart;
                stream.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        sealed class ConveyorBead
        {
            public GameObject go;
            public float offset;
        }
    }

    /// <summary>Keeps a world TextMesh readable while allowing the controller to use UI Text as a mutable value holder.</summary>
    public sealed class WorldTextProxy : MonoBehaviour
    {
        public Text source;
        public TextMesh target;

        void LateUpdate()
        {
            if (source != null && target != null && source.text != target.text) target.text = source.text;
        }
    }
}
