/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using System.IO;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject corner;
    public GameObject straight;
    public GameObject cross4;
    public GameObject cross3;
    public GameObject mountain;

    private char get(string[] lines, int y, int x)
    {
        if (y < 0 || y >= lines.Length)
        {
            return ' ';
        }
        if (x < 0 || x >= lines[y].Length)
        {
            return ' ';
        }
        return lines[y][x];
    }

    void Start ()
    {
        string root = Directory.GetParent(Application.dataPath).ToString();

        string[] lines;
        try
        {
            lines = File.ReadAllLines(Path.Combine(root, "Map.txt"));
        }
        catch (IOException)
        {
            return;
        }

        float scale = 0.0632f;

        float posX = -30.0f * scale;
        float posZ = 20.0f * scale;
        float size = 10.0f * scale;

        for (int y = 0; y < lines.Length; y++)
        {
            for (int x = 0; x < lines[y].Length; x++)
            {
                char ch = lines[y][x];

                GameObject template = null;
                float s = scale;
                float angle = 0;

                if (ch == '|')
                {
                    template = straight;
                    angle = 0;
                }
                else if (ch == '-')
                {
                    template = straight;
                    angle = 90;
                }
                else if (ch == '+')
                {
                    char left = get(lines, y, x - 1);
                    char right = get(lines, y, x + 1);
                    char top = get(lines, y - 1, x);
                    char bottom = get(lines, y + 1, x);

                    if (left == '|' || left == ' ')
                    {
                        template = cross3;
                        angle = 90;
                    }
                    else if (right == '|' || right == ' ')
                    {
                        template = cross3;
                        angle = 270;
                    }
                    else if (top == '-' || top == ' ')
                    {
                        template = cross3;
                        angle = 180;
                    }
                    else if (bottom == '-' || bottom == ' ')
                    {
                        template = cross3;
                        angle = 0;
                    }
                    else
                    {
                        template = cross4;
                        angle = 0;
                    }
                }
                else if (ch == '/')
                {
                    template = corner;

                    char left = get(lines, y, x - 1);
                    char top = get(lines, y - 1, x);

                    if (left != '|' && left != ' ' && top != '-' && top != ' ')
                    {
                        //   |
                        //  -/
                        angle = 180;
                    }
                    else
                    {
                        //  /-
                        //  |
                        angle = 0;
                    }
                }
                else if (ch == '\\')
                {
                    template = corner;

                    char right = get(lines, y, x + 1);
                    char top = get(lines, y - 1, x);

                    if (right != '|' && right != ' ' && top != '-' && top != ' ')
                    {
                        //   |
                        //   \-
                        angle = 270;
                    }
                    else
                    {
                        //  -\
                        //   |
                        angle = 90;
                    }
                }
                else if (ch == '.')
                {
                    template = mountain;
                    angle = Random.Range(0, 4) * 90;
                    s = 1.5f;
                }

                if (template != null)
                {
                    var obj = Instantiate(template,
                        new Vector3(- posX - x * size, 0.0f, posZ + y * size),
                        Quaternion.AngleAxis(angle, Vector3.up));
                    obj.transform.localScale = new Vector3(s, s, s);
                    obj.name = string.Format("map_{0}_{1}", x, y);
                }
            }
        }
    }
}
