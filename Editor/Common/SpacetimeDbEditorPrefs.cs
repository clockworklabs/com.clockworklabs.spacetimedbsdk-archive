// using UnityEngine;
//
// namespace SpacetimeDB.Editor
// {
//     /// Prefs for SpacetimeDB editor tools
//     /// TODO: This file is not yet used!
//     /// TODO: Pull data from SpacetimeDbMeta, PublisherMeta, etc (using the current val as = default val) 
//     [CreateAssetMenu(fileName = "SpacetimeDbEditorPrefs", menuName = "SpacetimeDB/EditorPrefs")]
//     public class SpacetimeDbEditorPrefs : ScriptableObject
//     {
//         [Header("Common Prefs (TODO: Pull more from SpacetimeDbMeta)")] 
//         [SerializeField, Tooltip("TODO")] 
//         private string pathToCustomTerminal = null; // TODO
//         public string PathToCustomTerminal => pathToCustomTerminal;
//
//         // #########################################################################
//         // TODO: Import from SpacetimeDbMeta, using the current val as = default val
//         // #########################################################################
//         
//         [Header("Other Prefs")]
//         [SerializeField, Tooltip("Drag + Drop from ../SpacetimePublisher/")] 
//         private SpacetimeDbPublisherPrefs spacetimeDbPublisherPrefs;
//         public SpacetimeDbPublisherPrefs SpacetimeDbPublisherPrefs => spacetimeDbPublisherPrefs;
//     }
// }