﻿/**********************************************************
*Author: wangjiaying
*Date: 2016.7.6
*Func:
**********************************************************/
using UnityEngine;
using CryStory.Runtime;
using Event = UnityEngine.Event;
using System.Collections.Generic;
using UnityEditor;
using System;

namespace CryStory.Editor
{
    public class NodeEditor<T> : Singleton<T> where T : class, new()
    {

        protected StoryEditorWindow _window;
        protected Rect _contentRect;

        protected Vector2 _leftScrollPosition;

        protected Vector2 _mouseDownPos = Vector2.zero;
        protected bool _mouseIsDown = false;
        protected Vector2 _mouseDownCenter = Vector2.zero;

        protected NodeModifier _currentNode;
        protected Rect _currentNodeRect;
        protected NodeModifier _currentHover;
        protected bool _isConnecting = false;
        public void OnGUI(StoryEditorWindow window, NodeModifier[] nodes)
        {
            _contentRect = window._contentRect;
            _window = window;

            ShowGraphCenterInfo();

            //========Right Area===============
            DrawRightGrid();
            DrawNodes(nodes, true);
            ShowConnectLine();
            //========Left Slider Area===========
            ShowLeftSliderArea();
            //Make graph dragable
            DragGraph();

            InternalOnGUI();
        }

        private void ShowConnectLine()
        {
            if (_isConnecting && _currentNode != null)
            {
                Vector2 pos1 = new Vector2(_currentNodeRect.max.x, _currentNodeRect.max.y - Tools._nodeHalfHeight);
                Tools.DrawBazier(pos1, Event.current.mousePosition);
                if (Tools.MouseUp)
                {
                    _isConnecting = false;
                    if (_currentNode == _currentHover) return;
                    if (Tools.IsValidMouseAABB(Tools.GetNodeRect(CalcRealPosition(_currentHover._position))))
                    {
                        //强制链接单节点
                        if (Event.current.control)
                        {
                            _currentNode.AddNextNode(_currentHover);
                            return;
                        }

                        if (!_currentHover.CanSetParent(_currentNode))
                        {
                            //if (_currentNode.IsParent(_currentHover))
                            if (!_currentNode.HaveParentNodeInNext())
                            {
                                _currentNode.AddNextNode(_currentHover);
                                return;
                            }

                            EditorUtility.DisplayDialog("Error", "Not allow connect to twice parent! You must break one connect with parent and then change it.", "OK");
                            return;
                        }

                        if (NodeModifier.SetParent(_currentHover, _currentNode))
                            OnLinkNode(_currentNode, _currentHover);
                    }
                }
            }
        }

        private void ShowGraphCenterInfo()
        {
            GUI.Label(new Rect(_contentRect.x, _contentRect.y, 120, 20), _window.CurrentContentCenter.ToString());

            if (GUI.Button(new Rect(_contentRect.x + 3, _contentRect.y + 20, 50, 22), "Reset", ResourcesManager.GetInstance.skin.button))
            {
                _window.CurrentContentCenter = Vector2.zero;
            }
        }


        protected void DragNodeEvent(NodeModifier node, Rect nodeRect)
        {
            #region Drag Event
            if (Event.current != null)
            {
                if (Event.current.button == 0)
                {
                    if (Event.current.type == EventType.MouseDown && Tools.IsValidMouseAABB(nodeRect))
                    {
                        _mouseIsDown = true;
                        _mouseDownPos = Event.current.mousePosition;
                        _mouseDownCenter = node._position;
                        _currentNode = node;
                        _currentNodeRect = nodeRect;
                    }

                    if (Event.current.type == EventType.MouseUp)
                    {
                        _mouseIsDown = false;
                    }

                    if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && _mouseIsDown)
                    {
                        //Debug.Log(Event.current.button);
                        Vector2 offset = Event.current.mousePosition - _mouseDownPos;

                        _currentNode._position = _mouseDownCenter + offset;

                        //Vector2 contentPos = Node2Content(newPos);
                        //if (contentPos.x < _contentRect.x)
                        //{
                        //    _currentMission._position = Content2Node(new Vector2(_contentRect.x, contentPos.y));
                        //}
                        //else _currentMission._position = newPos;
                    }
                }
            }
            #endregion
        }

