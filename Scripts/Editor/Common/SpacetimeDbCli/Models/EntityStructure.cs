using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SpacetimeDB.Editor
{
    /// Parses the entity JSON data from`spacetime describe {server}`
    [Serializable]
    public class EntityStructure
    {
        #region Root Props
        [JsonProperty("entities")]
        public Dictionary<string, Entity> EntitiesDict { get; set; }
        
        [JsonIgnore]
        public List<ReducerInfo> ReducersInfo{ get; set; }
        #endregion // Root Props
        
        
        #region Child Classes
        public class Entity
        {
            [JsonProperty("arity")]
            public int Arity { get; set; }

            [JsonProperty("schema")]
            public Schema Schema { get; set; }

            [JsonProperty("type")]
            public string EntityType { get; set; }
        }

        public class Schema
        {
            [JsonProperty("elements")]
            public List<Element> Elements { get; set; }

            [JsonProperty("name")]
            public string SchemaName { get; set; }
        }

        public class Element
        {
            [JsonProperty("algebraic_type")]
            public AlgebraicType AlgebraicType { get; set; }

            [JsonProperty("name")]
            public Dictionary<string, string> ElementName { get; set; }
        }

        public class AlgebraicType
        {
            [JsonProperty("Builtin")]
            public Dictionary<string, object> Builtin { get; set; }
        }
        #endregion // Child Classes
        
        
        /// Parses non-server data *after* json is deserialized
        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            Dictionary<string, Entity> reducers = getReducers();
            if (reducers is null)
                return;
            
            this.ReducersInfo = new List<ReducerInfo>();

            // For every reducer, create a new ReducerInfo, passing in the Entity
            this.ReducersInfo = reducers
                .Select(r => new ReducerInfo(r.Value))
                .ToList();
        }
        

        #region Utils
        private Dictionary<string, Entity> getReducers()
        {
            Dictionary<string, Entity> reducersDict = EntitiesDict?
                .Where(e => e.Value.EntityType == "reducer")
                .ToDictionary(e => e.Key, e => e.Value);

            return reducersDict;
        }
        
        /// isSuccess?
        public bool HasEntities => EntitiesDict is { Count: > 0 };

        public List<string> GetReducerNames() =>
            getReducers().Keys.ToList();

        public override string ToString() =>
            JsonConvert.SerializeObject(EntitiesDict);
        #endregion // Utils
    }
}