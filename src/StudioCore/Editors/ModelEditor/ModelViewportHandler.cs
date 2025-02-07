﻿using HKLib.hk2018.hkHashMapDetail;
using ImGuiNET;
using Microsoft.AspNetCore.Mvc.Rendering;
using SoulsFormats;
using StudioCore.Configuration;
using StudioCore.Editors.MapEditor;
using StudioCore.Editors.ModelEditor.Actions;
using StudioCore.Interface;
using StudioCore.MsbEditor;
using StudioCore.Resource;
using StudioCore.Resource.Locators;
using StudioCore.Resource.Types;
using StudioCore.Scene;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Veldrid.Utilities;

namespace StudioCore.Editors.ModelEditor
{
    public class ModelViewportHandler
    {
        public ModelEditorScreen Screen;
        public IViewport Viewport;

        public MeshRenderableProxy _Flver_RenderMesh;
        public MeshRenderableProxy _LowCollision_RenderMesh;
        public MeshRenderableProxy _HighCollision_RenderMesh;

        public ResourceHandle<FlverResource> _flverhandle;
        public ResourceHandle<HavokCollisionResource> _lowCollisionHandle;
        public ResourceHandle<HavokCollisionResource> _highCollisionHandle;

        public string ContainerID;

        public ModelViewportHandler(ModelEditorScreen screen, IViewport viewport)
        {
            Screen = screen;
            Viewport = viewport;
            ContainerID = "";
        }

        public bool IsUpdatingViewportModel = false;
        public bool IgnoreHierarchyFocus = false;

        public void OnResourceLoaded(IResourceHandle handle, int tag)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            // Collision
            if (handle is ResourceHandle<HavokCollisionResource>)
            {
                var colHandle = (ResourceHandle<HavokCollisionResource>)handle;

                if (colHandle.AssetVirtualPath.Contains("_h"))
                {
                    _highCollisionHandle = (ResourceHandle<HavokCollisionResource>)handle;
                    _highCollisionHandle.Acquire();
                }
                if (colHandle.AssetVirtualPath.Contains("_l"))
                {
                    _lowCollisionHandle = (ResourceHandle<HavokCollisionResource>)handle;
                    _lowCollisionHandle.Acquire();
                }
            }

            // FLVER
            if (handle is ResourceHandle<FlverResource>)
            {
                _flverhandle = (ResourceHandle<FlverResource>)handle;
                _flverhandle.Acquire();

                if (_Flver_RenderMesh != null)
                {
                    BoundingBox box = _Flver_RenderMesh.GetBounds();
                    Viewport.FrameBox(box);

                    Vector3 dim = box.GetDimensions();
                    var mindim = Math.Min(dim.X, Math.Min(dim.Y, dim.Z));
                    var maxdim = Math.Max(dim.X, Math.Max(dim.Y, dim.Z));

                    var minSpeed = 1.0f;
                    var basespeed = Math.Max(minSpeed, (float)Math.Sqrt(mindim / 3.0f));
                    Viewport.WorldView.CameraMoveSpeed_Normal = basespeed;
                    Viewport.WorldView.CameraMoveSpeed_Slow = basespeed / 10.0f;
                    Viewport.WorldView.CameraMoveSpeed_Fast = basespeed * 10.0f;

                    Viewport.NearClip = Math.Max(0.001f, maxdim / 10000.0f);
                }

                if (_flverhandle.IsLoaded && _flverhandle.Get() != null)
                {
                    var currentFlverClone = Screen.ResourceHandler.GetCurrentFLVER().Clone();
                    var currentInfo = Screen.ResourceHandler.LoadedFlverContainer;

                    FlverResource r = _flverhandle.Get();
                    if (r.Flver != null)
                    {
                        Screen._universe.UnloadModels();

                        Screen._universe.LoadFlverInModelEditor(currentFlverClone, currentInfo.ContainerName, _Flver_RenderMesh, _LowCollision_RenderMesh, _HighCollision_RenderMesh);

                        ContainerID = Screen.ResourceHandler.LoadedFlverContainer.ContainerName;
                    }
                }

                if (CFG.Current.Viewport_Enable_Texturing)
                {
                    Screen._universe.ScheduleTextureRefresh();
                }
            }
        }

