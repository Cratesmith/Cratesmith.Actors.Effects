using Cratesmith;
using UnityEngine;


public class EffectLine : Effect
{
	public static Handle<T> Spawn<T>(T effectPrefab, Vector3 to, Transform fromParent, Transform toTransform=null) where T : EffectLine
	{
		return Spawn(effectPrefab, fromParent.transform.position, to, fromParent, toTransform);
	}

	public static Handle<T> Spawn<T>(T effectPrefab, Vector3 from, Vector3 to, Transform fromParent=null, Transform toTransform=null)
		where T : EffectLine
	{
		if (effectPrefab == null) return null;
		var inst = Effect.Spawn(effectPrefab, from, Quaternion.LookRotation(to - from), fromParent);
		var fx = inst.value;
		fx.Init(from, to, toTransform);
		return inst;
	}

	private void Init(Vector3 from, Vector3 to, Transform toTransform)
	{
		this.toTransform = toTransform;
		this.fromWorldPosition = from;
		this.toWorldPosition = to;
	}
	 
	public Handle<Transform>	toTransform			{ get; set; }
	public Vector3				toLocalPosition		{ get; set; }
	
	public Vector3				toWorldPosition		
	{
		get { return toTransform ? toTransform.value.TransformPoint(toLocalPosition):toLocalPosition; } 
		set { toLocalPosition = toTransform ? toTransform.value.InverseTransformPoint(value):value; } 
	}

	public Vector3 fromWorldPosition
	{
		get { return parent.valid ? parent.value.TransformPoint(localPosition):localPosition; } 
		set { localPosition = parent ? parent.value.InverseTransformPoint(value):value; } 		
	}
}