using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

public class GameObjectManagerWindow : EditorWindow
{
    // Style variables
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle sectionStyle;
    private GUIStyle objectRowStyle;
    private GUIStyle selectedObjectRowStyle;
    private GUIStyle buttonStyle;
    private GUIStyle toggleStyle;
    private GUIStyle searchFieldStyle;
    private Color activeColor = new Color(0.6f, 1f, 0.6f, 1f);
    private Color inactiveColor = new Color(1f, 0.6f, 0.6f, 1f);
    private Color selectedColor = new Color(0.7f, 0.85f, 1f, 1f);
    private Texture2D pingIcon;
    private Texture2D selectIcon;
    private Texture2D folderIcon;
    private GUIContent expandedContent;
    private GUIContent collapsedContent;
    
    // Tab system
    private int selectedTabIndex = 0;
    private readonly string[] tabNames = { "Game Objects", "Tools" };
    
    // Undo/Redo icons
    private GUIContent undoContent;
    private GUIContent redoContent;
    
    // Scrolling and state
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private bool showOnlyActive = false;
    private bool showOnlyInactive = false;
    private bool showHierarchyState = true;
    private GameObject selectedObject = null;
    private Vector2 transformScrollPosition;
    private bool showTransformEditor = true;
    private bool doUndo = false;
    private bool doRedo = false;
    
    // Component filter options
    private bool filterByMeshRenderer = false;
    private bool filterByCollider = false;
    private bool filterByRigidbody = false;
    private bool showFilters = false;

    // Multi-selection related variables
    private List<GameObject> selectedObjects = new List<GameObject>();
    private bool showBatchOperations = true;
    private Vector2 batchScrollPosition;
    private Vector3 positionDelta = Vector3.zero;
    private Vector3 rotationDelta = Vector3.zero;
    private Vector3 scaleDelta = Vector3.one;
    private bool useAbsoluteValues = true;
    private string selectedComponentToAdd = "None";
    private int selectedComponentIndex = 0;
    private List<Type> availableComponents = new List<Type>();
    private string[] availableComponentNames = new string[0];
    private Vector2 componentsScrollPosition;
    private Dictionary<string, bool> componentsToRemove = new Dictionary<string, bool>();
    
    // Component organization variables
    private Dictionary<string, string> componentCategories = new Dictionary<string, string>();
    private Dictionary<string, Texture> componentIcons = new Dictionary<string, Texture>();
    private Dictionary<string, List<string>> groupedComponents = new Dictionary<string, List<string>>();
    private Dictionary<string, bool> categoryFoldouts = new Dictionary<string, bool>();

    // Dictionary to track expanded state of GameObjects
    private Dictionary<int, bool> expandedObjects = new Dictionary<int, bool>();

    // Variables for tracking component section foldouts
    private bool showAddComponentSection = true;
    private bool showRemoveComponentsSection = true;

    // Menu item to create the window
    [MenuItem("Window/GameObject Manager")]
    public static void ShowWindow()
    {
        GameObjectManagerWindow window = (GameObjectManagerWindow)EditorWindow.GetWindow(typeof(GameObjectManagerWindow));
        window.titleContent = new GUIContent("GameObject Manager");
        
        window.minSize = new Vector2(650, 500);
        
        window.Show();
    }

    void OnEnable()
    {
        // Initialize the available components list
        UpdateAvailableComponents();
        
        // Load icons
        pingIcon = EditorGUIUtility.FindTexture("d_ViewToolZoom");
        selectIcon = EditorGUIUtility.FindTexture("d_Selection.All");
        folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
        
        // Create GUIContent for expand/collapse arrows
        expandedContent = EditorGUIUtility.IconContent("IN_foldout_on");
        collapsedContent = EditorGUIUtility.IconContent("IN_foldout");
        
        // Initialize Undo/Redo icons
        undoContent = EditorGUIUtility.IconContent("Animation.PrevKey");
        undoContent.text = " Undo";
        redoContent = EditorGUIUtility.IconContent("Animation.NextKey");
        redoContent.text = " Redo";
        
        // Initialize component icons and categories
        InitializeComponentCategories();
    }
    