        public void OnResourceUnloaded(IResourceHandle handle, int tag)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            if (handle is ResourceHandle<FlverResource>)
            {
                _flverhandle = null;
            }

            if (handle is ResourceHandle<HavokCollisionResource>)
            {
                var colHandle = (ResourceHandle<HavokCollisionResource>)handle;

                if (colHandle.AssetVirtualPath.Contains("_h"))
                {
                    _highCollisionHandle = (ResourceHandle<HavokCollisionResource>)handle;
                    _highCollisionHandle.Acquire();
                }
                if (colHandle.AssetVirtualPath.Contains("_l"))
                {
                    _lowCollisionHandle = (ResourceHandle<HavokCollisionResource>)handle;
                    _lowCollisionHandle.Acquire();
                }
            }
        }

        public void UpdateRepresentativeModel(int selectionIndex)
        {
            IsUpdatingViewportModel = true;

            Screen._selection.ClearSelection();

            UpdateRepresentativeModel();

            if (Screen.ModelHierarchy._lastSelectedEntry == ModelEntrySelectionType.Dummy)
            {
                SelectViewportDummy(selectionIndex, Screen._universe.LoadedModelContainers[ContainerID].DummyPoly_RootNode);
            }
            if (Screen.ModelHierarchy._lastSelectedEntry == ModelEntrySelectionType.Node)
            {
                SelectViewportDummy(selectionIndex, Screen._universe.LoadedModelContainers[ContainerID].Bone_RootNode);
            }
            if (Screen.ModelHierarchy._lastSelectedEntry == ModelEntrySelectionType.Mesh)
            {
                SelectViewportDummy(selectionIndex, Screen._universe.LoadedModelContainers[ContainerID].Mesh_RootNode);
            }

            IsUpdatingViewportModel = false;
        }

        private void SelectViewportDummy(int selectIndex, Entity rootNode)
        {
            if (selectIndex != -1)
            {
                int idx = 0;
                foreach (var entry in rootNode.Children)
                {
                    if (idx == selectIndex)
                    {
                        Screen._selection.AddSelection(entry);
                    }
                    idx++;
                }
            }
        }
        public void UpdateRepresentativeModel()
        {
            _flverhandle.Acquire();

            if (_flverhandle.IsLoaded && _flverhandle.Get() != null)
            {
                var currentFlverClone = Screen.ResourceHandler.GetCurrentFLVER().Clone();
                var currentInfo = Screen.ResourceHandler.LoadedFlverContainer;

                Screen._universe.UnloadTransformableEntities(true);
                Screen._universe.LoadFlverInModelEditor(currentFlverClone, currentInfo.ContainerName, _Flver_RenderMesh, _LowCollision_RenderMesh, _HighCollision_RenderMesh);
            }
        }

        /// <summary>
        /// Updated the viewport FLVER model render mesh
        /// </summary>
        public void UpdateRenderMesh(ResourceDescriptor modelAsset)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            if (Universe.IsRendering)
            {
                if (_Flver_RenderMesh != null)
                {
                    _Flver_RenderMesh.Dispose();
                }

                _Flver_RenderMesh = MeshRenderableProxy.MeshRenderableFromFlverResource(Screen.RenderScene, modelAsset.AssetVirtualPath, ModelMarkerType.None, null);
                _Flver_RenderMesh.World = Matrix4x4.Identity;
            }
        }

        /// <summary>
        /// Updated the viewport FLVER model render collision mesh
        /// </summary>
        public void UpdateRenderMeshCollision(ResourceDescriptor collisionAsset)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            if (Universe.IsRendering)
            {
                // High Collision
                if (collisionAsset.AssetVirtualPath.Contains("_h"))
                {
                    if (_HighCollision_RenderMesh != null)
                    {
                        _HighCollision_RenderMesh.Dispose();
                    }

                    _HighCollision_RenderMesh = MeshRenderableProxy.MeshRenderableFromCollisionResource(Screen.RenderScene, collisionAsset.AssetVirtualPath, ModelMarkerType.None, collisionAsset.AssetVirtualPath);
                    _HighCollision_RenderMesh.World = Matrix4x4.Identity;
                }

                // Low Collision
                if (collisionAsset.AssetVirtualPath.Contains("_l"))
                {
                    if (_LowCollision_RenderMesh != null)
                    {
                        _LowCollision_RenderMesh.Dispose();
                    }

                    _LowCollision_RenderMesh = MeshRenderableProxy.MeshRenderableFromCollisionResource(Screen.RenderScene, collisionAsset.AssetVirtualPath, ModelMarkerType.None, collisionAsset.AssetVirtualPath);
                    _LowCollision_RenderMesh.World = Matrix4x4.Identity;
                }
            }
        }

        public void UpdateRepresentativeDummy(int index, Vector3 position)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.DummyPoly_RootNode.Children.Count - 1)
                return;

            var curNode = container.DummyPoly_RootNode.Children[index];
            DummyPositionChange act = new(curNode, position);
            Screen.EditorActionManager.ExecuteAction(act);
        }

        public void UpdateRepresentativeNode(int index, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.Bone_RootNode.Children.Count - 1)
                return;

            var curNode = container.Bone_RootNode.Children[index];
            BoneTransformChange act = new(curNode, position, rotation, scale);
            Screen.EditorActionManager.ExecuteAction(act);
        }

        public void SelectRepresentativeDummy(int index, HierarchyMultiselect multiSelect)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.DummyPoly_RootNode.Children.Count - 1)
                return;

            if (multiSelect.HasValidMultiselection())
            {
                Screen._selection.ClearSelection();

                foreach (var entry in multiSelect.StoredIndices)
                {
                    var curNode = container.DummyPoly_RootNode.Children[entry];
                    IgnoreHierarchyFocus = true;
                    Screen._selection.AddSelection(curNode);
                }
            }
            else
            {

                var curNode = container.DummyPoly_RootNode.Children[index];
                IgnoreHierarchyFocus = true;
                Screen._selection.ClearSelection();
                Screen._selection.AddSelection(curNode);
            }
        }

        public void SelectRepresentativeNode(int index)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.Bone_RootNode.Children.Count - 1)
                return;

            var curNode = container.Bone_RootNode.Children[index];
            IgnoreHierarchyFocus = true;
            Screen._selection.ClearSelection();
            Screen._selection.AddSelection(curNode);
        }
        public void SelectRepresentativeMesh(int index)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.Mesh_RootNode.Children.Count - 1)
                return;

            var curNode = container.Mesh_RootNode.Children[index];
            IgnoreHierarchyFocus = true;
            Screen._selection.ClearSelection();
            Screen._selection.AddSelection(curNode);
        }

        public void DisplayRepresentativeDummyState(int index)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.DummyPoly_RootNode.Children.Count - 1)
                return;

            Entity curEntity = null;

            var curNode = container.DummyPoly_RootNode.Children[index];
            curEntity = curNode;

            if (curEntity != null)
            {
                ImGui.SetItemAllowOverlap();
                var isVisible = curEntity.EditorVisible;
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - 18.0f * DPI.GetUIScale());
                ImGui.PushStyleColor(ImGuiCol.Text, isVisible
                    ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                    : new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                ImGui.TextWrapped(isVisible ? ForkAwesome.Eye : ForkAwesome.EyeSlash);
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    // Quick-tool all if this key is down
                    if (InputTracker.GetKey(KeyBindings.Current.MODEL_ToggleVisibility))
                    {
                        for (int i = 0; i < container.DummyPoly_RootNode.Children.Count; i++)
                        {
                            Screen.ViewportHandler.ToggleRepresentativeDummy(i);
                        }
                    }
                    // Otherwise just toggle this row
                    else
                    {
                        Screen.ViewportHandler.ToggleRepresentativeDummy(index);
                    }
                }
            }
        }

        public void ToggleRepresentativeDummy(int index)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.DummyPoly_RootNode.Children.Count - 1)
                return;

            var curNode = container.DummyPoly_RootNode.Children[index];
            curNode.EditorVisible = !curNode.EditorVisible;
        }

        public void DisplayRepresentativeNodeState(int index)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.Bone_RootNode.Children.Count - 1)
                return;

            Entity curEntity = null;

            var curNode = container.Bone_RootNode.Children[index];
            curEntity = curNode;

            if (curEntity != null)
            {
                ImGui.SetItemAllowOverlap();
                var isVisible = curEntity.EditorVisible;
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - 18.0f * DPI.GetUIScale());
                ImGui.PushStyleColor(ImGuiCol.Text, isVisible
                    ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                    : new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                ImGui.TextWrapped(isVisible ? ForkAwesome.Eye : ForkAwesome.EyeSlash);
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    // Quick-tool all if this key is down
                    if (InputTracker.GetKey(KeyBindings.Current.MODEL_ToggleVisibility))
                    {
                        for (int i = 0; i < container.Bone_RootNode.Children.Count; i++)
                        {
                            Screen.ViewportHandler.ToggleRepresentativeNode(i);
                        }
                    }
                    // Otherwise just toggle this row
                    else
                    {
                        Screen.ViewportHandler.ToggleRepresentativeNode(index);
                    }
                }
            }
        }

        public void ToggleRepresentativeNode(int index)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.Bone_RootNode.Children.Count - 1)
                return;

            var curNode = container.Bone_RootNode.Children[index];
            curNode.EditorVisible = !curNode.EditorVisible;
        }
        public void DisplayRepresentativeMeshState(int index)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            if (Screen._universe.LoadedModelContainers.Count <= 0)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.Mesh_RootNode.Children.Count - 1)
                return;

            Entity curEntity = null;

            var curNode = container.Mesh_RootNode.Children[index];
            curEntity = curNode;

            if(curEntity != null)
            {
                ImGui.SetItemAllowOverlap();
                var isVisible = curEntity.EditorVisible;
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - 18.0f * DPI.GetUIScale());
                ImGui.PushStyleColor(ImGuiCol.Text, isVisible
                    ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                    : new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                ImGui.TextWrapped(isVisible ? ForkAwesome.Eye : ForkAwesome.EyeSlash);
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    // Quick-tool all if this key is down
                    if (InputTracker.GetKey(KeyBindings.Current.MODEL_ToggleVisibility))
                    {
                        for (int i = 0; i < container.Mesh_RootNode.Children.Count; i++)
                        {
                            Screen.ViewportHandler.ToggleRepresentativeMesh(i);
                        }
                    }
                    // Otherwise just toggle this row
                    else
                    {
                        Screen.ViewportHandler.ToggleRepresentativeMesh(index);
                    }
                }
            }
        }

        public void ToggleRepresentativeMesh(int index)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            var container = Screen._universe.LoadedModelContainers[ContainerID];

            if (index > container.Mesh_RootNode.Children.Count - 1)
                return;

            var curNode = container.Mesh_RootNode.Children[index];
            curNode.EditorVisible = !curNode.EditorVisible;
        }

        public void OnRepresentativeEntitySelected(Entity ent)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            if (!IsSelectableNode(ent))
                return;

            if (IsUpdatingViewportModel)
                return;

            // Dummies
            if (ent.WrappedObject is FLVER.Dummy)
            {
                TransformableNamedEntity transformEnt = (TransformableNamedEntity)ent;

                Screen.ModelHierarchy._lastSelectedEntry = ModelEntrySelectionType.Dummy;
                Screen.ModelHierarchy._selectedDummy = transformEnt.Index;

                if (IgnoreHierarchyFocus)
                {
                    IgnoreHierarchyFocus = false;
                }
                else
                {
                    Screen.ModelHierarchy.FocusSelection = true;
                }
            }
            // Bones
            if (ent.WrappedObject is FLVER.Node)
            {
                TransformableNamedEntity transformEnt = (TransformableNamedEntity)ent;

                Screen.ModelHierarchy._lastSelectedEntry = ModelEntrySelectionType.Node;
                Screen.ModelHierarchy._selectedNode = transformEnt.Index;

                if (IgnoreHierarchyFocus)
                {
                    IgnoreHierarchyFocus = false;
                }
                else
                {
                    Screen.ModelHierarchy.FocusSelection = true;
                }
            }
            // Mesh
            if (ent.WrappedObject is FLVER2.Mesh)
            {
                NamedEntity namedEnt = (NamedEntity)ent;

                Screen.ModelHierarchy._lastSelectedEntry = ModelEntrySelectionType.Mesh;
                Screen.ModelHierarchy._selectedMesh = namedEnt.Index;

                if (IgnoreHierarchyFocus)
                {
                    IgnoreHierarchyFocus = false;
                }
                else
                {
                    Screen.ModelHierarchy.FocusSelection = true;
                }
            }
        }

        public void OnRepresentativeEntityDeselected(Entity ent)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            if (!IsSelectableNode(ent))
                return;

            if (IsUpdatingViewportModel)
                return;
        }

        public void OnRepresentativeEntityUpdate(Entity ent)
        {
            // Required to stop the LowRequirements build from failing
            if (Smithbox.LowRequirementsMode)
                return;

            if (!IsSelectableNode(ent))
                return;

            if (IsUpdatingViewportModel)
                return;

            if(ent.WrappedObject is FLVER.Dummy or FLVER.Node)
            {
                TransformableNamedEntity transformEnt = (TransformableNamedEntity)ent;

                // Dummies
                if (transformEnt.WrappedObject is FLVER.Dummy)
                {
                    UpdateStoredDummyPosition(transformEnt);
                }
                // Bones
                if (transformEnt.WrappedObject is FLVER.Node)
                {
                    UpdateStoredNodeTransform(transformEnt);
                }
            }
        }

        private void UpdateStoredDummyPosition(TransformableNamedEntity transformEnt)
        {
            if (Screen.ModelHierarchy._selectedDummy == -1)
                return;
            
            var dummy = Screen.ResourceHandler.GetCurrentFLVER().Dummies[Screen.ModelHierarchy._selectedDummy];
            var entDummy = (FLVER.Dummy)transformEnt.WrappedObject;

            if(dummy.Position != entDummy.Position)
            {
                dummy.Position = entDummy.Position;
            }
        }

        private void UpdateStoredNodeTransform(TransformableNamedEntity transformEnt)
        {
            if (Screen.ModelHierarchy._selectedNode == -1)
                return;

            var bone = Screen.ResourceHandler.GetCurrentFLVER().Nodes[Screen.ModelHierarchy._selectedNode];
            var entBone = (FLVER.Node)transformEnt.WrappedObject;

            if (bone.Position != entBone.Position)
            {
                bone.Position = entBone.Position;
            }
        }

        public bool IsSelectableNode(Entity ent)
        {
            if(ent is TransformableNamedEntity or NamedEntity)
            {
                return true;
            }

            return false;
        }

        public void OnProjectChanged()
        {
            ContainerID = "";
            Screen._universe.UnloadModels();
            _flverhandle?.Unload();
            _flverhandle = null;
            _Flver_RenderMesh?.Dispose();
            _Flver_RenderMesh = null;
            
        }
    }
}