        private void ShowLeftSliderArea()
        {
            Rect background = new Rect(0, _window._topHeight, _window._leftWidth, _window._windowRect.height);
            GUI.Box(background, "", ResourcesManager.GetInstance.StyleBackground);

            _leftScrollPosition = GUI.BeginScrollView(new Rect(0, _window._topHeight, _window._leftWidth, _window._windowRect.height - _window._topHeight), _leftScrollPosition, new Rect(0, _window._topHeight, _window._leftWidth - 30, _window._windowRect.height - _window._topHeight), false, true, ResourcesManager.GetInstance.skin.horizontalScrollbar, ResourcesManager.GetInstance.skin.verticalScrollbar);

            _heightLeft = _window._topHeight + 5;

            if (_currentNode != null)
            {
                #region Modify Name
                EditorGUI.LabelField(GetGUILeftScrollAreaRect(50, 18, false), "Name: ");
                Rect editNameRect = GetGUILeftScrollAreaRect(50, 150, 18);// new Rect(50, _heightLeft, 150, 18);

                string oldName = _currentNode._name;
                EditorGUI.BeginChangeCheck();
                _currentNode._name = EditorGUI.TextField(editNameRect, _currentNode._name);
                if (EditorGUI.EndChangeCheck())
                {
                    OnNodeNameChange(_currentNode, oldName);
                }

                if (Event.current != null)
                {
                    if (Event.current.button == 0)
                    {
                        //Debug.Log(Event.current.type);
                        if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.Ignore)
                        {
                            //Debug.Log(Event.current.type);
                            GUI.FocusControl("Name: ");
                        }
                    }
                }
                #endregion

                #region Next Nodes
                NodeModifier[] nodes = _currentNode.NextNodes;
                if (nodes.Length > 0)
                {
                    LeftHeightSpace(5);
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.white;
                    style.fontSize = 14;
                    EditorGUI.LabelField(GetGUILeftScrollAreaRect(_window._leftWidth, 30), "Next Nodes:", style);
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        Rect rect = GetGUILeftScrollAreaRect(_window._leftWidth, 20, false);
                        EditorGUI.LabelField(rect, nodes[i]._name);
                        if (GUI.Button(GetGUILeftScrollAreaRect(_window._leftWidth - 68f, 50f, 20f), "<color=red>Delete</color>", ResourcesManager.GetInstance.skin.button))
                        {
                            if (nodes[i].Parent == _currentNode)
                                NodeModifier.SetToDefaltContent(nodes[i]);
                            else _currentNode.Remove(nodes[i]);
                            break;
                        }
                    }
                }
                #endregion

                LeftHeightSpace(10);

