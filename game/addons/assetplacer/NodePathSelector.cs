// NodePathSelector.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System;
using System.Diagnostics;
using Godot;
using Godot.Collections;

namespace AssetPlacer;

[Tool]
public partial class NodePathSelector : Node
{
    private string _nodePathSaveKey;
    private const string NullPath = "@#@null";
    
    private EditorInterface _editorInterface;
    private NodePathSelectorUi _nodePathSelectorUi;
    private Node _node;
    private NodePath _nodePath;
    private Node _sceneRoot;
    private bool _initialized = false;
    public Node Node
    {
        get { return _node;}
        private set => _node = value;
    }

    public void Init(EditorInterface editorInterface, string nodePathSaveKey)
    {
        this._editorInterface = editorInterface;
        this._nodePathSaveKey = nodePathSaveKey;
    }

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint()) return;
        
        try
        {
            if (Node != null)
            {
                if (Node.IsInsideTree())
                {
                    var path = Node.GetTree().EditedSceneRoot.GetPathTo(Node);
                    if (!_nodePath.Equals(path))
                    {
                        SetNodePath(Node);
                    }

                    if (path == "." && Node.Name != _nodePathSelectorUi._selectNodeButton.Text)
                    {
                        SetNodePath(Node);
                    }
                } else if (_initialized)
                {
                    SetNodePath(null);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            Node = null;
        }
    }

    public void SetUi(NodePathSelectorUi nodePathSelectorUi)
    {
        _nodePathSelectorUi = nodePathSelectorUi;
        _nodePathSelectorUi._selectNodeButton.NodeDropped += SetNodePath;
        _nodePathSelectorUi._selectNodeButton.Pressed += SelectNode;
        _nodePathSelectorUi._setSelectedButton.Pressed += SetSelected;
    }

    
    private void SetSelected()
    {
        var selectedNodes = _editorInterface.GetSelection().GetSelectedNodes();
        if (selectedNodes.Count == 1 && selectedNodes[0] != Node)
        {
            SetNodePath(selectedNodes[0]);
            _nodePathSelectorUi?.SetSelectedButtonDisabled(true);
        }
    }
    
    private void SelectNode()
    {
        if (Node == null || !Node.IsInsideTree()) return;
        _editorInterface.GetSelection().Clear();
        _editorInterface.GetSelection().AddNode(Node);
    }
    
    
    public void SetNodePath(Node node)
    {
        Node = node;
        Texture2D spawnNodeIcon = null;
        if (node != null && IsValidType(node))
        {
            Debug.Assert(_sceneRoot != null && _sceneRoot.IsInsideTree());
            Debug.Assert(node.IsInsideTree());
            _nodePath = _sceneRoot.GetPathTo(node);

            var nodeName = Node.GetClass().Split('.')[^1];
            spawnNodeIcon = _editorInterface.GetBaseControl().GetThemeIcon(nodeName, "EditorIcons");
            if (spawnNodeIcon == _editorInterface.GetBaseControl().GetThemeIcon("notavalidiconname", "EditorIcons")) // File Broken icon
            {
                spawnNodeIcon = _editorInterface.GetBaseControl().GetThemeIcon("Node", "EditorIcons");
            }
            _nodePathSelectorUi.SetNode(Node, spawnNodeIcon);
        }
        else
        {
            _nodePath = NullPath;
            Node = null;
            _nodePathSelectorUi.SetNode(null, null);
        }
        AssetPlacerPersistence.StoreSceneData(_nodePathSaveKey, _nodePath);
    }

    private bool IsValidType(Node node)
    {
        if (string.IsNullOrEmpty(_nodePathSelectorUi.classType)) return true;
        return node.GetClass() == _nodePathSelectorUi.classType;
    }

    // The scene has changed i.e., user has closed the scene, or opened a new one
    public void OnSceneChanged(Node newRoot)
    {
        // load the new spawn parent
        string path = null;
        if (newRoot != null)
        {
            path = AssetPlacerPersistence.LoadSceneData(_nodePathSaveKey, new NodePath("."), Variant.Type.NodePath).AsNodePath();
        }
        _sceneRoot = newRoot;
        if(!_initialized) _initialized = _sceneRoot != null;
        if(_initialized) UpdateNodePath(path);
    }

    public void OnSelectionChanged(Array<Node> selectedNodes)
    {
        _nodePathSelectorUi?.SetSelectedButtonDisabled(selectedNodes.Count != 1 || selectedNodes[0] == Node);
    }
    
    // The scene root changed, i.e. a different node has been made the root, or the scene was changed
    public void SetSceneRoot(Node root)
    {
        _sceneRoot = root;
        if(!_initialized) _initialized = _sceneRoot != null;
        if(_initialized) UpdateNodePath(_nodePath);
    }

    private void UpdateNodePath(string path)
    {
        if (_sceneRoot == null)
        {
            // no root in the scene -> no nodes
            SetNodePath(null);
            _nodePath = null; // this is to prevent the path==NullPath, thus when a root will be added, this will be the new spawn node.
            return;
        }
        
        if (path == NullPath)
        {
            // path == NullPath: the old node could not be found anymore
            SetNodePath(null);
            return;
        }
        
        // Check if spawnNode is still in the scene
        try
        {
            if (Node != null && Node.IsInsideTree())
            {
                SetNodePath(Node); // Updates the path
                return;
            }
        }
        catch (ObjectDisposedException)
        {
            Node = null;
        }

        // Update spawn parent
        // Check if the path is valid
        var spawnNode = _sceneRoot.GetNodeOrNull(path);
        if (spawnNode != null)
        {
            SetNodePath(spawnNode);
            return;
        }
        
        // Root exists, a path was loaded, but it doesn't lead to a valid node and the current Node is invalid
        // e.g. when the spawn parent was the root and it was deleted (and the scene change is not triggered first)
        // e.g. when you have an empty scene and you add a root for the first time.
        if(_nodePathSelectorUi.defaultAssignRoot) SetNodePath(_sceneRoot);
    }
}

#endif