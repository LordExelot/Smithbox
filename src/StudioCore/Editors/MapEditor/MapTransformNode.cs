﻿using System.Numerics;

namespace StudioCore.Editors.MapEditor;

/// <summary>
///     A simple transform node that can be a parent to map objects
///     to transform them
/// </summary>
public class MapTransformNode
{
    public MapTransformNode()
    {
    }

    public MapTransformNode(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; } = Vector3.One;
}