    private void InitializeComponentCategories()
    {
        // Clear dictionaries
        componentCategories.Clear();
        componentIcons.Clear();
        
        // Load component icons
        componentIcons["UnityEngine.BoxCollider"] = EditorGUIUtility.IconContent("BoxCollider Icon").image;
        componentIcons["UnityEngine.SphereCollider"] = EditorGUIUtility.IconContent("SphereCollider Icon").image;
        componentIcons["UnityEngine.CapsuleCollider"] = EditorGUIUtility.IconContent("CapsuleCollider Icon").image;
        componentIcons["UnityEngine.MeshCollider"] = EditorGUIUtility.IconContent("MeshCollider Icon").image;
        componentIcons["UnityEngine.Rigidbody"] = EditorGUIUtility.IconContent("Rigidbody Icon").image;
        componentIcons["UnityEngine.AudioSource"] = EditorGUIUtility.IconContent("AudioSource Icon").image;
        componentIcons["UnityEngine.Light"] = EditorGUIUtility.IconContent("Light Icon").image;
        componentIcons["UnityEngine.ParticleSystem"] = EditorGUIUtility.IconContent("ParticleSystem Icon").image;
        componentIcons["UnityEngine.MeshRenderer"] = EditorGUIUtility.IconContent("MeshRenderer Icon").image;
        componentIcons["UnityEngine.MeshFilter"] = EditorGUIUtility.IconContent("MeshFilter Icon").image;
        componentIcons["UnityEngine.Canvas"] = EditorGUIUtility.IconContent("Canvas Icon").image;
        
        // Physics category
        componentCategories["UnityEngine.BoxCollider"] = "Physics";
        componentCategories["UnityEngine.SphereCollider"] = "Physics";
        componentCategories["UnityEngine.CapsuleCollider"] = "Physics";
        componentCategories["UnityEngine.MeshCollider"] = "Physics";
        componentCategories["UnityEngine.Rigidbody"] = "Physics";
        componentCategories["UnityEngine.Collider"] = "Physics";
        componentCategories["UnityEngine.Joint"] = "Physics";
        
        // Rendering category
        componentCategories["UnityEngine.MeshRenderer"] = "Rendering";
        componentCategories["UnityEngine.MeshFilter"] = "Rendering";
        componentCategories["UnityEngine.SkinnedMeshRenderer"] = "Rendering";
        componentCategories["UnityEngine.ParticleSystem"] = "Rendering";
        componentCategories["UnityEngine.ParticleSystemRenderer"] = "Rendering";
        componentCategories["UnityEngine.TrailRenderer"] = "Rendering";
        componentCategories["UnityEngine.LineRenderer"] = "Rendering";
        
        // Audio category
        componentCategories["UnityEngine.AudioSource"] = "Audio";
        componentCategories["UnityEngine.AudioListener"] = "Audio";
        componentCategories["UnityEngine.AudioReverbZone"] = "Audio";
        
        // Lighting category
        componentCategories["UnityEngine.Light"] = "Lighting";
        componentCategories["UnityEngine.LightProbeGroup"] = "Lighting";
        componentCategories["UnityEngine.ReflectionProbe"] = "Lighting";
        
        // UI category
        componentCategories["UnityEngine.Canvas"] = "UI";
        componentCategories["UnityEngine.CanvasRenderer"] = "UI";
        componentCategories["UnityEngine.UI.Image"] = "UI";
        componentCategories["UnityEngine.UI.Text"] = "UI";
        componentCategories["UnityEngine.UI.Button"] = "UI";
        componentCategories["UnityEngine.UI.RawImage"] = "UI";
        componentCategories["UnityEngine.UI.Slider"] = "UI";
        componentCategories["UnityEngine.UI.Toggle"] = "UI";
        
        // Default categories for foldouts
        categoryFoldouts["Physics"] = true;
        categoryFoldouts["Rendering"] = true;
        categoryFoldouts["Audio"] = true;
        categoryFoldouts["Lighting"] = true;
        categoryFoldouts["UI"] = true;
        categoryFoldouts["Scripts"] = true;
        categoryFoldouts["Other"] = true;
    }
    
