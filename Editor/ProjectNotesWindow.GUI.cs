﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBG.ProjectNotes.Editor
{
    partial class ProjectNotesWindow
    {
        #region Static

        private static bool _toolbarEntryRedDotIconVisible;
        private static Image _toolbarEntryRedDotIcon;
        private static Vector3 _toolbarEntryRedDotIconTransitionScale = new Vector3(1.4f, 1.4f, 1.4f);
        private static float _isToolbarEntryRedDotIconTransitionDuration = 0.8f;
        private static bool _isToolbarEntryRedDotIconTransitToScale = true;


        private static void TryCreateToolbarEntry()
        {
            if (_toolbarEntryRedDotIcon != null)
            {
                return;
            }

            Type toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            object toolbarObj = toolbarType.GetField("get").GetValue(null);
            VisualElement toolbarRoot = (VisualElement)toolbarType.GetField("m_Root",
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(toolbarObj);
            VisualElement toolbarZonePlayMode = toolbarRoot.Q("ToolbarZonePlayMode");

            EditorToolbarButton entryButton = new EditorToolbarButton(Open)
            {
                name = "ProjectNotesButton",
                icon = EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "d_console.infoicon.sml" : "console.infoicon.sml") as Texture2D,
                style =
                    {
                        marginLeft = 8,
                        marginRight = 8,
                    }
            };
            toolbarZonePlayMode.Insert(0, entryButton);

            Image entryIconImage = entryButton.Q<Image>();
            entryIconImage.style.alignItems = Align.FlexEnd;

            _toolbarEntryRedDotIconVisible = false;
            _toolbarEntryRedDotIcon = new Image
            {
                //image = EditorGUIUtility.Load("winbtn_mac_close") as Texture,
                image = EditorGUIUtility.Load("redLight") as Texture,
                style =
                {
                    marginTop = 1,
                    marginRight = -1,
                    width = 7,
                    height = 7,
                    display = _toolbarEntryRedDotIconVisible ? DisplayStyle.Flex : DisplayStyle.None,
                    transitionDuration = new List<TimeValue>
                    {
                        new TimeValue(_isToolbarEntryRedDotIconTransitionDuration)
                    },
                    transitionTimingFunction = new List<EasingFunction>
                    {
                        new EasingFunction(EasingMode.EaseInOut)
                    },
                }
            };
            _toolbarEntryRedDotIcon.RegisterCallback<TransitionEndEvent>(ReverseToolbarEntryRedDotTransition);
            _toolbarEntryRedDotIcon.schedule.Execute(StartToolbarEntryRedDotTransition);
            entryIconImage.Add(_toolbarEntryRedDotIcon);
        }

        private static void StartToolbarEntryRedDotTransition()
        {
            if (_isToolbarEntryRedDotIconTransitToScale)
            {
                _toolbarEntryRedDotIcon.style.scale = new Scale(_toolbarEntryRedDotIconTransitionScale);
            }
            else
            {
                _toolbarEntryRedDotIcon.style.scale = new Scale(Vector3.one);
            }
        }

        private static void ReverseToolbarEntryRedDotTransition(TransitionEndEvent evt)
        {
            _isToolbarEntryRedDotIconTransitToScale = !_isToolbarEntryRedDotIconTransitToScale;
            if (_isToolbarEntryRedDotIconTransitToScale)
            {
                _toolbarEntryRedDotIcon.style.scale = new Scale(_toolbarEntryRedDotIconTransitionScale);
            }
            else
            {
                _toolbarEntryRedDotIcon.style.scale = new Scale(Vector3.one);
            }
        }

        private static void UpdateToolbarEntryIcon(bool hasUnreadNotes)
        {
            if (hasUnreadNotes && !_toolbarEntryRedDotIconVisible)
            {
                _toolbarEntryRedDotIconVisible = true;
                _toolbarEntryRedDotIcon.style.display = DisplayStyle.Flex;
            }
            else if (!hasUnreadNotes && _toolbarEntryRedDotIconVisible)
            {
                _toolbarEntryRedDotIcon.style.display = DisplayStyle.None;
            }
        }

        private static void UpdateWindowTitleIcon(bool hasUnreadNotes)
        {
            if (!_windowInstance)
            {
                return;
            }

            // TODO: Update Window Title Icon
        }

        #endregion


        private bool _createSettingsButtonVisible;
        private Button _createSettingsButton;
        private VisualElement _mainViewContainer;
        private EditorToggleGroup _categoryGroup;
        private ListView _noteEntryListView;
        private NoteContentView _contentView;


#pragma warning disable IDE0051 // Remove unused private members
        private void ShowButton(Rect position)
        {
            if (GUI.Button(position, EditorGUIUtility.IconContent("_Help"), GUI.skin.FindStyle("IconButton")))
            {
                Application.OpenURL("https://github.com/SolarianZ/UnityProjectNotes");
            }
        }
#pragma warning restore IDE0051 // Remove unused private members

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;


            #region Create New Settings

            _createSettingsButtonVisible = true;
            _createSettingsButton = new Button(ProjectNotesSettings.CreateNewInstance)
            {
                text = "Create New Settings",
                style =
                {
                    display = _createSettingsButtonVisible ? DisplayStyle.Flex : DisplayStyle.None,
                }
            };
            root.Add(_createSettingsButton);

            #endregion


            _mainViewContainer = new VisualElement
            {
                name = "MainViewContainer",
                style =
                {
                    display = _createSettingsButtonVisible ? DisplayStyle.None : DisplayStyle.Flex,
                }
            };
            root.Add(_mainViewContainer);
            _mainViewContainer.StretchToParentSize();


            #region Category

            VisualElement categoryContainer = new VisualElement
            {
                name = "CategoryContainer",
                style =
                {
                    flexDirection = FlexDirection.Row,
                },
            };
            _mainViewContainer.Add(categoryContainer);

            _categoryGroup = new EditorToggleGroup
            {
                name = "CategoryGroup",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    flexGrow = 1,
                }
            };
            categoryContainer.Add(_categoryGroup);

            #endregion


            TwoPaneSplitView noteContainer = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal)
            {
                name = "NoteContainer",
            };
            noteContainer.RegisterCallback<GeometryChangedEvent>(SetupNotePanes);
            _mainViewContainer.Add(noteContainer);


            #region Note List

            _noteEntryListView = new ListView
            {
                itemsSource = _filteredNotes,
                fixedItemHeight = 24,
                makeItem = NoteListViewItemLabel.MakeItem,
                bindItem = BindNoteListItemView,
                reorderable = false,
                selectionType = SelectionType.Single,
            };
