using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class MovementStuff : MonoBehaviour
{
	// Start is called once before the first execution of Update after the MonoBehaviour is created

	public int swag;

	void Start()
	{
		swag = 0;
	}

	// Update is called once per frame
	void Update()
	{
		Trace.WriteLine("This is a trace message.");
	}
}
