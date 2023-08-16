using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

// Editor window for listing all float curves in an animation clip
public class RepresentAnimation : EditorWindow
{
    private Animator animator;
    private AnimationClip clip;
    private AnimatorClipInfo[] clipInfos;

    private GameObject selectedObject;

    int keyframeCount = -1;
    int indexKeyframe = 0;
    float valueOutWeight = 1 / 3f;
    float valueInWeight = 1 / 3f;
    bool toggleOutWeight = false;
    bool toggleInWeight = false;

    AnimationCurve[] curvesFromAnimation = new AnimationCurve[3];

    private Vector4[] tangentX;
    private Vector4[] tangentY;
    private Vector4[] tangentZ;

    private string errorLog = "";
    private bool isAnimRunning = false;

    Vector3[] positions;

    [MenuItem("TechArtTool/Represent Animation")]
    static void Init()
    {
        GetWindowWithRect(typeof(RepresentAnimation), new Rect(200f, 200f, 500f, 600f));
    }

    private void Update()
    {
        if (isAnimRunning)
        {
            animator.Update(Time.deltaTime);
        }


    }

    public void OnGUI()
    {
        ObjectSelectionGetData();

        if (selectedObject != null)
        {
            EditorGUILayout.LabelField("Object: " + selectedObject.name, EditorStyles.boldLabel);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (errorLog.Length <= 0)
        {
            float[] positionX = GetCurvePoints(curvesFromAnimation[0])[0].ToArray();
            float[] positionY = GetCurvePoints(curvesFromAnimation[1])[0].ToArray();
            float[] positionZ = GetCurvePoints(curvesFromAnimation[2])[0].ToArray();

            EditorGUILayout.LabelField("Curves:", EditorStyles.boldLabel);
            if (GameObject.Find(selectedObject.name + " Curve"))
            {
                if (GUILayout.Button("Update Curve"))
                {
                    UpdateBezierCurve("Update", positionX, positionY, positionZ);
                }
            }
            else
            {
                if (GUILayout.Button("Create Curve"))
                {
                    UpdateBezierCurve("Create", positionX, positionY, positionZ);
                }
            }


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            if (keyframeCount >= 0)
            {
                EditorGUILayout.LabelField("Keyframe Count: " + keyframeCount, EditorStyles.boldLabel);
                string[] options = new string[keyframeCount];
                for (int i = 0; i < keyframeCount; i++)
                {
                    options[i] = "Keyframe " + i;
                }
                indexKeyframe = EditorGUILayout.Popup(indexKeyframe, options);

                //void OnGUI()
                EditorGUILayout.BeginHorizontal();
                toggleInWeight = EditorGUILayout.BeginToggleGroup("In Weight", toggleInWeight);
                if (!toggleInWeight)
                {
                    valueInWeight = curvesFromAnimation[0].keys[indexKeyframe].inWeight;
                }
                valueInWeight = EditorGUILayout.Slider(valueInWeight, 0f, 1.0f);
                EditorGUILayout.EndToggleGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                toggleOutWeight = EditorGUILayout.BeginToggleGroup("Out Weight", toggleOutWeight);
                if (!toggleOutWeight)
                {
                    valueOutWeight = curvesFromAnimation[0].keys[indexKeyframe].outWeight;
                }
                valueOutWeight = EditorGUILayout.Slider(valueOutWeight, 0f, 1.0f);
                EditorGUILayout.EndToggleGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginDisabledGroup(!(toggleInWeight || toggleOutWeight));

                if (valueOutWeight >= 0.0f && valueOutWeight <= 1.0f)
                {
                    if (GUILayout.Button("Apply weight"))
                    {
                        UpdateAnimationForObject(clip, WeightToKeyframe());

                        /*if (GameObject.Find(selectedObject.name + " Curve"))
                        {
                            Debug.Log("check 0");
                            UpdateBezierCurve("Update", positionX, positionY, positionZ);
                        } else
                        {
                            UpdateBezierCurve("Create", positionX, positionY, positionZ);

                        }*/

                        if (!isAnimRunning)
                        {
                            animator.Rebind();
                        }
                    }
                }

                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();

            }
        }
        else
        {
            EditorGUILayout.LabelField(errorLog, EditorStyles.boldLabel);
            if (errorLog.Equals("No animator in this object") && HasAnimator())
            {
                EditorGUILayout.LabelField("Target object: " + GameObject.Find(selectedObject.name.Remove(selectedObject.name.IndexOf(" Curve"))).name, EditorStyles.boldLabel);
            }
        }

        if (HasAnimator() && errorLog.Length >= 0)
        {
            if (HasAnimator() && selectedObject.name.Contains(" Curve"))
            {
                if (GUILayout.Button("Apply Beizer curve to animation"))
                {
                    UpdateAnimationForObject(clip, GetBezierCurve());
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            isAnimRunning = EditorGUILayout.BeginToggleGroup("Run animation", isAnimRunning);
            EditorGUILayout.EndToggleGroup();

            if (!isAnimRunning && GUILayout.Button("Reset Object position"))
            {
                animator.Rebind();
            }

        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Delete all curve"))
        {
            GameObject[] gobjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];
            if (gobjects != null && gobjects.Length > 0)
            {
                foreach (var gameObj in gobjects)
                {
                    if (gameObj != null && gameObj.name.Contains("Curve"))
                    {
                        Debug.Log("Deleted " + gameObj.name);
                        DestroyImmediate(gameObj);
                    }
                }
            }

        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Note", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("If there is no Update Curve button, click Reset Object position!!");
        EditorGUILayout.LabelField("Choose the curve to apply curve to animation curve!!");

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Important", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("The single animation curve of positions at a keyframe MUST be the same value!!");
        EditorGUILayout.LabelField("The weight will be not correctly as before when apply curve to animation curve!!");

        Texture2D banner = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/BezierCurve/gearinc.jpg", typeof(Texture2D));
        if (banner != null)
        {
            GUI.DrawTexture(new Rect(0, 0, 600, 150), banner);
        }
        Repaint();
    }

    private void UpdateBezierCurve(string _action, float[] _positionX, float[] _positionY, float[] _positionZ)
    {
        if (_action.Equals("Update"))
        {
            DestroyImmediate(GameObject.Find(selectedObject.name + " Curve"));
        }

        positions = new Vector3[_positionX.Length];
        for (int i = 0; i < _positionX.Length; i++)
        {
            Vector3 position = new Vector3(_positionX[i], _positionY[i], _positionZ[i]);
            positions[i] = position;
        }

        DrawBezierCurve(positions);
    }

    private void DrawBezierCurve(Vector3[] _positions)
    {
        GameObject curveObject = new GameObject(selectedObject.name + " Curve");

        BezierCurve curve = curveObject.AddComponent<BezierCurve>();
        curve.resolution = 200;
        Vector3 lastHandle = Vector3.zero;
        for (int i = 0; i < _positions.Length; i += 3)
        {
            if (i < _positions.Length - 3)
            {
                BezierPoint p = curve.AddPointAt(_positions[i]);
                p.handleStyle = BezierPoint.HandleStyle.Broken;

                p.handle1 = lastHandle;
                p.handle2 = _positions[i + 1];
                lastHandle = _positions[i + 2];

                if (i == 0)
                {
                    Debug.Log(_positions[i + 1]);
                }
            }
        }

        BezierPoint lastP = curve.AddPointAt(_positions[_positions.Length - 1]);

        lastP.handleStyle = BezierPoint.HandleStyle.Broken;
        lastP.handle1 = lastHandle;
        lastP.handle2 = Vector3.zero;
    }

    private List<float>[] GetCurvePoints(AnimationCurve curveList)
    {
        List<float> points = new List<float>();

        for (var i = 0; i < curveList.length - 1; i++)
        {
            var start = curveList[i];
            var end = curveList[i + 1];
            var difference = Mathf.Abs(start.time - end.time);

            var startTangentLength = difference;
            var endTangentLength = difference;

            if (start.weightedMode == WeightedMode.Out || start.weightedMode == WeightedMode.Both)
            { startTangentLength *= start.outWeight; }

            else
            { startTangentLength /= 3f; }

            if (end.weightedMode == WeightedMode.In || end.weightedMode == WeightedMode.Both)
            { endTangentLength *= end.inWeight; }
            else
            { endTangentLength /= 3f; }

            var p1 = start.value;
            //var p2 = end.value;

            var c1 = startTangentLength * start.outTangent;
            var c2 = -endTangentLength * end.inTangent;

            if (start.outTangent == 0)
            {
                c1 = startTangentLength;
            }

            if (end.inTangent == 0)
            {
                c1 = -endTangentLength;
            }

            points.Add(p1);
            points.Add(c1);
            points.Add(c2);
        }

        points.Add(curveList[curveList.length - 1].value);

        List<float>[] list = new List<float>[2];
        list[0] = points;
        return list;
    }
    private bool HasAnimator()
    {
        if (selectedObject && selectedObject.name.Contains(" Curve"))
        {
            if (GameObject.Find(selectedObject.name.Remove(selectedObject.name.IndexOf(" Curve"))) && selectedObject.name.Remove(selectedObject.name.IndexOf(" Curve")).Equals(GameObject.Find(selectedObject.name.Remove(selectedObject.name.IndexOf(" Curve"))).name))
            {
                animator = GameObject.Find(selectedObject.name.Remove(selectedObject.name.IndexOf(" Curve"))).GetComponent<Animator>();
                return true;
            }
        }
        else if (selectedObject && selectedObject.GetComponent<Animator>())
        {
            animator = selectedObject.GetComponent<Animator>();
            return true;
        }

        return false;
    }
    private void ObjectSelectionGetData()
    {
        if (Selection.activeGameObject)
        {
            selectedObject = Selection.activeGameObject;

            if (selectedObject.GetComponent<Animator>())
            {
                Animator _animator;
                if (Selection.activeGameObject.name.Contains(" Curve"))
                {
                    _animator = GameObject.Find(selectedObject.name.Remove(selectedObject.name.IndexOf(" Curve"))).GetComponent<Animator>();
                }
                else
                {
                    _animator = selectedObject.GetComponent<Animator>();
                }

                clipInfos = _animator.GetCurrentAnimatorClipInfo(0);

                if (clipInfos != null && clipInfos.Length > 0)
                {
                    clip = clipInfos[0].clip;
                    if (clip != null)
                    {
                        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

                        for (int i = 0; i < bindings.Length; i++)
                        {
                            var binding = bindings[i];
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                            //Debug.Log(binding.path + "/" + binding.propertyName + ", Keys: " + curve.keys.Length);

                            if (binding.propertyName.Contains("m_LocalPosition.x")) { curvesFromAnimation[0] = curve; }
                            else if (binding.propertyName.Contains("m_LocalPosition.y")) { curvesFromAnimation[1] = curve; }
                            else if (binding.propertyName.Contains("m_LocalPosition.z")) { curvesFromAnimation[2] = curve; }
                        }
                        keyframeCount = curvesFromAnimation[0].length;
                        errorLog = "";
                    }
                    else
                    {
                        errorLog = "No clip in this animator!!!";
                    }
                }
                else
                {
                    errorLog = "No clip in this animator!!!";
                }

                /* array of animation clips 
                 * foreach (var clipInfo in clipInfos)
                {
                    Debug.Log("Starting clip : " + clipInfo.clip.name);
                    clip = clipInfo.clip;
                    // drawcurve();
                }
                //clip = EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), false) as AnimationClip;
                */
                Debug.Log($"animation clip name to modified {clipInfos.Length}");
            }
            else
            {
                errorLog = "No animator in this object";
            }
        }
        else
        {
            errorLog = "No object selected!!!";
        }
    }

    private AnimationCurve[] WeightToKeyframe()
    {
        Keyframe[] ksX = new Keyframe[keyframeCount];
        for (int i = 0; i < ksX.Length; i++)
        {
            ksX[i] = new Keyframe();
            ksX[i].value = curvesFromAnimation[0].keys[i].value;
            ksX[i].time = curvesFromAnimation[0].keys[i].time; ;
            ksX[i].weightedMode = WeightedMode.Both;
            if (toggleOutWeight && indexKeyframe == i)
                ksX[i].outWeight = valueOutWeight;
            else
                ksX[i].outWeight = curvesFromAnimation[0].keys[i].outWeight;
            if (toggleInWeight && indexKeyframe == i)
                ksX[i].inWeight = valueInWeight;
            else
                ksX[i].inWeight = curvesFromAnimation[0].keys[i].inWeight;
            ksX[i].inTangent = curvesFromAnimation[0].keys[i].inTangent;
            ksX[i].outTangent = curvesFromAnimation[0].keys[i].outTangent;
        }
        AnimationCurve curveX = new AnimationCurve(ksX);
        for (int i = 0; i < curveX.length; i++)
        {
            AnimationUtility.SetKeyBroken(curveX, i, true);
        }

        /*Keyframe Y*/

        Keyframe[] ksY = new Keyframe[keyframeCount];
        for (int i = 0; i < ksY.Length; i++)
        {
            ksY[i] = new Keyframe();
            ksY[i].value = curvesFromAnimation[1].keys[i].value;
            ksY[i].time = curvesFromAnimation[1].keys[i].time; ;
            ksY[i].weightedMode = WeightedMode.Both;
            if (toggleOutWeight && indexKeyframe == i)
                ksY[i].outWeight = valueOutWeight;
            else
                ksY[i].outWeight = curvesFromAnimation[1].keys[i].outWeight;
            if (toggleInWeight && indexKeyframe == i)
                ksY[i].inWeight = valueInWeight;
            else
                ksY[i].inWeight = curvesFromAnimation[1].keys[i].inWeight;
            ksY[i].inTangent = curvesFromAnimation[1].keys[i].inTangent;
            ksY[i].outTangent = curvesFromAnimation[1].keys[i].outTangent;
        }

        AnimationCurve curveY = new AnimationCurve(ksY);
        for (int i = 0; i < curveY.length; i++)
        {
            AnimationUtility.SetKeyBroken(curveY, i, true);
        }

        /*Keyframe Z*/

        Keyframe[] ksZ = new Keyframe[keyframeCount];
        for (int i = 0; i < ksZ.Length; i++)
        {
            ksZ[i] = new Keyframe();
            ksZ[i].value = curvesFromAnimation[2].keys[i].value;
            ksZ[i].time = curvesFromAnimation[2].keys[i].time; ;
            ksZ[i].weightedMode = WeightedMode.Both;
            if (toggleOutWeight && indexKeyframe == i)
                ksZ[i].outWeight = valueOutWeight;
            else
                ksZ[i].outWeight = curvesFromAnimation[2].keys[i].outWeight;
            if (toggleInWeight && indexKeyframe == i)
                ksZ[i].inWeight = valueInWeight;
            else
                ksZ[i].inWeight = curvesFromAnimation[2].keys[i].inWeight;
            ksZ[i].inTangent = curvesFromAnimation[2].keys[i].inTangent;
            ksZ[i].outTangent = curvesFromAnimation[2].keys[i].outTangent;
        }

        AnimationCurve curveZ = new AnimationCurve(ksZ);
        for (int i = 0; i < curveZ.length; i++)
        {
            AnimationUtility.SetKeyBroken(curveZ, i, true);
        }

        AnimationCurve[] _curve = new AnimationCurve[3];
        _curve[0] = curveX;
        _curve[1] = curveY;
        _curve[2] = curveZ;

        return _curve;
    }

    private void UpdateAnimationForObject(AnimationClip _targetClip, AnimationCurve[] _curves)
    {
        EditorCurveBinding[] bindings = new EditorCurveBinding[3];
        bindings[0].propertyName = "m_LocalPosition.x";
        bindings[1].propertyName = "m_LocalPosition.y";
        bindings[2].propertyName = "m_LocalPosition.z";
        bindings[0].type = typeof(Transform);
        bindings[1].type = typeof(Transform);
        bindings[2].type = typeof(Transform);
        AnimationUtility.SetEditorCurves(_targetClip, bindings, _curves);

        Debug.Log("animation curve updated");
    }
    private AnimationCurve[] GetBezierCurve()
    {
        BezierCurve bezierCurve = selectedObject.GetComponent<BezierCurve>();
        int pointCounts = bezierCurve.pointCount;
        tangentX = new Vector4[pointCounts - 1];
        tangentY = new Vector4[pointCounts - 1];
        tangentZ = new Vector4[pointCounts - 1];
        //Debug.Log("bezierCurve.pointCount: " + bezierCurve.pointCount);
        for (int i = 0; i < pointCounts - 1; i++)
        {
            tangentX[i] = GetTangent(curvesFromAnimation[0].keys[i + 1].time - curvesFromAnimation[0].keys[i].time, bezierCurve[i].position.x, bezierCurve[i].handle2.x, bezierCurve[i + 1].handle1.x, bezierCurve[i + 1].position.x);
            tangentY[i] = GetTangent(curvesFromAnimation[1].keys[i + 1].time - curvesFromAnimation[1].keys[i].time, bezierCurve[i].position.y, bezierCurve[i].handle2.y, bezierCurve[i + 1].handle1.y, bezierCurve[i + 1].position.y);
            tangentZ[i] = GetTangent(curvesFromAnimation[2].keys[i + 1].time - curvesFromAnimation[2].keys[i].time, bezierCurve[i].position.z, bezierCurve[i].handle2.z, bezierCurve[i + 1].handle1.z, bezierCurve[i + 1].position.z);
        }

        //Debug.Log($"getdiff outtangent: {tangentX[1].x} - outweight: {tangentX[1].z}" );

        Keyframe[] ksX = new Keyframe[pointCounts];
        float lastTangent;
        float lastWeight;
        lastTangent = 0f;
        lastWeight = 0f;
        for (int i = 0; i < tangentX.Length; i++)
        {
            ksX[i] = new Keyframe();
            ksX[i].value = bezierCurve[i].position.x;
            ksX[i].time = curvesFromAnimation[0].keys[i].time; ;
            ksX[i].weightedMode = WeightedMode.Both;
            ksX[i].inWeight = lastWeight;
            ksX[i].inTangent = lastTangent;
            ksX[i].outWeight = tangentX[i].z;
            ksX[i].outTangent = tangentX[i].x;
            lastTangent = tangentX[i].y;
            lastWeight = tangentX[i].w;
        }
        Debug.Log($"check_length curve: {ksX.Length}");
        ksX[pointCounts - 1] = new Keyframe();
        ksX[pointCounts - 1].value = bezierCurve[pointCounts - 1].position.x;
        ksX[pointCounts - 1].time = curvesFromAnimation[0].keys[pointCounts - 1].time;
        ksX[pointCounts - 1].weightedMode = WeightedMode.Both;
        ksX[pointCounts - 1].inTangent = lastTangent;
        ksX[pointCounts - 1].inWeight = lastWeight;
        AnimationCurve curveX = new AnimationCurve(ksX);
        for (int i = 0; i < curveX.length; i++)
        {
            AnimationUtility.SetKeyBroken(curveX, i, true);
        }

        Keyframe[] ksY = new Keyframe[pointCounts];
        lastTangent = 0f;
        lastWeight = 0f;
        for (int i = 0; i < tangentY.Length; i++)
        {
            ksY[i] = new Keyframe();
            ksY[i].value = bezierCurve[i].position.y;
            ksY[i].time = curvesFromAnimation[1].keys[i].time;
            ksY[i].weightedMode = WeightedMode.Both;
            ksY[i].inWeight = lastWeight;
            ksY[i].inTangent = lastTangent;
            ksY[i].outWeight = tangentY[i].z;
            ksY[i].outTangent = tangentY[i].x;
            lastTangent = tangentY[i].y;
            lastWeight = tangentY[i].w;
        }
        ksY[pointCounts - 1] = new Keyframe();
        ksY[pointCounts - 1].value = bezierCurve[pointCounts - 1].position.y;
        ksY[pointCounts - 1].time = curvesFromAnimation[1].keys[pointCounts - 1].time;
        ksY[pointCounts - 1].weightedMode = WeightedMode.Both;
        ksY[pointCounts - 1].inTangent = lastTangent;
        ksY[pointCounts - 1].inWeight = lastWeight;
        AnimationCurve curveY = new AnimationCurve(ksY);
        for (int i = 0; i < curveY.length; i++)
        {
            AnimationUtility.SetKeyBroken(curveY, i, true);
        }

        Keyframe[] ksZ = new Keyframe[pointCounts];
        lastTangent = 0f;
        lastWeight = 0f;
        for (int i = 0; i < tangentZ.Length; i++)
        {
            ksZ[i] = new Keyframe();
            ksZ[i].value = bezierCurve[i].position.z;
            ksZ[i].time = curvesFromAnimation[2].keys[i].time; ;
            ksZ[i].weightedMode = WeightedMode.Both;
            ksZ[i].inWeight = lastWeight;
            ksZ[i].inTangent = lastTangent;
            ksZ[i].outWeight = tangentZ[i].z;
            ksZ[i].outTangent = tangentZ[i].x;
            lastTangent = tangentZ[i].y;
            lastWeight = tangentZ[i].w;
        }
        ksZ[pointCounts - 1] = new Keyframe();
        ksZ[pointCounts - 1].value = bezierCurve[pointCounts - 1].position.z;
        ksZ[pointCounts - 1].time = curvesFromAnimation[2].keys[pointCounts - 1].time; ;
        ksZ[pointCounts - 1].weightedMode = WeightedMode.Both;
        ksZ[pointCounts - 1].inTangent = lastTangent;
        ksZ[pointCounts - 1].inWeight = lastWeight;
        AnimationCurve curveZ = new AnimationCurve(ksZ);
        for (int i = 0; i < curveZ.length; i++)
        {
            AnimationUtility.SetKeyBroken(curveZ, i, true);
        }

        AnimationCurve[] _curve = new AnimationCurve[3];
        _curve[0] = curveX;
        _curve[1] = curveY;
        _curve[2] = curveZ;

        return _curve;
    }

    public Vector4 GetTangent(float deltaTime, float a, float b, float c, float d)
    {

        if (b == 0)
        {
            b = .00001f;
        }
        if (c == 0)
        {
            c = .00001f;
        }

        float t = 0;

        float CalculateTangent(float t)
        {
            return -3 * (1 - t) * (1 - t) * a + 3 * (1 - t) * (1 - t) * (b + a) - 6 * t * (1 - t) * (b + a)
                                                - 3 * t * t * (c + d) + 6 * t * (1 - t) * (c + d) + 3 * t * t * d;
        }

        float outTangent = CalculateTangent(t);
        float inTangent = 3 * (-c); // CalculateTangent(t+1f);

        float outWeight;
        float inWeight;
        if (outTangent == Mathf.Infinity)
        {
            outWeight = 0;
        }
        else if (outTangent == 0)
        {
            outWeight = Mathf.Abs(b / deltaTime);
        }
        else
        {
            outWeight = Mathf.Abs(b / (deltaTime * outTangent));
        }

        if (inTangent == Mathf.Infinity)
        {
            inWeight = 0;
        }
        else if (inTangent == 0)
        {
            inWeight = Mathf.Abs(c / deltaTime);
        }
        else
        {
            inWeight = Mathf.Abs(c / (deltaTime * inTangent));
        }

        Vector4 tangent = new Vector4(outTangent, inTangent, outWeight, inWeight);
        //Debug.Log($" outtangent: {outTangent} / outweight: {outWeight} - intangent: {inTangent} / inweight: {inWeight} - a: {a} - b: {b} - c: {c} - d: {d}");

        return tangent;
    }
}