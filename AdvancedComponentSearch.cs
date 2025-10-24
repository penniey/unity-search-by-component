using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

public class AdvancedComponentSearch : EditorWindow
{
    private string searchText = "";
    private List<ComponentSearchResult> searchResults = new List<ComponentSearchResult>();
    private Vector2 scrollPosition;
    private bool includeInactive = false;
    private bool exactMatch = false;
    private bool showComponentDetails = true;
    private int searchMode = 0; 
    private string pendingSearchText = "";
    private bool searchPending = false;
    private string[] searchModes = { "By Name"};
    
    [MenuItem("Tools/Component Search")]
    public static void ShowWindow()
    {
        GetWindow<AdvancedComponentSearch>("Component Search");
    }
    
    private void OnGUI()
    {
        DrawSearchControls();
        DrawResults();
    }
    
    private void DrawSearchControls()
    {
        GUILayout.Label("Component Search", EditorStyles.boldLabel);
        
        //Search options
        EditorGUILayout.BeginHorizontal();
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        exactMatch = EditorGUILayout.Toggle("Exact Match", exactMatch);
        showComponentDetails = EditorGUILayout.Toggle("Show Details", showComponentDetails);
        EditorGUILayout.EndHorizontal();

        //Search mode selection
        searchMode = GUILayout.SelectionGrid(searchMode, searchModes, 3);
        
        //Search section
        EditorGUILayout.BeginHorizontal();
        string newSearchText = EditorGUILayout.TextField(searchText);
        
        if (newSearchText != searchText)
        {
            searchText = newSearchText;
            
            searchPending = false;
            
            if (!string.IsNullOrEmpty(searchText))
            {
                pendingSearchText = searchText;
                searchPending = true;
                EditorApplication.delayCall += PerformDelayedSearch;
            }
            else
            {
                searchResults.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        //Clear
        if (GUILayout.Button("Clear Results"))
        {
            searchResults.Clear();
            searchText = "";
            searchPending = false;  
        }
    }
    
    private void DrawResults()
    {
        if (searchResults.Count > 0)
        {
            GUILayout.Label($"Found {searchResults.Count} results:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var result in searchResults)
            {
                DrawSearchResult(result);
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
    
    private void DrawSearchResult(ComponentSearchResult result)
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(result.gameObject.name, EditorStyles.linkLabel))
        {
            Selection.activeGameObject = result.gameObject;
            EditorGUIUtility.PingObject(result.gameObject);
        }
        
        if (!result.gameObject.activeInHierarchy)
        {
            GUILayout.Label("(Inactive)", EditorStyles.miniLabel, GUILayout.Width(60));
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (showComponentDetails)
        {
            EditorGUI.indentLevel++;
            GUILayout.Label($"Component: {result.componentType.Name}", EditorStyles.miniLabel);
            GUILayout.Label($"Namespace: {result.componentType.Namespace}", EditorStyles.miniLabel);
            
            if (result.component != null)
            {
                var fields = result.componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fields.Length > 0)
                {
                    GUILayout.Label("Fields:", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var field in fields)
                    {
                        var value = field.GetValue(result.component);
                        GUILayout.Label($"{field.Name}: {value}", EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }

    private void SearchForComponents()
    {
        searchResults.Clear();

        if (string.IsNullOrEmpty(searchText))
            return;

        GameObject[] allObjects = FindObjectsByType<GameObject>(includeInactive ? FindObjectsSortMode.None : FindObjectsSortMode.InstanceID);

        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();

            foreach (Component comp in components)
            {
                if (comp == null) continue;

                Type compType = comp.GetType();
                bool isMatch = false;

                isMatch = exactMatch ?
                    compType.Name.Equals(searchText, StringComparison.OrdinalIgnoreCase) :
                    compType.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

                if (isMatch)
                {
                    searchResults.Add(new ComponentSearchResult(go, compType, comp));
                }
            }
        }
    }
    
    private void PerformDelayedSearch()
    {
        if (searchPending && searchText == pendingSearchText)
        {
            SearchForComponents();
            searchPending = false;
        }
    }
    private class ComponentSearchResult
    {
        public GameObject gameObject;
        public Type componentType;
        public Component component;
        
        public ComponentSearchResult(GameObject gameObject, Type componentType, Component component)
        {
            this.gameObject = gameObject;
            this.componentType = componentType;
            this.component = component;
        }
    }
}