    // Initializes and configures all custom GUI styles used throughout the editor window
    void InitStyles()
    {
        if (headerStyle == null)
        {
            // Main section header style
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 16;
            headerStyle.margin.top = 8;
            headerStyle.margin.bottom = 8;
            
            // Sub-section header style
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            subHeaderStyle.fontSize = 14;
            subHeaderStyle.margin.top = 6;
            subHeaderStyle.margin.bottom = 6;
            
            // Container style for major sections
            sectionStyle = new GUIStyle(EditorStyles.helpBox);
            sectionStyle.padding = new RectOffset(10, 10, 10, 10);
            sectionStyle.margin = new RectOffset(5, 5, 5, 5);
            
            // Style for rows in the GameObject list
            objectRowStyle = new GUIStyle(EditorStyles.helpBox);
            objectRowStyle.padding = new RectOffset(8, 8, 4, 4);
            objectRowStyle.margin = new RectOffset(2, 2, 2, 2);
            
            // Highlighted version of objectRowStyle used when a row is selected
            selectedObjectRowStyle = new GUIStyle(objectRowStyle);
            selectedObjectRowStyle.normal.background = MakeTexture(2, 2, selectedColor);
            
            // Custom button style with balanced padding to improve visual appearance
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(8, 8, 4, 4);
            
            // Custom toggle style based on labels
            toggleStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(16, 0, 2, 0)
            };
            toggleStyle.normal.background = null;
            
            searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField);
            searchFieldStyle.margin = new RectOffset(5, 5, 5, 5);
            searchFieldStyle.fixedHeight = 22;
        }
    }
    
    // Create color textures
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // Populates the list of available components that can be added to GameObjects in the editor
    void UpdateAvailableComponents()
    {
        availableComponents.Clear();
        
        // Add components to the list
        availableComponents.Add(typeof(BoxCollider));
        availableComponents.Add(typeof(SphereCollider));
        availableComponents.Add(typeof(CapsuleCollider));
        availableComponents.Add(typeof(MeshCollider));
        availableComponents.Add(typeof(Rigidbody));
        availableComponents.Add(typeof(AudioSource));
        availableComponents.Add(typeof(Light));
        availableComponents.Add(typeof(ParticleSystem));
        availableComponents.Add(typeof(Canvas));
        availableComponents.Add(typeof(Camera));
        
        // Create string array of component names for the popup
        availableComponentNames = new string[availableComponents.Count + 1];
        availableComponentNames[0] = "None";
        
        for (int i = 0; i < availableComponents.Count; i++)
        {
            availableComponentNames[i + 1] = availableComponents[i].Name;
        }
    }
    
    // Renders the main editor window UI with tabs, toolbar, and content panels
    void OnGUI()
    {
        // Initialize custom styles
        InitStyles();
        
        // Execute pending Undo/Redo operations before starting any layout groups
        ExecutePendingUndoRedo();
        
        // Window Settings Bar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button(undoContent, EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            doUndo = true;
            EditorApplication.delayCall += () => {
                Undo.PerformUndo();
                Repaint();
            };
        }
        
        if (GUILayout.Button(redoContent, EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            doRedo = true;
            EditorApplication.delayCall += () => {
                Undo.PerformRedo();
                Repaint();
            };
        }
        
        GUILayout.FlexibleSpace();
        
        // Displays selection status in the upper right
        GUIStyle selectionStatusStyle = new GUIStyle(EditorStyles.label) {
            fontStyle = FontStyle.Bold
        };
        
        string selectionText = "No Objects Selected";
        if (selectedObjects.Count == 1)
        {
            selectionText = $"Selected: {selectedObjects[0].name}";
        }
        else if (selectedObjects.Count > 1)
        {
            selectionText = $"Multiple Objects Selected ({selectedObjects.Count})";
        }
        
        GUILayout.Label(selectionText, selectionStatusStyle);
        
        EditorGUILayout.EndHorizontal();
        
        // Tab system
        selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, tabNames, EditorStyles.toolbarButton);
        
        // Draw the selected tab content
        switch (selectedTabIndex)
        {
            case 0: // Game Objects Tab
                DrawGameObjectsTab();
                break;
            case 1: // Tools Tab
                DrawToolsTab();
                break;
        }
    }
    
    // Renders the Game Objects tab with search filters and the hierarchical list of scene objects
    private void DrawGameObjectsTab()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        
        // Get and filter all GameObjects in the scene - moved up for early count access
        GameObject[] allObjects = FindAllGameObjectsInScene();
        List<GameObject> filteredObjects = FilterGameObjects(allObjects);
        
        // Organize GameObjects in hierarchical order
        List<GameObject> organizedObjects = OrganizeHierarchy(filteredObjects);
        
        // Search and filter panel with count
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        
        // "Search & Filters" label
        EditorGUILayout.LabelField("Search & Filters", subHeaderStyle, GUILayout.Width(120));
        
        GUILayout.FlexibleSpace();
        
        // Create count label
        GUIContent countContent = new GUIContent($"Found {filteredObjects.Count} of {allObjects.Length}");
        GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel) {
            alignment = TextAnchor.MiddleRight,
            padding = new RectOffset(0, 5, 2, 0)
        };
        EditorGUILayout.LabelField(countContent, countStyle, GUILayout.Width(140));
        
        EditorGUILayout.EndHorizontal();
        
        // Search bar
        EditorGUILayout.BeginHorizontal();
        string previousSearchFilter = searchFilter;
        searchFilter = EditorGUILayout.TextField(searchFilter, searchFieldStyle);
        EditorGUILayout.EndHorizontal();
        
        // Show/hide filters 
        showFilters = EditorGUILayout.Foldout(showFilters, "Show Filters", true);
        
        if (showFilters)
        {
            GUIStyle spacedRadioButton = new GUIStyle(EditorStyles.radioButton);
            spacedRadioButton.padding.left = 20;
            
            // Component filtering options
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Component Filters:", GUILayout.Width(120));
            
            Rect toggleRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUILayout.Width(120));
            filterByMeshRenderer = GUI.Toggle(toggleRect, filterByMeshRenderer, "Mesh Renderer", spacedRadioButton);
            
            toggleRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUILayout.Width(100));
            filterByCollider = GUI.Toggle(toggleRect, filterByCollider, "Collider", spacedRadioButton);
            
            toggleRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            filterByRigidbody = GUI.Toggle(toggleRect, filterByRigidbody, "Rigidbody", spacedRadioButton);
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();

        // Help text
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Hold Ctrl or Shift while clicking to select multiple objects.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndHorizontal();

        // GameObject list
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        Rect listRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true));
        Rect headerRect = new Rect(listRect.x, listRect.y, listRect.width, EditorGUIUtility.singleLineHeight);
        GUI.Box(headerRect, GUIContent.none, EditorStyles.toolbar);
        
        float xPos = headerRect.x;
        float toggleWidth = 45;
        float nameWidth = Mathf.Max(250, listRect.width - 280);
        float statusWidth = 90;
        float actionWidth = 30;
        float selectWidth = 80;
        
        // Header labels
        EditorGUI.LabelField(new Rect(xPos, headerRect.y, toggleWidth, headerRect.height), "Active", EditorStyles.toolbarButton);
        xPos += toggleWidth;
        EditorGUI.LabelField(new Rect(xPos, headerRect.y, nameWidth, headerRect.height), "Name", EditorStyles.toolbarButton);
        xPos += nameWidth;
        EditorGUI.LabelField(new Rect(xPos, headerRect.y, statusWidth, headerRect.height), "in Hierarchy", EditorStyles.toolbarButton);
        xPos += statusWidth;
        EditorGUI.LabelField(new Rect(xPos, headerRect.y, actionWidth + selectWidth, headerRect.height), "Actions", EditorStyles.toolbarButton);
        
        // Scrollview for the list content
        Rect contentRect = new Rect(listRect.x, listRect.y + EditorGUIUtility.singleLineHeight, 
                                  listRect.width - 16, // Account for scrollbar
                                  listRect.height - EditorGUIUtility.singleLineHeight);
        
        scrollPosition = GUI.BeginScrollView(contentRect, scrollPosition, 
            new Rect(0, 0, contentRect.width - 16, organizedObjects.Count * (EditorGUIUtility.singleLineHeight + 4)));
        
        // Draw list items
        float yPos = 6;
        foreach (GameObject obj in organizedObjects)
        {
            DrawGameObjectRowInList(obj, filteredObjects, yPos, contentRect.width, nameWidth);
            yPos += EditorGUIUtility.singleLineHeight + 4;
        }
        
        GUI.EndScrollView();
        
        if (organizedObjects.Count == 0)
        {
            EditorGUI.HelpBox(new Rect(listRect.x, listRect.y + EditorGUIUtility.singleLineHeight, 
                                     listRect.width, EditorGUIUtility.singleLineHeight * 2),
                            "No GameObjects match the current filters.", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();
    }
    
    // Renders a single row in the GameObject list
    private void DrawGameObjectRowInList(GameObject obj, List<GameObject> allFilteredObjects, float yPos, float totalWidth, float nameWidth)
    {
        bool isSelected = selectedObjects.Contains(obj);
        float rowHeight = EditorGUIUtility.singleLineHeight + 4;
        
        if (isSelected)
        {
            GUI.Box(new Rect(0, yPos, totalWidth, rowHeight), GUIContent.none, selectedObjectRowStyle);
        }
        else
        {
            GUI.Box(new Rect(0, yPos, totalWidth, rowHeight), GUIContent.none, objectRowStyle);
        }
        
        float xPos = 0;
        float contentY = yPos + (rowHeight - EditorGUIUtility.singleLineHeight) * 0.5f;
        
        // Active toggle checkbox
        bool isActiveSelf = obj.activeSelf;
        EditorGUI.BeginChangeCheck();
        Rect toggleRect = new Rect(xPos + 5, contentY, 30, EditorGUIUtility.singleLineHeight);
        bool newIsActiveSelf = EditorGUI.Toggle(toggleRect, isActiveSelf);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(obj, "Toggle GameObject Active State");
            obj.SetActive(newIsActiveSelf);
            EditorUtility.SetDirty(obj);
        }
        xPos += 45;
        
        int depth = GetHierarchyDepth(obj, allFilteredObjects);
        bool hasChildren = HasVisibleChildren(obj, allFilteredObjects);
        
        // Name section with proper indentation
        Rect nameRect = new Rect(xPos, yPos, nameWidth, rowHeight);
        float indentWidth = depth * 15;
        
        // Coloring based on active state
        Color originalColor = GUI.contentColor;
        bool isActiveInHierarchy = obj.activeInHierarchy;
        GUI.contentColor = isActiveInHierarchy ? activeColor : inactiveColor;
        
        // Draw arrow if has children
        if (hasChildren)
        {
            if (!expandedObjects.TryGetValue(obj.GetInstanceID(), out bool isExpanded))
            {
                expandedObjects[obj.GetInstanceID()] = true;
                isExpanded = true;
            }
            
            Rect arrowRect = new Rect(nameRect.x + indentWidth, contentY, 16, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(arrowRect, isExpanded ? expandedContent : collapsedContent, EditorStyles.label))
            {
                expandedObjects[obj.GetInstanceID()] = !isExpanded;
                Repaint();
            }
            indentWidth += 20;
        }
        else
        {
            indentWidth += 20;
        }
        
        // Draw object name
        EditorGUI.LabelField(new Rect(nameRect.x + indentWidth, contentY, nameWidth - indentWidth, EditorGUIUtility.singleLineHeight),
                           obj.name, EditorStyles.boldLabel);
        
        GUI.contentColor = originalColor;
        xPos += nameWidth;
        
        GUI.enabled = false;
        EditorGUI.LabelField(new Rect(xPos + 20, contentY, 50, EditorGUIUtility.singleLineHeight), isActiveInHierarchy ? "Yes" : "No");
        xPos += 90; 
        GUI.enabled = true;
        
        // Action buttons
        if (GUI.Button(new Rect(xPos, contentY, 30, EditorGUIUtility.singleLineHeight), 
                      new GUIContent(pingIcon, "Ping in Hierarchy")))
        {
            EditorGUIUtility.PingObject(obj);
        }
        xPos += 30;
        
        if (GUI.Button(new Rect(xPos, contentY, 80, EditorGUIUtility.singleLineHeight), 
                      new GUIContent("Select", selectIcon, "Select this GameObject")))
        {
            Event e = Event.current;
            if (e.control || e.command)
            {
                if (selectedObjects.Contains(obj))
                    selectedObjects.Remove(obj);
                else
                    selectedObjects.Add(obj);
            }
            else if (e.shift)
            {
                if (selectedObjects.Contains(obj))
                    selectedObjects.Remove(obj);
                else
                    selectedObjects.Add(obj);
            }
            else
            {
                selectedObjects.Clear();
                selectedObjects.Add(obj);
                selectedObject = obj;
            }
            
            Selection.objects = selectedObjects.Cast<UnityEngine.Object>().ToArray();
            UpdateComponentsToRemove();
        }
    }
    
    // Renders the "Tools" tab
    private void DrawToolsTab()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        
        // Check if we have valid selected objects
        if (selectedObjects.Count == 0)
        {
            EditorGUILayout.HelpBox("Select one or more GameObjects from the 'Game Objects' tab to edit their properties.", MessageType.Info);
        }
        else
        {
            // Transform Tools Section
            EditorGUILayout.LabelField("Transform Tools", subHeaderStyle);
            
            if (selectedObjects.Count == 1)
            {
                // Single object selection
                selectedObject = selectedObjects[0];
                DrawTransformEditor();
            }
            else
            {
                // Multiple objects selected
                DrawBatchOperations();
            }
            
            EditorGUILayout.Space(10);
            
            // Component Tools Section
            EditorGUILayout.LabelField("Component Tools", subHeaderStyle);
            
            DrawComponentSection(selectedObjects);
        }
        
        EditorGUILayout.EndVertical();
    }

    // Renders the transform editor UI for a single selected GameObject
    private void DrawTransformEditor()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();

        Rect foldoutRect = GUILayoutUtility.GetRect(12, 16, GUILayout.Width(12));
        showTransformEditor = EditorGUI.Foldout(foldoutRect, showTransformEditor, "");
        
        GUILayout.Label(EditorGUIUtility.IconContent("Transform Icon"), GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField($"Transform Editor: {selectedObject.name}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        if (showTransformEditor)
        {
            // Fixed height scrollview that just fits the content
            transformScrollPosition = EditorGUILayout.BeginScrollView(transformScrollPosition, GUILayout.Height(75));
            
            // Get the transform component
            Transform transform = selectedObject.transform;
            
            // Position
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Position", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = EditorGUILayout.Vector3Field("", transform.localPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(transform, "Change Position");
                transform.localPosition = newPosition;
                EditorUtility.SetDirty(transform);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            
            // Rotation
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Rotation", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            Vector3 newRotation = EditorGUILayout.Vector3Field("", transform.localEulerAngles);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(transform, "Change Rotation");
                transform.localEulerAngles = newRotation;
                EditorUtility.SetDirty(transform);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            
            // Scale
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Scale", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            Vector3 newScale = EditorGUILayout.Vector3Field("", transform.localScale);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(transform, "Change Scale");
                transform.localScale = newScale;
                EditorUtility.SetDirty(transform);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // Renders UI for batch transform operations
    private void DrawBatchOperations()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
   
        EditorGUILayout.BeginHorizontal();
        
        Rect foldoutRect = GUILayoutUtility.GetRect(12, 16, GUILayout.Width(12));
        showBatchOperations = EditorGUI.Foldout(foldoutRect, showBatchOperations, "");
        
        GUILayout.Label(EditorGUIUtility.IconContent("Transform Icon"), GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField($"Multi-Object Transform Editor ({selectedObjects.Count} objects)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        if (showBatchOperations)
        {
            // Fixed height scrollview that just fits the content
            batchScrollPosition = EditorGUILayout.BeginScrollView(batchScrollPosition, GUILayout.Height(75));
            
            useAbsoluteValues = true;
            
            // Position 
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Position", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = EditorGUILayout.Vector3Field("", positionDelta);
            if (EditorGUI.EndChangeCheck())
            {
                positionDelta = newPosition;
                ApplyTransformChanges();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            
            // Rotation 
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Rotation", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            Vector3 newRotation = EditorGUILayout.Vector3Field("", rotationDelta);
            if (EditorGUI.EndChangeCheck())
            {
                rotationDelta = newRotation;
                ApplyTransformChanges();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(2);
            
            // Scale
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Scale", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            Vector3 newScale = EditorGUILayout.Vector3Field("", scaleDelta);
            if (EditorGUI.EndChangeCheck())
            {
                scaleDelta = newScale;
                ApplyTransformChanges();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // Applies the transform changes to all selected objects
    private void ApplyTransformChanges()
    {
        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                Transform transform = obj.transform;
                Undo.RecordObject(transform, "Batch Transform Change");
                
                if (useAbsoluteValues)
                {
                    transform.localPosition = positionDelta;
                    transform.localEulerAngles = rotationDelta;
                    transform.localScale = scaleDelta;
                }
                else
                {
                    transform.localPosition += positionDelta;
                    transform.localEulerAngles += rotationDelta;
                    transform.localScale = new Vector3(
                        transform.localScale.x * scaleDelta.x,
                        transform.localScale.y * scaleDelta.y,
                        transform.localScale.z * scaleDelta.z
                    );
                }
                
                EditorUtility.SetDirty(transform);
            }
        }
    }
    
    // Renders the component management section for selected objects
    private void DrawComponentSection(List<GameObject> objects)
    {
        // Add Component Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        DrawSectionHeader(EditorStyles.helpBox, "d_Toolbar Plus", "Add Component", ref showAddComponentSection);
        
        if (showAddComponentSection)
        {
            EditorGUILayout.BeginHorizontal();
            int newSelectedComponentIndex = EditorGUILayout.Popup("Component Type", selectedComponentIndex, availableComponentNames);
            if (newSelectedComponentIndex != selectedComponentIndex)
            {
                selectedComponentIndex = newSelectedComponentIndex;
                selectedComponentToAdd = availableComponentNames[selectedComponentIndex];
            }
            
            GUI.enabled = selectedComponentIndex > 0;
            if (GUILayout.Button("Add to Selected", buttonStyle, GUILayout.Width(120)))
            {
                Type componentType = availableComponents[selectedComponentIndex - 1];
                bool componentAdded = false;
                
                foreach (GameObject obj in objects.Where(o => o != null))
                {
                    if (!obj.GetComponent(componentType))
                    {
                        Undo.RecordObject(obj, "Add Component");
                        obj.AddComponent(componentType);
                        EditorUtility.SetDirty(obj);
                        componentAdded = true;
                    }
                }
                
                if (componentAdded) UpdateComponentsToRemove();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
        
        // Component Removal Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        DrawSectionHeader(EditorStyles.helpBox, "d_TreeEditor.Trash", "Remove Components", ref showRemoveComponentsSection);
        
        if (showRemoveComponentsSection && objects.Count > 0)
        {
            if (componentsToRemove.Count == 0)
                UpdateComponentsToRemove();
                
            if (componentsToRemove.Count == 0)
            {
                EditorGUILayout.HelpBox("No removable components found on the selected objects.", MessageType.Info);
            }
            else
            {
                // Display the component removal options
                DrawComponentRemovalOptions();
                
                // Draw the remove button
                DrawRemoveComponentsButton(objects);
            }
        }
        else if (showRemoveComponentsSection)
        {
            EditorGUILayout.HelpBox("Select GameObjects to see and manage their components.", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // Draw a section header with icon and foldout
    private void DrawSectionHeader(GUIStyle style, string iconName, string title, ref bool foldoutState)
    {
        EditorGUILayout.BeginHorizontal();
        
        Rect foldoutRect = GUILayoutUtility.GetRect(12, 16, GUILayout.Width(12));
        foldoutState = EditorGUI.Foldout(foldoutRect, foldoutState, "");
        
        GUILayout.Label(EditorGUIUtility.IconContent(iconName), GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        
        EditorGUILayout.EndHorizontal();
    }
    
    // Draw component removal options
    private void DrawComponentRemovalOptions()
    {
        componentsScrollPosition = EditorGUILayout.BeginScrollView(componentsScrollPosition, GUILayout.Height(160));
        Dictionary<string, bool> pendingChanges = new Dictionary<string, bool>();
        GUIStyle categoryHeaderStyle = new GUIStyle(EditorStyles.foldout) {
            fontStyle = FontStyle.Bold,
            margin = new RectOffset(0, 0, 5, 5)
        };
        
        // Draw components grouped by category
        foreach (var categoryGroup in groupedComponents.Where(cg => cg.Value.Count > 0))
        {
            string category = categoryGroup.Key;
            List<string> componentList = categoryGroup.Value;
            
            if (!categoryFoldouts.ContainsKey(category))
                categoryFoldouts[category] = true;
            
            // Draw category foldout
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            categoryFoldouts[category] = EditorGUILayout.Foldout(categoryFoldouts[category], category, true, categoryHeaderStyle);
            
            if (categoryFoldouts[category])
            {
                foreach (string typeName in componentList)
                {
                    pendingChanges = DrawComponentToggle(typeName, pendingChanges);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Apply all changes after the loop finishes
        foreach (var change in pendingChanges)
            componentsToRemove[change.Key] = change.Value;
        
        EditorGUILayout.EndScrollView();
    }
    
    // Draw a component toggle row
    private Dictionary<string, bool> DrawComponentToggle(string typeName, Dictionary<string, bool> pendingChanges)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Get icon and display name
        Texture icon = componentIcons.TryGetValue(typeName, out Texture componentIcon) ? componentIcon : null;
        
        // Get display name
        string displayName = typeName.StartsWith("UnityEngine.") ? typeName.Substring("UnityEngine.".Length) : typeName;
        
        GUILayout.Space(15);
        
        // Draw toggle
        EditorGUI.BeginChangeCheck();
        bool shouldRemove = EditorGUILayout.Toggle(componentsToRemove[typeName], GUILayout.Width(20));
        
        // Draw icon
        if (icon != null)
            GUILayout.Label(new GUIContent(icon), GUILayout.Width(20), GUILayout.Height(20));
        else
            GUILayout.Space(20);
        
        // Draw component name
        if (GUILayout.Button(displayName, EditorStyles.label))
            shouldRemove = !componentsToRemove[typeName];
        
        if (EditorGUI.EndChangeCheck())
            pendingChanges[typeName] = shouldRemove;
        
        EditorGUILayout.EndHorizontal();
        
        return pendingChanges;
    }
    
    // Draw the remove components button
    private void DrawRemoveComponentsButton(List<GameObject> objects)
    {
        bool anyComponentSelected = componentsToRemove.Any(kvp => kvp.Value);
        int selectedComponentCount = componentsToRemove.Count(kvp => kvp.Value);
        
        EditorGUILayout.Space(5);
        
        GUIStyle removeButtonStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };
        
        Color originalBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = anyComponentSelected 
            ? new Color(0.9f, 0.3f, 0.3f, 1f)  // Bold red for enabled state
            : new Color(0.6f, 0.6f, 0.6f, 1f); // Gray for disabled state
        
        GUI.enabled = anyComponentSelected;
        if (GUILayout.Button($"Remove Selected Components ({selectedComponentCount})", removeButtonStyle))
        {
            RemoveSelectedComponents(objects);
        }
        
        GUI.backgroundColor = originalBackgroundColor;
        GUI.enabled = true;
    }
    
    // Remove selected components
    private void RemoveSelectedComponents(List<GameObject> objects)
    {
        Dictionary<string, List<string>> objectsMissingComponents = new Dictionary<string, List<string>>();
        
        foreach (GameObject obj in objects.Where(o => o != null))
        {
            foreach (var kvp in componentsToRemove.Where(c => c.Value))
            {
                // Get component type
                Type componentType = FindTypeByName(kvp.Key);
                
                // Skip if type cannot be found
                if (componentType == null)
                {
                    AddToMissingComponentsList(objectsMissingComponents, obj.name, kvp.Key + " (Type not found)");
                    continue;
                }
                
                Component[] components = obj.GetComponents(componentType);
                
                if (components == null || components.Length == 0)
                {
                    AddToMissingComponentsList(objectsMissingComponents, obj.name, componentType?.Name ?? kvp.Key);
                }
                else
                {
                    // Remove components from objects that have them
                    foreach (Component comp in components)
                    {
                        if (comp != null && !(comp is Transform))
                        {
                            Undo.DestroyObjectImmediate(comp);
                            EditorUtility.SetDirty(obj);
                        }
                    }
                }
            }
        }
        
        // Show notification if some objects were missing components
        if (objectsMissingComponents.Count > 0)
        {
            ShowMissingComponentsDialog(objectsMissingComponents);
        }
        
        // Refresh the list
        UpdateComponentsToRemove();
    }
    
    // Add to the missing components list
    private void AddToMissingComponentsList(Dictionary<string, List<string>> dictionary, string objectName, string componentName)
    {
        if (!dictionary.ContainsKey(objectName))
            dictionary.Add(objectName, new List<string>());
            
        dictionary[objectName].Add(componentName);
    }
    
    // Show dialog for missing components
    private void ShowMissingComponentsDialog(Dictionary<string, List<string>> objectsMissingComponents)
    {
        string message = "The following objects didn't have some of the selected components:\n\n";
        
        foreach (var objEntry in objectsMissingComponents)
            message += $"• {objEntry.Key}: missing {string.Join(", ", objEntry.Value)}\n";
        
        EditorUtility.DisplayDialog("Component Removal", message, "OK");
    }
    
    // Update the list of components to remove
    private void UpdateComponentsToRemove()
    {
        componentsToRemove.Clear();
        groupedComponents.Clear();
        
        groupedComponents["Physics"] = new List<string>();
        groupedComponents["Rendering"] = new List<string>();
        groupedComponents["Audio"] = new List<string>();
        groupedComponents["Lighting"] = new List<string>();
        groupedComponents["UI"] = new List<string>();
        groupedComponents["Scripts"] = new List<string>();
        groupedComponents["Other"] = new List<string>();
        
        foreach (GameObject obj in selectedObjects)
        {
            if (obj != null)
            {
                Component[] components = obj.GetComponents<Component>();
                foreach (Component comp in components)
                {
                    if (comp != null && !(comp is Transform))
                    {
                        string typeName = comp.GetType().FullName;
                        
                        if (!componentsToRemove.ContainsKey(typeName))
                        {
                            componentsToRemove.Add(typeName, false);
                            
                            // Add icon if missing
                            if (!componentIcons.ContainsKey(typeName))
                            {
                                string iconName = comp.GetType().Name + " Icon";
                                Texture icon = EditorGUIUtility.IconContent(iconName).image;
                                
                                if (icon == null)
                                {
                                    icon = EditorGUIUtility.IconContent("cs Script Icon").image;
                                }
                                
                                componentIcons[typeName] = icon;
                            }
                            
                            // Categorize component
                            if (componentCategories.TryGetValue(typeName, out string category))
                            {
                                // Component has a category
                                if (!groupedComponents.ContainsKey(category))
                                {
                                    groupedComponents[category] = new List<string>();
                                }
                                groupedComponents[category].Add(typeName);
                            }
                            else if (typeName.StartsWith("UnityEngine."))
                            {
                                groupedComponents["Other"].Add(typeName);
                            }
                            else
                            {
                                groupedComponents["Scripts"].Add(typeName);
                            }
                        }
                    }
                }
            }
        }
        
        // Remove empty categories
        List<string> emptyCategories = new List<string>();
        foreach (var category in groupedComponents)
        {
            if (category.Value.Count == 0)
            {
                emptyCategories.Add(category.Key);
            }
        }
        
        foreach (string category in emptyCategories)
        {
            groupedComponents.Remove(category);
        }
    }

    // Find all GameObjects in the scene
    private GameObject[] FindAllGameObjectsInScene()
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => {
                if (!go.scene.isLoaded)
                    return false;
                
                // Filter out objects with HideInHierarchy flag
                if ((go.hideFlags & HideFlags.HideInHierarchy) != 0)
                    return false;
                return true;
            })
            .ToArray();
    }

    // Filter GameObjects
    private List<GameObject> FilterGameObjects(GameObject[] allObjects)
    {
        // Early return if no filters are active
        if (!showOnlyActive && !showOnlyInactive && string.IsNullOrEmpty(searchFilter) && 
            !filterByMeshRenderer && !filterByCollider && !filterByRigidbody)
        {
            return new List<GameObject>(allObjects);
        }

        // Pre-compute search filter for case-insensitive comparison
        string searchFilterLower = searchFilter?.ToLower();
        
        // Pre-check
        bool needComponentFiltering = filterByMeshRenderer || filterByCollider || filterByRigidbody;
        
        // Use a List for building the result
        List<GameObject> result = new List<GameObject>(allObjects.Length);
        
        foreach (GameObject obj in allObjects)
        {
            // Check active state
            bool isActive = showHierarchyState ? obj.activeInHierarchy : obj.activeSelf;
            if (showOnlyActive && !isActive) continue;
            if (showOnlyInactive && isActive) continue;
            
            // Check name filter
            if (!string.IsNullOrEmpty(searchFilterLower) && 
                !obj.name.ToLower().Contains(searchFilterLower))
                continue;
            
            if (needComponentFiltering)
            {
                MeshRenderer meshRenderer = filterByMeshRenderer ? obj.GetComponent<MeshRenderer>() : null;
                Collider collider = filterByCollider ? obj.GetComponent<Collider>() : null;
                Rigidbody rigidbody = filterByRigidbody ? obj.GetComponent<Rigidbody>() : null;
                
                if (filterByMeshRenderer && meshRenderer == null) continue;
                if (filterByCollider && collider == null) continue;
                if (filterByRigidbody && rigidbody == null) continue;
            }
            result.Add(obj);
        }
        return result;
    }

    // Organize GameObjects hierarchically
    private List<GameObject> OrganizeHierarchy(List<GameObject> objects)
    {
        Dictionary<Transform, List<GameObject>> childrenByParent = new Dictionary<Transform, List<GameObject>>();
        List<GameObject> rootObjects = new List<GameObject>();
        
        // Categorize objects by their parent
        foreach (GameObject obj in objects)
        {
            if (obj.transform.parent == null || !objects.Any(go => go.transform == obj.transform.parent))
            {
                // Root object
                rootObjects.Add(obj);
            }
            else
            {
                // Has a parent
                if (!childrenByParent.ContainsKey(obj.transform.parent))
                {
                    childrenByParent[obj.transform.parent] = new List<GameObject>();
                }
                childrenByParent[obj.transform.parent].Add(obj);
            }
        }
        
        // Sort root objects by name
        rootObjects.Sort((a, b) => string.Compare(a.name, b.name));
        
        // Sort children by name within each parent
        foreach (var parentKey in childrenByParent.Keys.ToList())
        {
            childrenByParent[parentKey].Sort((a, b) => string.Compare(a.name, b.name));
        }
        
        List<GameObject> result = new List<GameObject>();
        
        // Add root objects and their children recursively
        foreach (GameObject rootObj in rootObjects)
        {
            result.Add(rootObj);
            AddChildrenRecursively(rootObj.transform, childrenByParent, result);
        }
        
        return result;
    }
    
    // Recursively add children to the organized list
    private void AddChildrenRecursively(Transform parent, Dictionary<Transform, List<GameObject>> childrenByParent, List<GameObject> result)
    {
        if (!childrenByParent.ContainsKey(parent))
            return;
            
        // Only add children if parent is expanded
        if (expandedObjects.TryGetValue(parent.gameObject.GetInstanceID(), out bool isExpanded) && isExpanded)
        {
            foreach (GameObject childObj in childrenByParent[parent])
            {
                result.Add(childObj);
                AddChildrenRecursively(childObj.transform, childrenByParent, result);
            }
        }
    }
    
    // Calculate hierarchy depth
    private int GetHierarchyDepth(GameObject obj, List<GameObject> allObjects)
    {
        int depth = 0;
        Transform parent = obj.transform.parent;
        
        while (parent != null && allObjects.Any(go => go.transform == parent))
        {
            depth++;
            parent = parent.parent;
        }
        
        return depth;
    }
    
    // Determine if an object has children in the filtered list
    private bool HasVisibleChildren(GameObject obj, List<GameObject> allObjects)
    {
        return allObjects.Any(go => go.transform.parent == obj.transform);
    }

    // Repaint the window when things change
    void OnInspectorUpdate()
    {
        Repaint();
    }

    // Execute pending Undo/Redo operations
    private void ExecutePendingUndoRedo()
    {
        if (doUndo)
        {
            doUndo = false;
        }
        else if (doRedo)
        {
            doRedo = false;
        }
    }

    // Find a type by its name
    private Type FindTypeByName(string typeName)
    {
        Type type = Type.GetType(typeName);
        if (type == null)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    break;
            }
        }
        return type;
    }
} 