                DrawLeftArribute(_currentNode);
            }


            GUI.EndScrollView();
        }

        private float _heightLeft;
        protected Rect GetGUILeftScrollAreaRect(float width, float height, bool addHeight = true)
        {
            Rect rect = new Rect(0, _heightLeft, width, height);
            if (addHeight)
                _heightLeft += height;
            return rect;
        }

        protected Rect GetGUILeftScrollAreaRect(float leftPos, float width, float height, bool addHeight = true)
        {
            Rect rect = new Rect(leftPos, _heightLeft, width, height);
            if (addHeight)
                _heightLeft += height;
            return rect;
        }

        protected void LeftHeightSpace(float height)
        {
            _heightLeft += height;
        }

        private void DrawRightGrid()
        {
            Rect coord = new Rect();
            Vector2 nodepos = CalcVirtualPosition(Vector2.zero);
            coord.x = nodepos.x / 128.0f;
            coord.y = nodepos.y / 128.0f;
            coord.width = _contentRect.width / 128.0f;
            coord.height = -_contentRect.height / 128.0f;
            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.DrawTextureWithTexCoords(_contentRect, ResourcesManager.GetInstance.texGrid, coord);
            GUI.color = Color.white;
        }

        private void DragGraph()
        {
            if (Event.current != null)
            {
                if (Event.current.button != 2) return;

                if (Event.current.type == EventType.MouseDown)
                {
                    _mouseDownPos = Event.current.mousePosition;
                    _mouseDownCenter = _window.CurrentContentCenter;
                }

                if (Event.current.type == EventType.MouseDrag)
                {
                    //_window.Repaint();
                    //                    Debug.Log("Window Center:" + _window._story._mission.graphCenter + "  mousePos:" +
                    //Event.current.mousePosition);
                    Vector2 offset = Event.current.mousePosition - _mouseDownPos;
                    _window.CurrentContentCenter = _mouseDownCenter - offset;
                }
            }
        }


        protected void DrawDebugBazierLine(NodeModifier node)
        {
            //NodeModifier node0 = node.Parent;
            //if (node0 != null)
            //{
            //    Rect nodeRect1 = Tools.GetNodeRect(CalcRealPosition(node0._position));
            //    Vector2 pos1 = new Vector2(nodeRect1.max.x, nodeRect1.max.y - Tools._nodeHalfHeight);
            //    Vector2 pos2 = CalcRealPosition(new Vector2(node._position.x, node._position.y + Tools._nodeHalfHeight));

            //    Tools.DrawBazier(pos1, pos2, Color.blue, Color.cyan, 0.1f, 10f);

            //    //DrawDebugBazierLine(node.Parent);
            //}
            NodeModifier[] nextNodes = node.NextNodes;
            for (int j = 0; j < nextNodes.Length; j++)
            {
                NodeModifier node2 = nextNodes[j];
                Rect nodeRect1 = Tools.GetNodeRect(CalcRealPosition(node._position));
                Vector2 pos1 = new Vector2(nodeRect1.max.x, nodeRect1.max.y - Tools._nodeHalfHeight);
                Vector2 pos2 = CalcRealPosition(new Vector2(node2._position.x, node2._position.y + Tools._nodeHalfHeight));

                Tools.DrawBazier(pos1, pos2, Color.magenta, new Color32(0, 255, 255, 180), 0.1f, 10f);
            }
        }

        //Virtual Method===========================

        protected virtual void InternalOnGUI() { }

        protected virtual void DrawNodes(NodeModifier[] nodeList, bool coreNode = false)
        {
            for (int i = 0; i < nodeList.Length; i++)
            {
                NodeModifier node = nodeList[i];
                Rect nodeRect = DrawNodeRect(node, coreNode);

                //Draw conection bazier line
                DrawBazierLine(node);
                //Draw Node
                DrawNodeSlot(node, nodeRect);

                DrawNodes(node.GetNextNodes(node));

                if (_isConnecting) continue;

                DragNodeEvent(node, nodeRect);
            }
        }

        protected virtual void OnLinkNode(NodeModifier parent, NodeModifier child) { }

        protected virtual void OnNodeNameChange(NodeModifier node, string oldName) { }

        protected virtual void DrawBazierLine(NodeModifier node)
        {
            NodeModifier[] nextNodes = node.NextNodes;
            for (int j = 0; j < nextNodes.Length; j++)
            {
                NodeModifier node2 = nextNodes[j];
                Rect nodeRect1 = Tools.GetNodeRect(CalcRealPosition(node._position));
                Vector2 pos1 = new Vector2(nodeRect1.max.x, nodeRect1.max.y - Tools._nodeHalfHeight);
                Vector2 pos2 = CalcRealPosition(new Vector2(node2._position.x, node2._position.y + Tools._nodeHalfHeight));

                bool singleNode = node2.Parent != node;//node.IsParent(node2);
                if (singleNode)
                    Tools.DrawBazier(pos1, pos2, Color.green);
                else Tools.DrawBazier(pos1, pos2);
            }
        }

        protected virtual Rect DrawNodeRect(NodeModifier node, bool coreNode = false)
        {
            Vector2 pos = CalcRealPosition(node._position);
            Rect nodeRect = Tools.GetNodeRect(pos);

            //GUI.Box(nodeRect, coreNode? "<color=#00FF00>" + node._name+"</color>": node._name, _currentNode == node ? (coreNode ? ResourcesManager.GetInstance.CoreNodeOn : ResourcesManager.GetInstance.NodeOn) : (coreNode ? ResourcesManager.GetInstance.CoreNode : ResourcesManager.GetInstance.Node));
            GUI.Box(nodeRect, coreNode ? "<color=#00FF00>" + node._name + "</color>" : node._name, _currentNode == node ? ResourcesManager.GetInstance.NodeOn : ResourcesManager.GetInstance.Node);

            DrawRunModeLable(node, nodeRect);


            return nodeRect;
        }

        protected virtual void DrawRunModeLable(NodeModifier node, Rect nodeRect)
        {
            Rect rect = new Rect(nodeRect);
            rect.position = new Vector2(rect.position.x + (rect.width / 2 - 5), rect.position.y - 15);
            GUIStyle style = new GUIStyle();
            switch (node.RunMode)
            {
                case EnumRunMode.DirectRuning:
                    GUI.Box(rect, "<color=#00BFFF>→</color>", style);
                    break;
                case EnumRunMode.UntilSuccess:
                    GUI.Box(rect, "<color=#00FF00>✔</color>", style);
                    break;
                case EnumRunMode.ReturnParentNode:
                    GUI.Box(rect, "<color=#FFFF00>←</color>", style);
                    break;
                case EnumRunMode.StopNodeList:
                    GUI.Box(rect, "<color=red>✘</color>", style);
                    break;
            }
        }

        protected virtual void DrawNodeSlot(NodeModifier node, Rect nodeRect)
        {
            #region Event drag connection line
            if (Event.current != null)
            {
                if (Tools.IsValidMouseAABB(nodeRect))
                {
                    _currentHover = node;

                    Rect leftRect = Tools.CalcLeftLinkRect(nodeRect);
                    Rect rightRect = Tools.CalcRightLinkRect(nodeRect);
                    GUIStyle inStyle = new GUIStyle();
                    GUIStyle outStyle = new GUIStyle();
                    inStyle.alignment = TextAnchor.MiddleLeft;
                    outStyle.alignment = TextAnchor.MiddleRight;

                    GUI.Box(leftRect, node.Parent == null ? ResourcesManager.GetInstance.texInputSlot : ResourcesManager.GetInstance.texInputSlotActive, inStyle);
                    GUI.Box(rightRect, node.NextNodes.Length < 1 ? ResourcesManager.GetInstance.texOutputSlot : ResourcesManager.GetInstance.texOutputSlotActive, outStyle);

                    //Connect
                    if (Event.current.type == EventType.MouseDown && Tools.IsValidMouseAABB(rightRect))
                    {
                        _isConnecting = true;
                        _currentNode = node;
                        _currentNodeRect = nodeRect;
                    }
                    //ReConnect
                    if (Event.current.type == EventType.MouseDown && Tools.IsValidMouseAABB(leftRect))
                    {
                        if (node.HaveParent)
                        {
                            _isConnecting = true;
                            _currentNode = node.Parent as NodeModifier;
                            Vector2 realPos = CalcRealPosition(_currentNode._position);
                            _currentNodeRect = Tools.GetNodeRect(realPos);
                            NodeModifier.SetToDefaltContent(node);
                            //node.DeleteParent();
                        }
                    }
                }
            }
            #endregion
        }

        protected virtual void DrawLeftArribute(object o)
        {
            #region Attribute

            System.Type type = o.GetType();
            System.Reflection.FieldInfo[] fileds = type.GetFields();

            for (int i = 0; i < fileds.Length; i++)
            {
                System.Reflection.FieldInfo filed = fileds[i];
                if (filed.Name.StartsWith("_")) continue;

                LeftHeightSpace(5);

                Vector2 size = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).label.CalcSize(new GUIContent(filed.Name));
                EditorGUI.LabelField(GetGUILeftScrollAreaRect(size.x, size.y, size.x > 60), filed.Name);
                Rect rect = GetGUILeftScrollAreaRect(60, 150, 18);


                if (!DrawAttribute(filed, rect, o))
                    DrawNormalValue(filed, rect, o);
            }
            #endregion
        }

        private bool DrawAttribute(System.Reflection.FieldInfo filed, Rect rect, object o)
        {
            ValueNameSelectAttribute selectVarName = Attribute.GetCustomAttribute(filed, typeof(ValueNameSelectAttribute)) as ValueNameSelectAttribute;
            if (selectVarName != null)
            {
                Mission mission = _currentNode.GetContentNode() as Mission;
                if (mission == null) return false;
                string[] keys = selectVarName.GetValueNameList(mission);
                int index = Array.FindIndex<string>(keys, (k) => k == (string)filed.GetValue(o));
                if (index == -1) index = 0;
                index = EditorGUI.Popup(rect, index, keys);
                if (keys.Length > 0)
                    filed.SetValue(o, keys[index]);
                return true;
            }
            return false;
        }

        private void DrawNormalValue(System.Reflection.FieldInfo filed, Rect rect, object o)
        {
            switch (filed.FieldType.ToString())
            {
                case "System.Single":
                    filed.SetValue(o, EditorGUI.FloatField(rect, (float)filed.GetValue(o)));
                    break;
                case "System.Int32":
                    filed.SetValue(o, EditorGUI.IntField(rect, (int)filed.GetValue(o)));
                    break;
                case "System.String":
                    filed.SetValue(o, EditorGUI.TextField(rect, (string)filed.GetValue(o)));
                    break;
                case "System.Boolean":
                    filed.SetValue(o, EditorGUI.Toggle(rect, (bool)filed.GetValue(o)));
                    break;
                case "System.String[]":
                    string[] array = filed.GetValue(o) as string[];
                    rect = GetGUILeftScrollAreaRect(175, 20, 18);
                    LeftHeightSpace(6);
                    if (GUI.Button(rect, "+", ResourcesManager.GetInstance.skin.button))
                        Array.Resize<string>(ref array, array.Length + 1);
                    for (int j = 0; j < array.Length; j++)
                    {
                        rect = GetGUILeftScrollAreaRect(10, 160, 18, false);
                        array[j] = EditorGUI.TextField(rect, (string)array[j]);
                        rect = GetGUILeftScrollAreaRect(175, 20, 18);
                        if (GUI.Button(rect, "-", ResourcesManager.GetInstance.skin.button))
                        {
                            for (int n = j; n < array.Length - 1; n++)
                            {
                                array[j] = array[j + 1];
                            }
                            Array.Resize<string>(ref array, array.Length - 1);
                            break;
                        }
                        LeftHeightSpace(2);
                    }
                    filed.SetValue(o, array);
                    break;
                default:
                    if (filed.FieldType.BaseType.ToString() == "System.Enum")
                    {
                        //System.Enum.Parse(filed.FieldType.DeclaringType, (string)filed.GetValue(_currentNode));
                        filed.SetValue(o, EditorGUI.EnumPopup(rect, (System.Enum)filed.GetValue(o)));
                    }
                    else
                        EditorGUI.LabelField(rect, "Not Deal Object.");
                    break;
            }
        }
        //Tool====================================
        protected Vector2 CalcVirtualPosition(Vector2 pos)
        {
            Vector2 center = _contentRect.size * 0.5f;
            center.x = (int)(center.x);
            center.y = (int)(center.y);

            return pos + _window.CurrentContentCenter - center;
        }

        protected Vector2 CalcRealPosition(Vector2 pos)
        {
            Vector2 center = _contentRect.size * 0.5f;
            center.x = (int)(center.x);
            center.y = (int)(center.y);

            return center + pos - _window.CurrentContentCenter;
        }
    }
}