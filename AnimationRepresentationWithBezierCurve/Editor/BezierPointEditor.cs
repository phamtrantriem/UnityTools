using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(BezierPoint))]
[CanEditMultipleObjects]
public class BezierPointEditor : Editor {
	
	BezierPoint point;
	
	SerializedProperty handleTypeProp;
	SerializedProperty handle1Prop;
	SerializedProperty handle2Prop;
	
	private delegate void HandleFunction(BezierPoint p);
	private HandleFunction[] handlers = new HandleFunction[] { HandleConnected, HandleBroken, HandleAbsent };
	
	void OnEnable(){
		point = (BezierPoint)target;
		
		handleTypeProp = serializedObject.FindProperty("handleStyle");
		handle1Prop = serializedObject.FindProperty("_handle1");
		handle2Prop = serializedObject.FindProperty("_handle2");
	}	
	
	public override void OnInspectorGUI (){
		
		serializedObject.Update();
		
		BezierPoint.HandleStyle newHandleType = (BezierPoint.HandleStyle)EditorGUILayout.EnumPopup("Handle Type", (BezierPoint.HandleStyle)handleTypeProp.intValue);

        if (handle1Prop.vector3Value.x == 0)
        {
            handle1Prop.vector3Value = new Vector3(0.00001f, handle1Prop.vector3Value.y, handle1Prop.vector3Value.z);
        }

        if (handle1Prop.vector3Value.y == 0)
        {
            handle1Prop.vector3Value = new Vector3(handle1Prop.vector3Value.x, 0.00001f, handle1Prop.vector3Value.z);
        }

        if (handle1Prop.vector3Value.z == 0)
        {
            handle1Prop.vector3Value = new Vector3(handle1Prop.vector3Value.x, handle1Prop.vector3Value.y, 0.00001f);
        }

        //
        if (handle2Prop.vector3Value.x == 0)
        {
            handle2Prop.vector3Value = new Vector3(0.00001f, handle2Prop.vector3Value.y, handle2Prop.vector3Value.z);
        }

        if (handle2Prop.vector3Value.y == 0)
        {
            handle2Prop.vector3Value = new Vector3(handle2Prop.vector3Value.x, 0.00001f, handle2Prop.vector3Value.z);
        }

        if (handle2Prop.vector3Value.z == 0)
        {
            handle2Prop.vector3Value = new Vector3(handle2Prop.vector3Value.x, handle2Prop.vector3Value.y, 0.00001f);
        }

        if (newHandleType != (BezierPoint.HandleStyle)handleTypeProp.intValue)
		{
			handleTypeProp.intValue = (int)newHandleType;
	
			if((int)newHandleType == 0)
			{
				if(handle1Prop.vector3Value != Vector3.zero) handle2Prop.vector3Value = -handle1Prop.vector3Value;
				else if(handle2Prop.vector3Value != Vector3.zero) handle1Prop.vector3Value = -handle2Prop.vector3Value;
				else
				{
					handle1Prop.vector3Value = new Vector3(0.1f, 0.1f, 0.1f);	
					handle2Prop.vector3Value = new Vector3(-0.1f, -0.1f, -0.1f);	
				}
			}
			
			else if((int)newHandleType == 1)
			{
				if(handle1Prop.vector3Value == Vector3.zero && handle2Prop.vector3Value == Vector3.zero)
				{
					handle1Prop.vector3Value = new Vector3(0.1f, 0.1f, 0.1f);	
					handle2Prop.vector3Value = new Vector3(-0.1f, -0.1f, -0.1f);
				}
			}
			
			else if((int)newHandleType == 2)
			{
				handle1Prop.vector3Value = Vector3.zero;	
				handle2Prop.vector3Value = Vector3.zero;	
			}
		}
		
		if(handleTypeProp.intValue != 2)
		{
			Vector3 newHandle1 = EditorGUILayout.Vector3Field("Handle 1", handle1Prop.vector3Value);
			Vector3 newHandle2 = EditorGUILayout.Vector3Field("Handle 2", handle2Prop.vector3Value);
			
			if(handleTypeProp.intValue == 0){
				if(newHandle1 != handle1Prop.vector3Value){
					handle1Prop.vector3Value = newHandle1;
					handle2Prop.vector3Value = -newHandle1;
				}
				
				else if(newHandle2 != handle2Prop.vector3Value){
					handle1Prop.vector3Value = -newHandle2;
					handle2Prop.vector3Value = newHandle2;			
				}
			}
			
			else{
				handle1Prop.vector3Value = newHandle1;
				handle2Prop.vector3Value = newHandle2;
			}
		}
		
		if(GUI.changed){
			serializedObject.ApplyModifiedProperties();
			EditorUtility.SetDirty(target);
		}
	}
	
