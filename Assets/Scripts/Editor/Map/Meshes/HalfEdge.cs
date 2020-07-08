/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapMeshes
{
    public class HalfEdge
    {
        public Vertex vertex;

        public Triangle triangle;

        public HalfEdge previous;
        public HalfEdge next;
        public HalfEdge opposite;

        public HalfEdge(Vertex vertex)
        {
            this.vertex = vertex;
        }

        public void Flip()
        {
            var eNext = next;
            var ePrev = previous;
            var eOpp = opposite;
            var eOppNext = opposite.next;
            var eOppPrev = opposite.previous;

            var v0 = vertex;
            var v1 = next.vertex;
            var v2 = previous.vertex;
            var v3 = opposite.next.vertex;

            next = ePrev;
            previous = eOppNext;
            eNext.next = eOpp;
            eNext.previous = eOppPrev;
            ePrev.next = eOppNext;
            ePrev.previous = this;

            eOpp.next = eOppPrev;
            eOpp.previous = eNext;
            eOppNext.next = this;
            eOppNext.previous = ePrev;
            eOppPrev.next = eNext;
            eOppPrev.previous = eOpp;

            vertex = v1;
            eNext.vertex = v1;
            ePrev.vertex = v2;
            eOpp.vertex = v3;
            eOppNext.vertex = v3;
            eOppPrev.vertex = v0;

            var tri0 = triangle;
            var tri1 = eOpp.triangle;

            triangle = tri0;
            ePrev.triangle = tri0;
            eOppNext.triangle = tri0;

            eNext.triangle = tri1;
            eOpp.triangle = tri1;
            eOppPrev.triangle = tri1;

            tri0.v0 = v1;
            tri0.v1 = v2;
            tri0.v2 = v3;

            tri1.v0 = v1;
            tri1.v1 = v3;
            tri1.v2 = v0;
        }
    }
}