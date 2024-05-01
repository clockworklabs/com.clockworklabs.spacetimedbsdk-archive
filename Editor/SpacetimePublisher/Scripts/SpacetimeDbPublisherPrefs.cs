using System;
using UnityEngine;

namespace SpacetimeDB.Editor
{
    /// <summary>
    /// Prefs for SpacetimeDB editor tools
    /// </summary>
    [Serializable, CreateAssetMenu(fileName = "SpacetimeDbPublisherPrefs", menuName = "SpacetimeDB/PublisherPrefs")]
    public class SpacetimeDbPublisherPrefs : ScriptableObject
    {
        [Header("Publisher Prefs")]
        [SerializeField] private string topBannerClickLink = "https://spacetimedb.com/docs/modules";
        public string TopBannerClickLink => topBannerClickLink;

        [SerializeField] private string installDocsUrl = "https://spacetimedb.com/install";
        public string InstallDocsUrl => installDocsUrl;

        [SerializeField] private string installWasmOptUrl = "https://github.com/WebAssembly/binaryen/releases";
        public string InstallWasmOptUrl => installWasmOptUrl;

        [SerializeField] private string publisherDirPath = "Assets/SpacetimeDB/SpacetimePublisher";
        public string PublisherDirPath => publisherDirPath;

        [SerializeField] private string pathToUxml = "Assets/SpacetimeDB/SpacetimePublisher/PublisherWindowComponents.uxml";
        public string PathToUxml => pathToUxml;

        [SerializeField] private string pathToUss = "Assets/SpacetimeDB/SpacetimePublisher/PublisherWindowStyles.uss";
        public string PathToUss => pathToUss;

        [SerializeField] private string pathToAutogenDir = "Assets/StdbAutogen";
        public string PathToAutogenDir => pathToAutogenDir;
    }
}