#if UNITY_2022_3_OR_NEWER
            _noteEntryListView.selectionChanged += OnNoteEntryListSelectionChanged;
#else
            _noteEntryListView.onSelectionChange += OnNoteEntryListSelectionChanged;
#endif
            noteContainer.Add(_noteEntryListView);

            #endregion


            #region Note Content

            _contentView = new NoteContentView();
            noteContainer.Add(_contentView);

            #endregion
        }

        // Fix fixedPane null exception on Unity 2021
        private void SetupNotePanes(GeometryChangedEvent evt)
        {
            TwoPaneSplitView resultContainer = (TwoPaneSplitView)evt.target;
            resultContainer.UnregisterCallback<GeometryChangedEvent>(SetupNotePanes);

            // NullReferenceException thrown when no pane added
            resultContainer.schedule.Execute(() =>
            {
                resultContainer.fixedPane.style.minWidth = 200;
                resultContainer.flexedPane.style.minWidth = 200;
            });
        }


        #region Category

        private bool _selectedCategoryChanged = true;


        private void UpdateCategories()
        {
            if (_categoryGroup == null || !_selectedCategoryChanged)
            {
                return;
            }

            _categoryGroup.Clear();
            if (Settings)
            {
                bool selectedCategoryFound = false;
                List<string> categories = Settings.CollectCategories();
                foreach (string category in categories)
                {
                    CategoryEntryToggle entry = new CategoryEntryToggle(category);
                    if (category == LocalCache.SelectedCategory)
                    {
                        selectedCategoryFound = true;
                        entry.SetValueWithoutNotify(true);
                    }
                    _categoryGroup.Add(entry);
                }

                if (!selectedCategoryFound)
                {
                    LocalCache.SelectedCategory = ProjectNotesSettings.CategoryAll;
                    ((CategoryEntryToggle)_categoryGroup[0]).SetValueWithoutNotify(true);
                }
            }

            _selectedCategoryChanged = false;
        }

        #endregion


        #region Note List View

        public void BindNoteListItemView(VisualElement element, int index)
        {
            NoteListViewItemLabel itemView = (NoteListViewItemLabel)element;
            NoteEntry noteItem = Settings.Notes[index];
            itemView.SetupView(noteItem.title, !LocalCache.IsRead(noteItem.GetKey()));
        }

        private void OnNoteEntryListSelectionChanged(IEnumerable<object> enumerable)
        {
            NoteEntry note = (NoteEntry)_noteEntryListView.selectedItem;
            _contentView.SetNote(note);
        }

        private void UpdateFilteredNoteList()
        {
            // TODO
        }

        #endregion
    }
}