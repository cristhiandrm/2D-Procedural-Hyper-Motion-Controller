using System.Collections.Generic;
using UnityEngine;

public class CapePhysics : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El Rigidbody2D del jugador para simular el 'viento'")]
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Tooltip("El punto de anclaje de la capa (ej. la nuca del jugador)")]
    [SerializeField] private Transform anchor;

    [Header("Configuración de la Capa")]
    [SerializeField] private int segmentCount = 10;
    [SerializeField] private float segmentLength = 0.1f;
    [SerializeField] private int stiffnessIterations = 8;

    [Header("Físicas")]
    [SerializeField] private Vector2 gravity = new Vector2(0, -15f);
    [SerializeField] private float damping = 0.98f;
    [SerializeField] private float windFactor = 0.1f;

    [Header("Colisión")]
    [Tooltip("La capa de colisión del entorno (ej. 'Ground')")]
    [SerializeField] private LayerMask collisionLayer;
    [Tooltip("El 'grosor' de cada punto de la capa para la colisión")]
    [SerializeField] private float segmentRadius = 0.05f;

    private LineRenderer lineRenderer;
    private List<CapeSegment> segments = new List<CapeSegment>();
    private Vector3 wind;

    public class CapeSegment
    {
        public Vector3 currentPos;
        public Vector3 lastPos;

        public CapeSegment(Vector3 pos)
        {
            this.currentPos = pos;
            this.lastPos = pos;
        }
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("¡Se necesita un LineRenderer!");
            return;
        }

        Vector3 startPos = anchor.position;
        for (int i = 0; i < segmentCount; i++)
        {
            segments.Add(new CapeSegment(startPos));
            startPos.y -= segmentLength;
        }

        lineRenderer.positionCount = segmentCount;
    }

    void FixedUpdate()
    {
        wind = playerRigidbody.linearVelocity * -1 * windFactor;
        Simulate();
        HandleCollisions(); 

        for (int i = 0; i < stiffnessIterations; i++)
        {
            ApplyConstraints();
        }
    }

    void LateUpdate()
    {
        DrawCape();
    }

    private void Simulate()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            CapeSegment segment = segments[i];
            Vector3 velocity = (segment.currentPos - segment.lastPos) * damping;
            segment.lastPos = segment.currentPos;

            segment.currentPos += velocity;
            segment.currentPos += (Vector3)gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
            segment.currentPos += wind * Time.fixedDeltaTime;

            segments[i] = segment;
        }
    }

    // --- ¡FUNCIÓN CORREGIDA! ---
    private void HandleCollisions()
    {
        for (int i = 1; i < segmentCount; i++)
        {
            CapeSegment segment = segments[i];

            Collider2D hit = Physics2D.OverlapCircle(segment.currentPos, segmentRadius, collisionLayer);

            if (hit != null)
            {
                Vector2 closestPoint = hit.ClosestPoint((Vector2)segment.currentPos);

                Vector2 normal = (Vector2)segment.currentPos - closestPoint;

                if (normal.sqrMagnitude < 0.0001f)
                {
                    normal = Vector2.up;
                }
                else
                {
                    normal = normal.normalized;
                }

                Vector2 newPos = closestPoint + normal * segmentRadius;

                segment.currentPos = new Vector3(newPos.x, newPos.y, segment.currentPos.z);
            }
        }
    }

    private void ApplyConstraints()
    {
        segments[0].currentPos = anchor.position;

        for (int i = 0; i < segmentCount - 1; i++)
        {
            CapeSegment segA = segments[i];
            CapeSegment segB = segments[i + 1];

            Vector3 delta = segB.currentPos - segA.currentPos;
            float dist = delta.magnitude;

            if (dist == 0) dist = 0.001f;

            float error = (dist - segmentLength) / dist;
            Vector3 correction = delta * 0.5f * error;

            if (i != 0) 
            {
                segA.currentPos += correction;
            }
            segB.currentPos -= correction;
        }
    }

    private void DrawCape()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            lineRenderer.SetPosition(i, segments[i].currentPos);
        }
    }
}