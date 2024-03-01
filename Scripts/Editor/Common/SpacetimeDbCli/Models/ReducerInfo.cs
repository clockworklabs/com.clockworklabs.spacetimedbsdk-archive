using System.Collections.Generic;
using System.Linq;

namespace SpacetimeDB.Editor
{
    /// Contains parsed reducers Entity info from`spacetime describe`
    /// Contains friendly syntax hints for end-users
    public class ReducerInfo
    {
        /// Raw Entity structure
        public EntityStructure.Entity ReducerEntity { get; }
        
        /// <returns>a raw list of names with CSV "{names}:{type}" to show end-users</returns>
        /// <remarks>See GetNormalizedStyledSyntaxHints() || GetNormalizedTypesCsv() for parsing opts</remarks>
        public List<string> RawSyntaxHints { get; }
        
        
        #region Common Shortcuts
        public string GetReducerName() => ReducerEntity.Schema.SchemaName;

        /// Eg: [ {firstName:string},{age:int} ]"
        /// - For unstyled, just get the RawSyntaxHints list.
        public List<string> GetNormalizedStyledSyntaxHints()
        {
            const string nameColor = ""; // Use the default label color
            string bracketColor = $"<color={SpacetimeMeta.ERROR_COLOR_HEX}>"; 
            const string typeColor = "<color=white>";
            
            // "Eg: "<color=red>{</color>firstName</color><color=blue>:string</color><color=red>}</color>"
            return GetNormalizedSyntaxHints().Select(s => 
                $"{bracketColor}{{</color>{nameColor}{s}" // Color the name and bracket
                    .Replace(":", $"</color>{typeColor}:") // Keep type color open for the entire type
                    + $"</color>{bracketColor}}}</color>") // Close off the blue tag and color the bracket
                    .ToList();
        }
        
        /// Lowercases the types -> replaces known Rust types with C# types
        public List<string> GetNormalizedSyntaxHints() => RawSyntaxHints
            .Select(s => s
                .ToLowerInvariant()
                .Replace("i32", "int32")
                .Replace("i16", "short")
                .Replace("i64", "long"))
            .ToList();
        
        /// Lowercases the types -> replaces known Rust types with C# types
        public List<string> GetNormalizedTypesCsv() => GetNormalizedSyntaxHints()
            .Select(s => s
                .Split(":")
                .Last())
            .ToList();
        #endregion // Common Shortcuts
        
        
        /// Sets { ReducerEntity, RawSyntaxHints }
        public ReducerInfo(EntityStructure.Entity entity)
        {
            this.ReducerEntity = entity;
            this.RawSyntaxHints = entity.Schema.Elements
                .Select(e => // eg: "firstName:string"
                    $"{e.ElementName.First().Value}:" + 
                    e.AlgebraicType.Builtin.First().Key.ToLowerInvariant())
                .ToList();
        }
    }
}
