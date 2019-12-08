using UnityEngine;

public class LogButtons : MonoBehaviour
{
	public void OnClickError() => Debug.LogError("Error");
	public void OnClickException() => Debug.LogException(new System.Exception());
	public void OnClickWarning() => Debug.LogWarning("Warning");
	public void OnClickAssert() => Debug.Assert(false);
	public void OnClickLog() => Debug.Log("Log");
}
