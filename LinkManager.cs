using UnityEngine;
using System;
using System.Collections;

public class LinkManager : MonoBehaviour
{
    [SerializeField] private Transform endPoint;
    [SerializeField] private Material cableMaterial;

    [SerializeField] private float cableLength = 0.5f;
    [SerializeField] private int totalSegments = 5;
    [SerializeField] private float segmentsPerUnit = 2f;
    private int segments = 0;
    [SerializeField] private float cableWidth = 0.1f;

    [SerializeField] private int verletIterations = 1;
    [SerializeField] private int solverIterations = 1;

    private LineRenderer line;
    private CableParticle[] points;

    void Start()
    {
        InitCableParticles();
        InitLineRenderer();
    }

    void InitCableParticles()
    {
        if (totalSegments > 0)
            segments = totalSegments;
        else
            segments = Mathf.CeilToInt(cableLength * segmentsPerUnit);

        Vector3 cableDirection = (endPoint.position - transform.position).normalized;
        float initialSegmentLength = cableLength / segments;
        points = new CableParticle[segments + 1];

        for (int pointIdx = 0; pointIdx <= segments; pointIdx++)
        {
            Vector3 initialPosition = transform.position + (cableDirection * (initialSegmentLength * pointIdx));
            points[pointIdx] = new CableParticle(initialPosition);
        }

        CableParticle start = points[0];
        CableParticle end = points[segments];
        start.Bind(this.transform);
        end.Bind(endPoint.transform);
    }

    void InitLineRenderer()
    {
        line = this.gameObject.AddComponent<LineRenderer>();
#pragma warning disable CS0618 
        line.SetWidth(cableWidth, cableWidth);
#pragma warning restore CS0618 
#pragma warning disable CS0618 
        line.SetVertexCount(segments + 1);
#pragma warning restore CS0618 
        line.material = cableMaterial;
        line.GetComponent<Renderer>().enabled = true;
    }

    void Update()
    {
        RenderCable();
    }

    void RenderCable()
    {
        for (int pointIdx = 0; pointIdx < segments + 1; pointIdx++)
        {
            line.SetPosition(pointIdx, points[pointIdx].Position);
        }
    }

    void FixedUpdate()
    {
        for (int verletIdx = 0; verletIdx < verletIterations; verletIdx++)
        {
            VerletIntegrate();
            SolveConstraints();
        }
    }

    void VerletIntegrate()
    {
        Vector3 gravityDisplacement = Time.fixedDeltaTime * Time.fixedDeltaTime * Physics.gravity;
        foreach (CableParticle particle in points)
        {
            particle.UpdateVerlet(gravityDisplacement);
        }
    }

    void SolveConstraints()
    {
        for (int iterationIdx = 0; iterationIdx < solverIterations; iterationIdx++)
        {
            SolveDistanceConstraint();
            SolveStiffnessConstraint();
        }
    }

    void SolveDistanceConstraint()
    {
        float segmentLength = cableLength / segments;
        for (int SegIdx = 0; SegIdx < segments; SegIdx++)
        {
            CableParticle particleA = points[SegIdx];
            CableParticle particleB = points[SegIdx + 1];

            SolveDistanceConstraint(particleA, particleB, segmentLength);
        }
    }

    void SolveDistanceConstraint(CableParticle particleA, CableParticle particleB, float segmentLength)
    {
        Vector3 delta = particleB.Position - particleA.Position;
      
        float currentDistance = delta.magnitude;
        float errorFactor = (currentDistance - segmentLength) / currentDistance;

        if (particleA.IsFree() && particleB.IsFree())
        {
            particleA.Position += errorFactor * 0.5f * delta;
            particleB.Position -= errorFactor * 0.5f * delta;
        }
        else if (particleA.IsFree())
        {
            particleA.Position += errorFactor * delta;
        }
        else if (particleB.IsFree())
        {
            particleB.Position -= errorFactor * delta;
        }
    }

    void SolveStiffnessConstraint()
    {
        float distance = (points[0].Position - points[segments].Position).magnitude;
        if (distance > cableLength)
        {
            foreach (CableParticle particle in points)
            {
                SolveStiffnessConstraint(particle, distance);
            }
        }
    }

}
