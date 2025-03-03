using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class VerletRope : MonoBehaviour
{
    [SerializeField] private LineRenderer m_LineRenderer;
    [SerializeField] private Transform m_ConnectedPoint;
    [SerializeField] private Color m_Color;
    [SerializeField] private bool m_DynamicPosition = false;
    [SerializeField] private float m_RopeWidth = 0.25f;
    [SerializeField] private float m_SegmentLength = 1.0f;
    [SerializeField] private float m_OvercorrectionScalar = 1.0f;
    [SerializeField] private float m_WindStrength = 1.0f; //TODO Wind system?
    [SerializeField] private float m_WindScale = 1.0f;
    [SerializeField] private float m_ConstrainStrength = 0.05f;
    [SerializeField] private int m_SegmentsCount = 32;
    [SerializeField] private int m_ConstrainIterations = 64;

    private List<RopeSegment> m_RopeSegments = new List<RopeSegment>();

    private Vector3[] m_TempSegments;
    
    private float m_MaximumError = 1.0f;
    
    private void Start()
    {
        m_RopeSegments.Add(new RopeSegment(transform.position));
        
        for (int i = 1; i < m_SegmentsCount; i++)
        {
            float interpolatedDistance = (float)i / (float)m_SegmentsCount;
            Vector3 segmentPosition = Vector3.Lerp(transform.position, m_ConnectedPoint.transform.position, interpolatedDistance);
            
            m_RopeSegments.Add(new RopeSegment(segmentPosition));
        }
        
        m_RopeSegments.Add(new RopeSegment(m_ConnectedPoint.transform.position));
        
        m_LineRenderer.positionCount = m_RopeSegments.Count;
        
        m_MaximumError = (Vector3.Distance(transform.position, m_ConnectedPoint.transform.position)) / (float)m_RopeSegments.Count;

        m_TempSegments = new Vector3[m_SegmentsCount + 1];
    }

    private void Update()
    {
        //If we're moving the anchors, move the anchors
        if (m_DynamicPosition)
        {
            m_RopeSegments[0].CurrentPosition = transform.position;
            m_RopeSegments[m_RopeSegments.Count - 1].CurrentPosition = m_ConnectedPoint.transform.position;
        }
    
        //Simulate
        for (int i = 1; i < m_RopeSegments.Count - 2; i++)
        {
            RopeSegment segment = m_RopeSegments[i];
            
            Vector3 velocity = segment.CurrentPosition - segment.PreviousPosition;
            segment.PreviousPosition = segment.CurrentPosition;
            segment.CurrentPosition += velocity;
            segment.CurrentPosition += Physics.gravity * Time.fixedDeltaTime;

            Vector3 offset = transform.position + segment.CurrentPosition + new Vector3(Time.time, Time.time, Time.time);
            Vector3 wind = offset * m_WindScale;

            Vector3 direction = m_ConnectedPoint.transform.position - transform.position;
            direction = Vector3.Cross(Vector3.up, direction.normalized);
            
            wind = direction * Mathf.PerlinNoise(wind.x, wind.z) * m_WindStrength;
            segment.CurrentPosition += (wind * 2.0f) - (wind * 0.5f);
        }
        
        //Constrain, connected at both ends
        for (int iteration = 0; iteration < m_ConstrainIterations; iteration++)
        {
            //First Connection
            RopeSegment thisSegment = m_RopeSegments[0];
            RopeSegment otherSegment = m_RopeSegments[1];

            float distance = (otherSegment.CurrentPosition - thisSegment.CurrentPosition).magnitude + m_OvercorrectionScalar;
            float error = distance - m_SegmentLength;

            Vector3 changeDirection = (thisSegment.CurrentPosition - otherSegment.CurrentPosition).normalized;
            Vector3 changeScalar = changeDirection * error;
            
            otherSegment.CurrentPosition += changeScalar * m_ConstrainStrength;
            
            //Segments
            for (int i = 1; i < m_RopeSegments.Count - 2; i++)
            {
                thisSegment = m_RopeSegments[i];
                otherSegment = m_RopeSegments[i + 1];

                distance = (otherSegment.CurrentPosition - thisSegment.CurrentPosition).magnitude + m_OvercorrectionScalar;
                error = distance - m_SegmentLength;

                changeDirection = (thisSegment.CurrentPosition - otherSegment.CurrentPosition).normalized;
                changeScalar = changeDirection * error;

                thisSegment.CurrentPosition -= changeScalar * m_ConstrainStrength;
                otherSegment.CurrentPosition += changeScalar * m_ConstrainStrength;
            }
            
            //Other Connection
            thisSegment = m_RopeSegments[m_RopeSegments.Count - 2];
            otherSegment = m_RopeSegments[m_RopeSegments.Count - 1];

            distance = (otherSegment.CurrentPosition - thisSegment.CurrentPosition).magnitude + m_OvercorrectionScalar;
            error = distance - m_SegmentLength;

            changeDirection = (thisSegment.CurrentPosition - otherSegment.CurrentPosition).normalized;
            changeScalar = changeDirection * error;

            thisSegment.CurrentPosition -= changeScalar * m_ConstrainStrength;
        }

        //Draw
        m_LineRenderer.startColor = m_Color;
        m_LineRenderer.endColor = m_Color;
        
        m_LineRenderer.startWidth = m_RopeWidth;
        m_LineRenderer.endWidth = m_RopeWidth;
        
        for (int i = 0; i < m_RopeSegments.Count; i++)
        {
            m_TempSegments[i] = m_RopeSegments[i].CurrentPosition;
        }

        m_LineRenderer.SetPositions(m_TempSegments);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, 0.25f);
        Gizmos.DrawSphere(m_ConnectedPoint.transform.position, 0.25f);
        Gizmos.DrawLine(transform.position, m_ConnectedPoint.transform.position);
    }

    private class RopeSegment
    { 
        public Vector3 PreviousPosition;
        public Vector3 CurrentPosition;

        public RopeSegment(Vector3 position)
        {
            PreviousPosition = position;
            CurrentPosition = position;
        }
    }

}
