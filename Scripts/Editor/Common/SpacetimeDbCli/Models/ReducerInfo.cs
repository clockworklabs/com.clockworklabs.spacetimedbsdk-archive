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
        
        /// <returns>a list of names with CSV "{names}:{type}" to show end-users</returns>
        /// <example>
        /// <code>{ "SetFirstLastName", "name:string,age:int" }</code>
        /// Following the syntax hint, the user would then input:
        /// <code>"John 42"</code>
        /// </example>
        public List<string> SyntaxHints { get; }
        
        
        #region Common Shortcuts
        public string GetReducerName() => ReducerEntity.Schema.SchemaName;

        /// Eg: "{firstName:string} {age:int}"
        /// If styled, brackets will become red; types will become blue.
        public string GetFriendlySyntaxHints(bool styled)
        {
            string friendlySyntaxHints = string.Join(" ", SyntaxHints);
            if (!styled)
                return friendlySyntaxHints;

            string redColorPrefix = $"<color={SpacetimeMeta.ERROR_COLOR_HEX}>"; 
            string blueColorPrefix = $"<color={SpacetimeMeta.SPECIAL_COLOR_HEX}>";
            
            // "Eg: "<color=red>{</color>firstName</color><color=blue>:string</color><color=red>}</color>"
            return friendlySyntaxHints
                .Replace("{", $"{redColorPrefix}{{</color>")
                .Replace("}", $"{redColorPrefix}}}</color>")
                .Replace(":", $"{blueColorPrefix}:")
                + "</color>";
        }
        
        /// Currently lowercases the types
        public List<string> GetNormalizedTypesCsv() => SyntaxHints
            .Select(s => s
                .Split(":")
                .Last()
                .ToLowerInvariant())
                // .Replace()) // TODO: Normalize actual type names to expected C# types. Eg: "i32" -> "int32"
            .ToList();
        #endregion // Common Shortcuts
        
        
        /// Sets { ReducerEntity, SyntaxHints }
        public ReducerInfo(EntityStructure.Entity entity)
        {
            this.ReducerEntity = entity;
            this.SyntaxHints = entity.Schema.Elements
                .Select(e => // eg: "firstName:string"
                    $"{e.ElementName.First().Key}:" + 
                    e.AlgebraicType.Builtin.First().Key.ToLowerInvariant())
                .ToList();
        }
    }
}
