using System;
using UnityEngine;

public static class Logger {

	public static bool debugMode = true; // whether to output debug logs

	public static void DebugLog(string name, string str) {
		if (debugMode) {
			Debug.Log ("[" + name + "] " + str); 
		}
	}

	public static void Warning(string name, string str) {
		Debug.LogWarning ("[" + name + "] " + str); 
	}

	public static void Error(string name, string str) {
		Debug.LogError ("[" + name + "] " + str); 
	}
}

public class NamedLogger {

	private string name;

	public NamedLogger(string name) {
		this.name = name;
	}

	public void DebugLog(string str) {
		Logger.DebugLog (name, str);
	}

	public void Warning(string str) {
		Logger.Warning (name, str);
	}

	public void Error(string str) {
		Logger.Error (name, str);
	}
}