﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bannerlord.ButterLib.Common.Extensions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace BannerlordTwitch.Util
{
    public sealed class YamlTagged : Attribute { }

    public static class YamlHelpers
    {
        public static string Serialize(object obj) => CreateDefaultSerializer().Serialize(obj);
        public static object Deserialize(string str, Type type) => CreateDefaultDeserializer().Deserialize(str, type);
        public static string SerializeUntagged(object obj) => CreateUntaggedSerializer().Serialize(obj);
        public static object DeserializeUntagged(string str, Type type) => CreateUntaggedDeserializer().Deserialize(str, type);

        public static T Deserialize<T>(string str) => (T)Deserialize(str, typeof(T));
        
        public static ISerializer CreateDefaultSerializer()
        {
            var builder = new SerializerBuilder();
            foreach ((string tag, var type) in nodeTypeResolver.Value.tagMappings)
            {
                builder.WithTagMapping(tag, type);
            }
            return builder.ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
                .DisableAliases()
                .Build();
        }

        public static IDeserializer CreateDefaultDeserializer() => new DeserializerBuilder()
            .WithNodeTypeResolver(nodeTypeResolver.Value)
            .IgnoreUnmatchedProperties()
            .Build();
        
        public static ISerializer CreateUntaggedSerializer()
        {
            var builder = new SerializerBuilder();
            return builder.ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
                .DisableAliases()
                .Build();
        }

        public static IDeserializer CreateUntaggedDeserializer() => new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        
        public static object ConvertObject(object obj, Type type) => Deserialize(Serialize(obj), type);
        public static object ConvertObjectUntagged(object obj, Type type) 
            => DeserializeUntagged(SerializeUntagged(obj), type);
        public static T ConvertObject<T>(object obj) => (T)ConvertObject(obj, typeof(T));

        private static readonly Lazy<TaggedNodeTypeResolver> nodeTypeResolver = new(() => new());
        
        // From https://github.com/aaubry/YamlDotNet/issues/343#issuecomment-424882014
        private sealed class TaggedNodeTypeResolver : INodeTypeResolver
        {
            public Dictionary<string, Type> tagMappings { get; }

            public TaggedNodeTypeResolver()
            {
                // create mappings so that the yaml parser can recognize that, for example,
                // items tagged with "!ScriptableObject" should be deserialized as a ScriptableObject.
                var taggedTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => p.GetCustomAttribute<YamlTagged>() != null);
                
                tagMappings = taggedTypes.ToDictionary(t => "!" + t.Name, t => t);
            }

            bool INodeTypeResolver.Resolve(NodeEvent nodeEvent, ref Type currentType)
            {
                if (nodeEvent == null || nodeEvent.Tag.IsEmpty)
                {
                    return false;
                }

                string typeName = nodeEvent.Tag.Value; // this is what gets the "!TargetingData" tag from the yaml
                bool arrayType = false;
                if (typeName.EndsWith("[]")) // this handles tags for array types like "!TargetingData[]"
                {
                    arrayType = true;
                    typeName = typeName.Substring(0, typeName.Length-2);
                }

                if (tagMappings.TryGetValue(typeName, out var predefinedType))
                {
                    currentType = arrayType ? predefinedType.MakeArrayType() : predefinedType;
                    return true;
                }
                else
                {
                    throw new YamlException(
                        $"I can't find the type '{nodeEvent.Tag}'. Is it spelled correctly? If there are" +
                        $" multiple types named '{nodeEvent.Tag}', you must used the fully qualified type name.");
                }
            }
        }
    }
}