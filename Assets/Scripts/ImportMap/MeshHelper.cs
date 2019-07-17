using UnityEngine;
using Util;

public class MeshHelper
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    /*
     * Add Mesh vertices
     */
    private Vector3[] CreateVertices(Vector2[] leftBoundary, Vector2[] rightBoundary)
    {
        int index = 0;
        Vector3[] vertices = new Vector3[leftBoundary.Length + rightBoundary.Length];

        foreach (Vector2 coor in leftBoundary)
        {
            vertices[index] = Tools.CoorExchange(new Vector3(coor.x, coor.y, 0.0f));
            index++;
        }

        foreach (Vector2 coor in rightBoundary)
        {
            vertices[index] = Tools.CoorExchange(new Vector3(coor.x, coor.y, 0.0f));
            index++;
        }

        return vertices;
    }

    /*
     * Add Mesh vertices
     */
    private Vector3[] CreateVertices(Vector2[] points)
    {
        int index = 0;
        Vector3[] vertices = new Vector3[points.Length];

        foreach (Vector2 coor in points)
        {
            // TODO(): Stopping overlapping textures from flickering,
            // set the layer down than another 0.0001f
            vertices[index] = Tools.CoorExchange(new Vector3(coor.x, coor.y, -0.0001f));
            index++;
        }

        return vertices;
    }

    /*
     * Add UV to fill the road
     * 
     */
    private Vector2[] CreateUV(Vector2[] leftBoundary, Vector2[] rightBoundary)
    {
        int index = 0;
        Vector2[] vertices = new Vector2[leftBoundary.Length + rightBoundary.Length];

        while (index < leftBoundary.Length)
        {
            vertices[index] = (index % 2 == 0) ? new Vector2(0, 1) : new Vector2(0, 0);
            index++;
        }

        while (index < leftBoundary.Length + rightBoundary.Length)
        {
            vertices[index] = (index - leftBoundary.Length) % 2 == 0 ? 
                new Vector2(1, 0) : new Vector2(1, 1);
            index++;
        }

        return vertices;
    }

    /*
     *      ----     
     * |   |    |   |
     *  ----     ----
     * The polygon must be in this order, or will render issues
     */
    private Vector2[] CreateUV(Vector2[] points)
    {
        int index = 0;
        Vector2[] vertices = new Vector2[points.Length];

        //TODO(): check the polygon is 4 points
        if (points.Length % 4 != 0) return vertices;

        while (index < points.Length)
        {
            vertices[index] = new Vector2(0, 0);
            vertices[index + 1] = new Vector2(0, 1);
            vertices[index + 2] = new Vector2(1, 1);
            vertices[index + 3] = new Vector2(1, 0);
            index += 4;
        }

        return vertices;
    }

    /*
     * Add Mesh triangles
     */
    private int[] CreateTriangles(Vector2[] leftBoundary, Vector2[] rightBoundary)
    {
        if (leftBoundary.Length == 0 || rightBoundary.Length == 0)
        {
            return null;
        }

        int minLength = leftBoundary.Length > rightBoundary.Length ? rightBoundary.Length : leftBoundary.Length;

        int index = 0;
        int[] triangles = new int[3 * (leftBoundary.Length + rightBoundary.Length)];

        /*
         * Add the triangle, first solve the same number of points on both sides
         */
        for (int i = 0; i < minLength - 1; i++)
        {
            triangles[index] = i;
            triangles[index + 1] = i + 1;
            triangles[index + 2] = leftBoundary.Length + i;
            triangles[index + 3] = i + 1;
            triangles[index + 4] = leftBoundary.Length + i + 1;
            triangles[index + 5] = leftBoundary.Length + i;
            index += 6;
        }

        /*
         * Add the res triangle, solve the extra points
         */
        if (leftBoundary.Length > rightBoundary.Length)
        {
            for (int i = 0; i < leftBoundary.Length - rightBoundary.Length; i++)
            {
                triangles[index] = rightBoundary.Length - 1 + i;
                triangles[index + 1] = rightBoundary.Length + i;
                triangles[index + 2] = leftBoundary.Length + rightBoundary.Length - 1;
                index += 3;
            }
        }
        else
        {
            for (int i = 0; i < rightBoundary.Length - leftBoundary.Length; i++)
            {
                triangles[index] = leftBoundary.Length - 1;
                triangles[index + 1] = 2 * leftBoundary.Length + i;
                triangles[index + 2] = 2 * leftBoundary.Length + i - 1;
                index += 3;
            }
        }

        return triangles;
    }


    private int[] CreateTriangles(Vector2[] points)
    {
        Triangulator tr = new Triangulator(points);
        return tr.Triangulate();
    }

    /*
     * All road is quadrilateral 
     */
    public Mesh GetMesh(Boundary boundary)
    {
        Mesh mesh = new Mesh
        {
            vertices = CreateVertices(boundary.left, boundary.right),
            uv = CreateUV(boundary.left, boundary.right),
            triangles = CreateTriangles(boundary.left, boundary.right)
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }


    public Mesh GetMesh(Polygon polygon)
    {
        Mesh mesh = new Mesh
        {
            vertices = CreateVertices(polygon.points),
            uv = CreateUV(polygon.points),
            triangles = CreateTriangles(polygon.points)
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
