using UnityEngine;
using Phobia.Audio.Vis;

namespace Phobia.debug.Audio.Vis
{
	public class VisualizerDebug : MonoBehaviour
	{
		private void OnDrawGizmos()
		{
			PhobiaVis vis = GetComponent<PhobiaVis>();
			if (vis == null)
			{
				return;
			}

			// Draw center point
			Gizmos.color = Color.cyan;
			Gizmos.DrawSphere(transform.position, 0.2f);

			// Draw radius circle if config is available
			if (vis.Config != null)
			{
				DrawGizmoCircle(transform.position, vis.Config.radius, 32, Color.green);

				// Draw band count info
				Gizmos.color = Color.yellow;
				for (int i = 0; i < vis.Config.bandCount; i++)
				{
					float angle = i * Mathf.PI * 2 / vis.Config.bandCount;
					Vector3 pos = transform.position + new Vector3(
						Mathf.Cos(angle) * vis.Config.radius,
						Mathf.Sin(angle) * vis.Config.radius,
						0
					);
					Gizmos.DrawSphere(pos, 2f);
				}
			}
		}

		private void DrawGizmoCircle(Vector3 center, float radius, int segments, Color color)
		{
			Gizmos.color = color;

			Vector3 prevPoint = center + new Vector3(radius, 0, 0);
			for (int i = 1; i <= segments; i++)
			{
				float angle = (float)i / segments * Mathf.PI * 2;
				Vector3 nextPoint = center + new Vector3(
					Mathf.Cos(angle) * radius,
					0,
					Mathf.Sin(angle) * radius
				);

				Gizmos.DrawLine(prevPoint, nextPoint);
				prevPoint = nextPoint;
			}
		}
	}
}
