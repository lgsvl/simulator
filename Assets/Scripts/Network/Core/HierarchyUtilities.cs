/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Network.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Utility methods for Unity hierarchy management
    /// </summary>
    public static class HierarchyUtilities
    {
        /// <summary>
        /// Nodes separator in the relative path
        /// </summary>
        public const char RelativePathSeparator = '/';

        /// <summary>
        /// Gets path in hierarchy of the given transform
        /// </summary>
        /// <param name="transform">Transform for which path is built</param>
        /// <returns>Path in hierarchy of the given transform</returns>
        public static string GetPath(Transform transform)
        {
            var sb = new StringBuilder();
            var nodesNames = new List<string>();
            var node = transform;
            while (node != null)
            {
                nodesNames.Add(node.name);
                node = node.parent;
            }

            for (var i = nodesNames.Count - 1; i >= 0; i--)
            {
                sb.Append(nodesNames[i]);
                sb.Append(RelativePathSeparator);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets relative path in hierarchy between parent and his child
        /// </summary>
        /// <param name="parent">Parent's transform</param>
        /// <param name="child">Child's transform</param>
        /// <returns>Relative path in hierarchy between parent and his child</returns>
        public static string GetRelativePath(Transform parent, Transform child)
        {
            if (parent == child)
                return "";
            var sb = new StringBuilder();
            var nodesNames = new List<string>();
            var node = child;
            while (node != parent)
            {
                nodesNames.Add(node.name);
                node = node.parent;
                if (node == null)
                    throw new ArgumentException("Child must be inside the parent's hierarchy.");
            }

            for (var i = nodesNames.Count - 1; i >= 0; i--)
            {
                sb.Append(nodesNames[i]);
                sb.Append(RelativePathSeparator);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets or create child transforms in the parent according to given path
        /// </summary>
        /// <param name="parent"> </param>
        /// <param name="relativePath"></param>
        /// <returns>Child transforms in the parent according to given path</returns>
        public static Transform GetOrCreateChild(Transform parent, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return parent;
            var currentNode = parent;
            var nodeNames = relativePath.Split(RelativePathSeparator);
            var creatingChildren = false;
            for (var i = 0; i < nodeNames.Length; i++)
            {
                var nodeName = nodeNames[i];
                Transform child;
                if (creatingChildren || (child = currentNode.Find(nodeName)) == null)
                {
                    creatingChildren = true;
                    child = (new GameObject(nodeName)).transform;
                    child.SetParent(currentNode);
                    child.localPosition = Vector3.zero;
                    child.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                }

                currentNode = child;
            }

            return currentNode;
        }

        /// <summary>
        /// Changes given game object name to unique inside it's parent
        /// </summary>
        /// <param name="gameObject">Game object which will gain unique name</param>
        /// <returns>True if name has changed, false otherwise</returns>
        public static bool ChangeToUniqueName(GameObject gameObject)
        {
            var parent = gameObject.transform.parent;
            var siblingsCount = parent.childCount;
            var nameTaken = false;
            var maxId = 0;
            for (var i = 0; i < siblingsCount; i++)
            {
                var sibling = parent.GetChild(i);
                if (sibling == gameObject.transform)
                    continue;
                if (!sibling.name.StartsWith(gameObject.name)) continue;
                if (sibling.name.Length == gameObject.name.Length)
                    nameTaken = true;
                else
                {
                    //Find the currently used max identifier with game object name
                    var nameLen = gameObject.name.Length;
                    if (int.TryParse(sibling.name.Substring(nameLen, sibling.name.Length - nameLen), out var siblingId))
                        maxId = siblingId + 1;
                }
            }

            if (nameTaken)
                gameObject.name = $"{gameObject.name}{maxId}";
            return nameTaken;
        }
    }
}