	void OnSceneGUI()
	{
		
		Handles.color = Color.green;
		var fmh_96_64_638095513285529364 = point.transform.rotation; Vector3 newPosition = Handles.FreeMoveHandle(point.position, Quaternion.identity, HandleUtility.GetHandleSize(point.position)*0.2f, Vector3.zero, Handles.CubeHandleCap);
		if(point.position != newPosition) point.position = newPosition;
		
		handlers[(int)point.handleStyle](point);
		
		Handles.color = Color.yellow;
		Handles.DrawLine(point.position, point.globalHandle1);
		Handles.DrawLine(point.position, point.globalHandle2);
		
		BezierCurveEditor.DrawOtherPoints(point.curve, point);
	}
	
	private static void HandleConnected(BezierPoint p){
		Handles.color = Color.cyan;
		
		var fmh_111_64_638095513285549498 = p.transform.rotation; Vector3 newGlobal1 = Handles.FreeMoveHandle(p.globalHandle1, Quaternion.identity, HandleUtility.GetHandleSize(p.globalHandle1)*0.15f, Vector3.zero, Handles.SphereHandleCap);
		
		if(newGlobal1 != p.globalHandle1){
			Undo.RegisterCompleteObjectUndo(p, "Move Handle");
			p.globalHandle1 = newGlobal1;
			p.globalHandle2 = -(newGlobal1 - p.position) + p.position;
		}
		
		var fmh_119_64_638095513285551860 = p.transform.rotation; Vector3 newGlobal2 = Handles.FreeMoveHandle(p.globalHandle2, Quaternion.identity, HandleUtility.GetHandleSize(p.globalHandle2)*0.15f, Vector3.zero, Handles.SphereHandleCap);
		
		if(newGlobal2 != p.globalHandle2){
			Undo.RegisterCompleteObjectUndo(p, "Move Handle");
			p.globalHandle1 = -(newGlobal2 - p.position) + p.position;
			p.globalHandle2 = newGlobal2;
		}
	}
	
	private static void HandleBroken(BezierPoint p){
		Handles.color = Color.cyan;

		var fmh_131_64_638095513285554232 = Quaternion.identity; Vector3 newGlobal1 = Handles.FreeMoveHandle(p.globalHandle1, Quaternion.identity, HandleUtility.GetHandleSize(p.globalHandle1)*0.15f, Vector3.zero, Handles.SphereHandleCap);
		var fmh_132_64_638095513285555943 = Quaternion.identity; Vector3 newGlobal2 = Handles.FreeMoveHandle(p.globalHandle2, Quaternion.identity, HandleUtility.GetHandleSize(p.globalHandle2)*0.15f, Vector3.zero, Handles.SphereHandleCap);
		
		if(newGlobal1 != p.globalHandle1)
		{
			Undo.RegisterCompleteObjectUndo(p, "Move Handle");
			p.globalHandle1 = newGlobal1;
		}
		
		if(newGlobal2 != p.globalHandle2)
		{
			Undo.RegisterCompleteObjectUndo(p, "Move Handle");
			p.globalHandle2 = newGlobal2;
		}
	}
	
	private static void HandleAbsent(BezierPoint p)
	{
		p.handle1 = Vector3.zero;
		p.handle2 = Vector3.zero;
	}
}