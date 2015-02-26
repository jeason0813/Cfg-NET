﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Transformalize.Libs.Cfg.Net {

    public static class CfgConstants {

        // ReSharper disable InconsistentNaming
        public static char ENTITY_END = ';';
        public static char ENTITY_START = '&';
        public static char HIGH_SURROGATE = '\uD800';
        public static char LOW_SURROGATE = '\uDC00';
        public static char PLACE_HOLDER_FIRST = '@';
        public static char PLACE_HOLDER_LAST = ')';
        public static char PLACE_HOLDER_SECOND = '(';
        public static int UNICODE_00_END = 0x00FFFF;
        public static int UNICODE_01_START = 0x10000;
        public static string ENVIRONMENTS_DEFAULT_NAME = "default";
        public static string ENVIRONMENTS_ELEMENT_NAME = "environments";
        public static string PARAMETERS_ELEMENT_NAME = "parameters";

        //PROBLEM PATTERNS
        public static string PROBLEM_DUPLICATE_SET = "You set a duplicate '{0}' value '{1}' in '{2}'.";
        public static string PROBLEM_INVALID_ATTRIBUTE = "A{3} '{0}' '{1}' element contains an invalid '{2}' attribute.  Valid attributes are: {4}.";
        public static string PROBLEM_INVALID_ELEMENT = "A{2} '{0}' element has an invalid '{1}' element.  If you need a{2} '{1}' element, decorate it with the Cfg[()] attribute in your Cfg-NET model.";
        public static string PROBLEM_INVALID_NESTED_ELEMENT = "A{3} '{0}' '{1}' element has an invalid '{2}' element.";
        public static string PROBLEM_MISSING_ADD_ELEMENT = "A{1} '{0}' element is missing an 'add' element.";
        public static string PROBLEM_MISSING_ATTRIBUTE = "A{3} '{0}' '{1}' element is missing a '{2}' attribute.";
        public static string PROBLEM_MISSING_ELEMENT = "The '{0}' element is missing a{2} '{1}' element.";
        public static string PROBLEM_MISSING_NESTED_ELEMENT = "A{3} '{0}' '{1}' element is missing a{4} '{2}' element.";
        public static string PROBLEM_MISSING_PLACE_HOLDER_VALUE = "You're missing {0} for {1}.";
        public static string PROBLEM_SETTING_VALUE = "Could not set '{0}' to '{1}' inside '{2}' '{3}'. {4}";
        public static string PROBLEM_UNEXPECTED_ELEMENT = "Invalid element {0} in {1}.  Only 'add' elements are allowed here.";
        public static string PROBLEM_XML_PARSE = "Could not parse the configuration. {0}";
        public static string PROBLEM_VALUE_NOT_IN_DOMAIN = "A{5} '{0}' '{1}' element has an invalid value of '{3}' in the '{2}' attribute.  The valid domain is: {4}.";
        public static string PROBLEM_ROOT_VALUE_NOT_IN_DOMAIN = "The root element has an invalid value of '{0}' in the '{1}' attribute.  The valid domain is: {2}.";
        public static string PROBLEM_SHARED_PROPERTY_MISSING = "A{3} '{0}' shared property '{1}' is missing in '{2}'.  Make sure it is defined and decorated with [Cfg()].";
        public static string PROBLEM_ONLY_ONE_ATTRIBUTE_ALLOWED = "You must have exactly 1 attribute in '{0}' '{1}'.  You have {2}.";
        public static string PROBLEM_TYPE_MISMATCH = "The `{0}` attribute's default value's type ({1}) does not match the property type ({2}).";
        // ReSharper restore InconsistentNaming
    }


    public class CfgProblems {

        private readonly StringBuilder _storage = new StringBuilder();

        public void DuplicateSet(string uniqueAttribute, object value, string nodeName) {
            _storage.AppendFormat(CfgConstants.PROBLEM_DUPLICATE_SET, uniqueAttribute, value, nodeName);
            _storage.AppendLine();
        }

        public void InvalidAttribute(string parentName, string nodeName, string attributeName, string validateAttributes) {
            _storage.AppendFormat(CfgConstants.PROBLEM_INVALID_ATTRIBUTE, parentName, nodeName, attributeName, Suffix(parentName), validateAttributes);
            _storage.AppendLine();
        }

        public void InvalidElement(string nodeName, string subNodeName) {
            _storage.AppendFormat(CfgConstants.PROBLEM_INVALID_ELEMENT, nodeName, subNodeName, Suffix(nodeName));
            _storage.AppendLine();
        }

        public void InvalidNestedElement(string parentName, string nodeName, string subNodeName) {
            _storage.AppendFormat(CfgConstants.PROBLEM_INVALID_NESTED_ELEMENT, parentName, nodeName, subNodeName, Suffix(parentName));
            _storage.AppendLine();
        }

        public void MissingAttribute(string parentName, string nodeName, string attributeName) {
            _storage.AppendFormat(CfgConstants.PROBLEM_MISSING_ATTRIBUTE, parentName, nodeName, attributeName, Suffix(parentName));
            _storage.AppendLine();
        }

        public void MissingElement(string nodeName, string elementName) {
            _storage.AppendFormat(CfgConstants.PROBLEM_MISSING_ELEMENT, nodeName, elementName, Suffix(elementName));
            _storage.AppendLine();
        }

        public void MissingAddElement(string elementName) {
            _storage.AppendFormat(CfgConstants.PROBLEM_MISSING_ADD_ELEMENT, elementName, Suffix(elementName));
            _storage.AppendLine();
        }

        public void MissingNestedElement(string parentName, string nodeName, string elementName) {
            _storage.AppendFormat(CfgConstants.PROBLEM_MISSING_NESTED_ELEMENT, parentName, nodeName, elementName, Suffix(parentName), Suffix(elementName));
            _storage.AppendLine();
        }

        public void MissingPlaceHolderValues(string[] keys) {
            var formatted = "@(" + string.Join("), @(", keys) + ")";
            _storage.AppendFormat(CfgConstants.PROBLEM_MISSING_PLACE_HOLDER_VALUE, keys.Length == 1 ? "a value" : "values", formatted);
            _storage.AppendLine();
        }

        public void SettingValue(string propertyName, object value, string parentName, string nodeName, string message) {
            _storage.AppendFormat(CfgConstants.PROBLEM_SETTING_VALUE, propertyName, value, parentName, nodeName, message);
            _storage.AppendLine();
        }

        public void UnexpectedElement(string elementName, string subNodeName) {
            _storage.AppendFormat(CfgConstants.PROBLEM_UNEXPECTED_ELEMENT, subNodeName, elementName);
            _storage.AppendLine();
        }

        public void ValueNotInDomain(string parentName, string nodeName, string propertyName, object value, string validValues) {
            _storage.AppendFormat(CfgConstants.PROBLEM_VALUE_NOT_IN_DOMAIN, parentName, nodeName, propertyName, value, validValues, Suffix(parentName));
            _storage.AppendLine();
        }

        public void RootValueNotInDomain(object value, string propertyName, string validValues) {
            _storage.AppendFormat(CfgConstants.PROBLEM_ROOT_VALUE_NOT_IN_DOMAIN, value, propertyName, validValues);
            _storage.AppendLine();
        }

        public void SharedPropertyMissing(string name, string sharedProperty, string listType) {
            var type = listType.IndexOf('.') > 0 ? listType.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Last() : listType;
            _storage.AppendFormat(CfgConstants.PROBLEM_SHARED_PROPERTY_MISSING, name, sharedProperty, type, Suffix(name));
            _storage.AppendLine();
        }

        public void XmlParse(string message) {
            _storage.AppendFormat(CfgConstants.PROBLEM_XML_PARSE, message);
            _storage.AppendLine();
        }

        private static string Suffix(string thing) {
            return thing == null || IsVowel(thing[0]) ? "n" : string.Empty;
        }

        public string[] Yield() {
            return _storage.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        }

        public void AddCustomProblem(string problem, params object[] args) {
            _storage.AppendFormat(problem, args);
            _storage.AppendLine();
        }

        private static bool IsVowel(char c) {
            return c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u' || c == 'A' || c == 'E' || c == 'I' ||
                   c == 'O' || c == 'U';
        }

        public void OnlyOneAttributeAllowed(string parentName, string name, int count) {
            _storage.AppendFormat(CfgConstants.PROBLEM_ONLY_ONE_ATTRIBUTE_ALLOWED, parentName, name, count);
            _storage.AppendLine();
        }

        public void TypeMismatch(string key, Type defaultType, Type propertyType) {
            _storage.AppendFormat(CfgConstants.PROBLEM_TYPE_MISMATCH, key, defaultType, propertyType);
            _storage.AppendLine();
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CfgAttribute : Attribute {
        // ReSharper disable InconsistentNaming
        public object value { get; set; }
        public bool required { get; set; }
        public bool unique { get; set; }
        public string sharedProperty { get; set; }
        public object sharedValue { get; set; }
        public string domain { get; set; }
        public char domainDelimiter { get; set; }
        public bool ignoreCase { get; set; }
        // ReSharper restore InconsistentNaming
    }

    public class CfgMetadata {
        private readonly HashSet<string> _domainSet;

        public PropertyInfo PropertyInfo { get; set; }
        public CfgAttribute Attribute { get; set; }
        public Type ListType { get; set; }
        public Func<CfgNode> Loader { get; set; }
        public string[] UniquePropertiesInList { get; set; }
        public string SharedProperty { get; set; }
        public object SharedValue { get; set; }
        public Action<object, object> Setter { get; set; }
        public Func<object, object> Getter { get; set; }
        public bool TypeMismatch { get; set; }

        public CfgMetadata(PropertyInfo propertyInfo, CfgAttribute attribute) {
            PropertyInfo = propertyInfo;
            Attribute = attribute;

            if (string.IsNullOrEmpty(attribute.domain))
                return;

            if (attribute.domainDelimiter == default(char)) {
                attribute.domainDelimiter = ',';
            }

            if (attribute.ignoreCase) {
                _domainSet = new HashSet<string>(attribute.domain.Split(new[] { attribute.domainDelimiter }, StringSplitOptions.None), StringComparer.OrdinalIgnoreCase);
            } else {
                _domainSet = new HashSet<string>(attribute.domain.Split(new[] { attribute.domainDelimiter }, StringSplitOptions.None), StringComparer.Ordinal);
            }
        }

        public bool IsInDomain(object value) {
            return _domainSet == null || (value != null && _domainSet.Contains(value.ToString()));
        }
    }

    public abstract class CfgNode {

        private static readonly ConcurrentDictionary<Type, Dictionary<string, CfgMetadata>> MetadataCache = new ConcurrentDictionary<Type, Dictionary<string, CfgMetadata>>();
        private static readonly ConcurrentDictionary<Type, List<string>> PropertyCache = new ConcurrentDictionary<Type, List<string>>();
        private static readonly ConcurrentDictionary<Type, List<string>> ElementCache = new ConcurrentDictionary<Type, List<string>>();
        private static readonly ConcurrentDictionary<Type, Dictionary<string, string>> NameCache = new ConcurrentDictionary<Type, Dictionary<string, string>>();

        private readonly Dictionary<string, string> _uniqueProperties = new Dictionary<string, string>();
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly CfgProblems _problems = new CfgProblems();
        private readonly Type _type;
        private readonly Dictionary<string, CfgMetadata> _metadata;
        private static Dictionary<string, char> _entities;
        private NanoXmlNode _node;

        protected CfgNode() {
            _type = GetType();
            _metadata = GetMetadata(_type, _problems, _builder);
            SetDefaults(this, _metadata);
        }

        private static Dictionary<Type, Func<string, object>> Converter {
            get {
                return new Dictionary<Type, Func<string, object>> {
                    {typeof(String), (x => x)},
                    {typeof(Guid), (x => Guid.Parse(x))},
                    {typeof(Int16), (x => Convert.ToInt16(x))},
                    {typeof(Int32), (x => Convert.ToInt32(x))},
                    {typeof(Int64), (x => Convert.ToInt64(x))},
                    {typeof(UInt16), (x => Convert.ToUInt16(x))},
                    {typeof(UInt32), (x => Convert.ToUInt32(x))},
                    {typeof(UInt64), (x => Convert.ToUInt64(x))},
                    {typeof(Double), (x => Convert.ToDouble(x))},
                    {typeof(Decimal), (x => Decimal.Parse(x, NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowCurrencySymbol, (IFormatProvider)CultureInfo.CurrentCulture.GetFormat(typeof(NumberFormatInfo))))},
                    {typeof(Char), (x => Convert.ToChar(x))},
                    {typeof(DateTime), (x => Convert.ToDateTime(x))},
                    {typeof(Boolean), (x => Convert.ToBoolean(x))},
                    {typeof(Single), (x => Convert.ToSingle(x))},
                    {typeof(Byte), (x => Convert.ToByte(x))}
                };
            }
        }

        /// <summary>
        /// Get any type that inherits from CfgNode with default values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setter"></param>
        /// <returns></returns>
        public T GetDefaultOf<T>(Action<T> setter = null) {
            var obj = Activator.CreateInstance(typeof(T));
            var metadata = GetMetadata(typeof(T), _problems, _builder);

            SetDefaults(obj, metadata);

            if (setter != null) {
                setter((T)obj);
            }

            ((CfgNode)obj).Modify();

            return (T)obj;
        }

        private static void SetDefaults(object obj, Dictionary<string, CfgMetadata> metadata) {
            foreach (var pair in metadata) {
                if (pair.Value.PropertyInfo.PropertyType.IsGenericType) {
                    pair.Value.Setter(obj, Activator.CreateInstance(pair.Value.PropertyInfo.PropertyType));
                } else {
                    if (!pair.Value.TypeMismatch) {
                        pair.Value.Setter(obj, pair.Value.Attribute.value);
                    }
                }
            }
        }

        protected void AddProblem(string problem, params object[] args) {
            _problems.AddCustomProblem(problem, args);
        }

        public void Load(string xml, Dictionary<string, string> parameters = null) {

            try {
                _node = new NanoXmlDocument(xml).RootNode;
                var environmentDefaults = LoadEnvironment(_node, parameters).ToArray();
                if (environmentDefaults.Length > 0) {
                    for (var i = 0; i < environmentDefaults.Length; i++) {
                        if (i == 0 && parameters == null) {
                            parameters = new Dictionary<string, string>(StringComparer.Ordinal);
                        }
                        if (!parameters.ContainsKey(environmentDefaults[i][0])) {
                            parameters[environmentDefaults[i][0]] = environmentDefaults[i][1];
                        }
                    }
                }
            } catch (Exception ex) {
                _problems.XmlParse(ex.Message);
                return;
            }

            LoadProperties(_node, null, parameters);
            LoadCollections(_node, null, parameters);
            Modify();
            Validate();
        }

        protected IEnumerable<string[]> LoadEnvironment(NanoXmlNode node, Dictionary<string, string> parameters) {

            for (var i = 0; i < node.SubNodes.Count; i++) {
                var environmentsNode = node.SubNodes[i];
                if (environmentsNode.Name != CfgConstants.ENVIRONMENTS_ELEMENT_NAME)
                    continue;

                if (!environmentsNode.HasSubNode())
                    break;

                NanoXmlNode environmentNode;

                if (environmentsNode.SubNodes.Count > 1) {
                    NanoXmlAttribute defaultEnvironment;
                    if (!environmentsNode.TryAttribute(CfgConstants.ENVIRONMENTS_DEFAULT_NAME, out defaultEnvironment))
                        continue;

                    for (var j = 0; j < environmentsNode.SubNodes.Count; j++) {
                        environmentNode = environmentsNode.SubNodes[j];

                        NanoXmlAttribute environmentName;
                        if (!environmentNode.TryAttribute("name", out environmentName))
                            continue;

                        var value = CheckParameters(parameters, defaultEnvironment.Value);

                        if (value != environmentName.Value || !environmentNode.HasSubNode())
                            continue;

                        return GetParameters(environmentNode.SubNodes[0]);
                    }
                }

                environmentNode = environmentsNode.SubNodes[0];
                if (!environmentNode.HasSubNode())
                    break;

                var parametersNode = environmentNode.SubNodes[0];

                if (parametersNode.Name != CfgConstants.PARAMETERS_ELEMENT_NAME || !parametersNode.HasSubNode())
                    break;

                return GetParameters(parametersNode);

            }

            return Enumerable.Empty<string[]>();
        }

        private static IEnumerable<string[]> GetParameters(NanoXmlNode parametersNode) {
            var parameters = new List<string[]>();

            for (var j = 0; j < parametersNode.SubNodes.Count; j++) {
                var parameter = parametersNode.SubNodes[j];
                string name = null;
                string value = null;
                for (var k = 0; k < parameter.Attributes.Count; k++) {
                    var attribute = parameter.Attributes[k];
                    switch (attribute.Name) {
                        case "name":
                            name = attribute.Value;
                            break;
                        case "value":
                            value = attribute.Value;
                            break;
                    }
                }
                if (name != null && value != null) {
                    parameters.Add(new[] { name, value });
                }
            }
            return parameters;
        }

        protected CfgNode Load(NanoXmlNode node, string parentName, Dictionary<string, string> parameters) {
            _node = node;
            LoadProperties(_node, parentName, parameters);
            LoadCollections(_node, parentName, parameters);
            Modify();
            Validate();
            return this;
        }

        //returns original xml string, does not reflect modifications
        public override string ToString() {
            return _node == null ? string.Empty : _node.ToString();
        }

        /// <summary>
        /// Override to add custom validation.  Use `AddProblem()` to add problems.
        /// </summary>
        protected virtual void Validate() { }

        /// <summary>
        /// Override for custom modifications.
        /// </summary>
        protected virtual void Modify() { }

        private void LoadCollections(NanoXmlNode node, string parentName, Dictionary<string, string> parameters = null) {

            var keys = ElementCache[_type];
            var elements = new Dictionary<string, IList>();
            var elementHits = new HashSet<string>();
            var addHits = new HashSet<string>();

            //initialize all the lists
            for (var i = 0; i < keys.Count; i++) {
                var key = keys[i];
                elements.Add(key, (IList)_metadata[key].Getter(this));
            }

            for (var i = 0; i < node.SubNodes.Count; i++) {
                var subNode = node.SubNodes[i];
                var subNodeKey = NormalizeName(_type, subNode.Name, _builder);
                if (_metadata.ContainsKey(subNodeKey)) {
                    elementHits.Add(subNodeKey);
                    var item = _metadata[subNodeKey];

                    object value = null;
                    CfgMetadata sharedCfg = null;

                    if (item.SharedProperty != null) {
                        var sharedMetadata = GetMetadata(item.ListType, _problems, _builder);
                        if (sharedMetadata.ContainsKey(item.SharedProperty)) {
                            sharedCfg = sharedMetadata[item.SharedProperty];
                        } else {
                            _problems.SharedPropertyMissing(subNode.Name, item.SharedProperty, item.ListType.ToString());
                        }
                        NanoXmlAttribute sharedAttribute;
                        if (subNode.TryAttribute(item.SharedProperty, out sharedAttribute)) {
                            value = sharedAttribute.Value ?? item.SharedValue;
                        }
                    }

                    for (var j = 0; j < subNode.SubNodes.Count; j++) {
                        var add = subNode.SubNodes[j];
                        if (add.Name.Equals("add", StringComparison.Ordinal))
                        {
                            var addKey = NormalizeName(_type, subNode.Name, _builder);
                            addHits.Add(addKey);
                            if (item.Loader == null) {
                                if (add.Attributes.Count == 1) {
                                    var attrValue = add.Attributes[0].Value;
                                    if (item.ListType == typeof(string) || item.ListType == typeof(object)) {
                                        elements[addKey].Add(attrValue);
                                    } else {
                                        try {
                                            elements[addKey].Add(Converter[item.ListType](attrValue));
                                        } catch (Exception ex) {
                                            _problems.SettingValue(subNode.Name, attrValue, parentName, subNode.Name, ex.Message);
                                        }
                                    }
                                } else {
                                    _problems.OnlyOneAttributeAllowed(parentName, subNode.Name, add.Attributes.Count);
                                }
                            } else {
                                var loaded = item.Loader().Load(add, subNode.Name, parameters);
                                if (sharedCfg != null) {
                                    var sharedValue = sharedCfg.Getter(loaded);
                                    if (sharedValue == null) {
                                        sharedCfg.Setter(loaded, value ?? item.SharedValue);
                                    }
                                }
                                elements[addKey].Add(loaded);
                            }
                        } else {
                            _problems.UnexpectedElement(add.Name, subNode.Name);
                        }
                    }
                } else {
                    if (parentName == null) {
                        _problems.InvalidElement(node.Name, subNode.Name);
                    } else {
                        _problems.InvalidNestedElement(parentName, node.Name, subNode.Name);
                    }
                }
            }

            // check for duplicates of unique properties required to be unique in collections
            for (var i = 0; i < keys.Count; i++) {
                var key = keys[i];
                var item = _metadata[key];
                var list = elements[key];

                if (list.Count > 1) {
                    if (item.UniquePropertiesInList.Length > 0) {
                        for (var j = 0; j < item.UniquePropertiesInList.Length; j++) {
                            var unique = item.UniquePropertiesInList[j];
                            var duplicates = list
                                .Cast<CfgNode>()
                                .Where(n => n.UniqueProperties.ContainsKey(unique))
                                .Select(n => n.UniqueProperties[unique])
                                .GroupBy(n => n)
                                .Where(group => group.Count() > 1)
                                .Select(group => group.Key)
                                .ToArray();

                            for (var l = 0; l < duplicates.Length; l++) {
                                _problems.DuplicateSet(unique, duplicates[l], key);
                            }
                        }

                    }
                } else if (list.Count == 0 && item.Attribute.required) {
                    if (elementHits.Contains(key) && !addHits.Contains(key)) {
                        _problems.MissingAddElement(key);
                    } else {
                        if (parentName == null) {
                            _problems.MissingElement(node.Name, key);
                        } else {
                            _problems.MissingNestedElement(parentName, node.Name, key);
                        }
                    }
                }

            }
        }

        private static string NormalizeName(Type type, string name, StringBuilder builder) {
            var cache = NameCache[type];
            if (cache.ContainsKey(name)) {
                return cache[name];
            }
            builder.Clear();
            for (var i = 0; i < name.Length; i++) {
                var character = name[i];
                if (char.IsLetterOrDigit(character)) {
                    builder.Append(char.IsUpper(character) ? char.ToLowerInvariant(character) : character);
                }
            }
            var result = builder.ToString();
            cache[name] = result;
            return result;
        }

        private void LoadProperties(NanoXmlNode node, string parentName, IDictionary<string, string> parameters = null) {

            var keys = PropertyCache[_type];

            if (keys.Count == 0)
                return;

            var keyHits = new HashSet<string>();

            for (var i = 0; i < node.Attributes.Count; i++) {
                var attribute = node.Attributes[i];
                var attributeKey = NormalizeName(_type, attribute.Name, _builder);
                if (_metadata.ContainsKey(attributeKey)) {

                    if (attribute.Value == null)
                        continue;

                    var decoded = false;
                    attribute.Value = CheckParameters(parameters, attribute.Value);

                    if (attribute.Value.IndexOf(CfgConstants.ENTITY_START) > -1) {
                        attribute.Value = Decode(attribute.Value, _builder);
                        decoded = true;
                    }

                    var item = _metadata[attributeKey];

                    if (item.Attribute.unique) {
                        UniqueProperties[attributeKey] = attribute.Value;
                    }

                    if (item.PropertyInfo.PropertyType == typeof(string) || item.PropertyInfo.PropertyType == typeof(object)) {
                        item.Setter(this, attribute.Value);
                        keyHits.Add(attributeKey);
                    } else {
                        try {
                            item.Setter(this, Converter[item.PropertyInfo.PropertyType](attribute.Value));
                            keyHits.Add(attributeKey);
                        } catch (Exception ex) {
                            _problems.SettingValue(attribute.Name, attribute.Value, parentName, node.Name, ex.Message);
                        }
                    }

                    // Setter has been called and may have changed the value
                    var value = item.Getter(this);

                    if (!item.IsInDomain(value)) {
                        if (parentName == null) {
                            _problems.RootValueNotInDomain(value, attribute.Name, item.Attribute.domain.Replace(item.Attribute.domainDelimiter.ToString(CultureInfo.InvariantCulture), ", "));
                        } else {
                            _problems.ValueNotInDomain(parentName, node.Name, attribute.Name, value, item.Attribute.domain.Replace(item.Attribute.domainDelimiter.ToString(CultureInfo.InvariantCulture), ", "));
                        }
                    }

                    attribute.Value = decoded ? Encode(value.ToString(), _builder) : value.ToString();
                } else {
                    _problems.InvalidAttribute(parentName, node.Name, attribute.Name, string.Join(", ", keys));
                }
            }

            // missing any required attributes?
            foreach (var key in keys.Except(keyHits)) {
                var item = _metadata[key];
                if (item.Attribute.required) {
                    _problems.MissingAttribute(parentName, node.Name, key);
                }
            }

        }

        private string CheckParameters(IDictionary<string, string> parameters, string input) {
            if (parameters == null || input.IndexOf('@') < 0)
                return input;
            var response = ReplaceParameters(input, parameters, _builder);
            if (response.Item2.Length > 1) {
                _problems.MissingPlaceHolderValues(response.Item2);
            }
            return response.Item1;
        }

        private static Tuple<string, string[]> ReplaceParameters(string value, IDictionary<string, string> parameters, StringBuilder builder) {
            builder.Clear();
            List<string> badKeys = null;
            for (var j = 0; j < value.Length; j++) {
                if (value[j] == CfgConstants.PLACE_HOLDER_FIRST &&
                    value.Length > j + 1 &&
                    value[j + 1] == CfgConstants.PLACE_HOLDER_SECOND) {
                    var length = 2;
                    while (value.Length > j + length && value[j + length] != CfgConstants.PLACE_HOLDER_LAST) {
                        length++;
                    }
                    if (length > 2) {
                        var key = value.Substring(j + 2, length - 2);
                        if (parameters.ContainsKey(key)) {
                            builder.Append(parameters[key]);
                        } else {
                            if (badKeys == null) {
                                badKeys = new List<string> { key };
                            } else {
                                badKeys.Add(key);
                            }
                            builder.AppendFormat("@({0})", key);
                        }
                    }
                    j = j + length;
                } else {
                    builder.Append(value[j]);
                }
            }
            return new Tuple<string, string[]>(builder.ToString(), badKeys == null ? new string[0] : badKeys.ToArray());
        }

        protected Dictionary<string, string> UniqueProperties {
            get { return _uniqueProperties; }
        }

        private static Dictionary<string, char> Entities {
            get {
                return _entities ?? (_entities = new Dictionary<string, char>(StringComparer.Ordinal)
                {
                    {"Aacute", "\x00c1"[0]},
                    {"aacute", "\x00e1"[0]},
                    {"Acirc", "\x00c2"[0]},
                    {"acirc", "\x00e2"[0]},
                    {"acute", "\x00b4"[0]},
                    {"AElig", "\x00c6"[0]},
                    {"aelig", "\x00e6"[0]},
                    {"Agrave", "\x00c0"[0]},
                    {"agrave", "\x00e0"[0]},
                    {"alefsym", "\x2135"[0]},
                    {"Alpha", "\x0391"[0]},
                    {"alpha", "\x03b1"[0]},
                    {"amp", "\x0026"[0]},
                    {"and", "\x2227"[0]},
                    {"ang", "\x2220"[0]},
                    {"apos", "\x0027"[0]},
                    {"Aring", "\x00c5"[0]},
                    {"aring", "\x00e5"[0]},
                    {"asymp", "\x2248"[0]},
                    {"Atilde", "\x00c3"[0]},
                    {"atilde", "\x00e3"[0]},
                    {"Auml", "\x00c4"[0]},
                    {"auml", "\x00e4"[0]},
                    {"bdquo", "\x201e"[0]},
                    {"Beta", "\x0392"[0]},
                    {"beta", "\x03b2"[0]},
                    {"brvbar", "\x00a6"[0]},
                    {"bull", "\x2022"[0]},
                    {"cap", "\x2229"[0]},
                    {"Ccedil", "\x00c7"[0]},
                    {"ccedil", "\x00e7"[0]},
                    {"cedil", "\x00b8"[0]},
                    {"cent", "\x00a2"[0]},
                    {"Chi", "\x03a7"[0]},
                    {"chi", "\x03c7"[0]},
                    {"circ", "\x02c6"[0]},
                    {"clubs", "\x2663"[0]},
                    {"cong", "\x2245"[0]},
                    {"copy", "\x00a9"[0]},
                    {"crarr", "\x21b5"[0]},
                    {"cup", "\x222a"[0]},
                    {"curren", "\x00a4"[0]},
                    {"dagger", "\x2020"[0]},
                    {"Dagger", "\x2021"[0]},
                    {"darr", "\x2193"[0]},
                    {"dArr", "\x21d3"[0]},
                    {"deg", "\x00b0"[0]},
                    {"Delta", "\x0394"[0]},
                    {"delta", "\x03b4"[0]},
                    {"diams", "\x2666"[0]},
                    {"divide", "\x00f7"[0]},
                    {"Eacute", "\x00c9"[0]},
                    {"eacute", "\x00e9"[0]},
                    {"Ecirc", "\x00ca"[0]},
                    {"ecirc", "\x00ea"[0]},
                    {"Egrave", "\x00c8"[0]},
                    {"egrave", "\x00e8"[0]},
                    {"empty", "\x2205"[0]},
                    {"emsp", "\x2003"[0]},
                    {"ensp", "\x2002"[0]},
                    {"Epsilon", "\x0395"[0]},
                    {"epsilon", "\x03b5"[0]},
                    {"equiv", "\x2261"[0]},
                    {"Eta", "\x0397"[0]},
                    {"eta", "\x03b7"[0]},
                    {"ETH", "\x00d0"[0]},
                    {"eth", "\x00f0"[0]},
                    {"Euml", "\x00cb"[0]},
                    {"euml", "\x00eb"[0]},
                    {"euro", "\x20ac"[0]},
                    {"exist", "\x2203"[0]},
                    {"fnof", "\x0192"[0]},
                    {"forall", "\x2200"[0]},
                    {"frac12", "\x00bd"[0]},
                    {"frac14", "\x00bc"[0]},
                    {"frac34", "\x00be"[0]},
                    {"frasl", "\x2044"[0]},
                    {"Gamma", "\x0393"[0]},
                    {"gamma", "\x03b3"[0]},
                    {"ge", "\x2265"[0]},
                    {"gt", "\x003e"[0]},
                    {"harr", "\x2194"[0]},
                    {"hArr", "\x21d4"[0]},
                    {"hearts", "\x2665"[0]},
                    {"hellip", "\x2026"[0]},
                    {"Iacute", "\x00cd"[0]},
                    {"iacute", "\x00ed"[0]},
                    {"Icirc", "\x00ce"[0]},
                    {"icirc", "\x00ee"[0]},
                    {"iexcl", "\x00a1"[0]},
                    {"Igrave", "\x00cc"[0]},
                    {"igrave", "\x00ec"[0]},
                    {"image", "\x2111"[0]},
                    {"infin", "\x221e"[0]},
                    {"int", "\x222b"[0]},
                    {"Iota", "\x0399"[0]},
                    {"iota", "\x03b9"[0]},
                    {"iquest", "\x00bf"[0]},
                    {"isin", "\x2208"[0]},
                    {"Iuml", "\x00cf"[0]},
                    {"iuml", "\x00ef"[0]},
                    {"Kappa", "\x039a"[0]},
                    {"kappa", "\x03ba"[0]},
                    {"Lambda", "\x039b"[0]},
                    {"lambda", "\x03bb"[0]},
                    {"lang", "\x2329"[0]},
                    {"laquo", "\x00ab"[0]},
                    {"larr", "\x2190"[0]},
                    {"lArr", "\x21d0"[0]},
                    {"lceil", "\x2308"[0]},
                    {"ldquo", "\x201c"[0]},
                    {"le", "\x2264"[0]},
                    {"lfloor", "\x230a"[0]},
                    {"lowast", "\x2217"[0]},
                    {"loz", "\x25ca"[0]},
                    {"lrm", "\x200e"[0]},
                    {"lsaquo", "\x2039"[0]},
                    {"lsquo", "\x2018"[0]},
                    {"lt", "\x003c"[0]},
                    {"macr", "\x00af"[0]},
                    {"mdash", "\x2014"[0]},
                    {"micro", "\x00b5"[0]},
                    {"middot", "\x00b7"[0]},
                    {"minus", "\x2212"[0]},
                    {"Mu", "\x039c"[0]},
                    {"mu", "\x03bc"[0]},
                    {"nabla", "\x2207"[0]},
                    {"nbsp", "\x00a0"[0]},
                    {"ndash", "\x2013"[0]},
                    {"ne", "\x2260"[0]},
                    {"ni", "\x220b"[0]},
                    {"not", "\x00ac"[0]},
                    {"notin", "\x2209"[0]},
                    {"nsub", "\x2284"[0]},
                    {"Ntilde", "\x00d1"[0]},
                    {"ntilde", "\x00f1"[0]},
                    {"Nu", "\x039d"[0]},
                    {"nu", "\x03bd"[0]},
                    {"Oacute", "\x00d3"[0]},
                    {"oacute", "\x00f3"[0]},
                    {"Ocirc", "\x00d4"[0]},
                    {"ocirc", "\x00f4"[0]},
                    {"OElig", "\x0152"[0]},
                    {"oelig", "\x0153"[0]},
                    {"Ograve", "\x00d2"[0]},
                    {"ograve", "\x00f2"[0]},
                    {"oline", "\x203e"[0]},
                    {"Omega", "\x03a9"[0]},
                    {"omega", "\x03c9"[0]},
                    {"Omicron", "\x039f"[0]},
                    {"omicron", "\x03bf"[0]},
                    {"oplus", "\x2295"[0]},
                    {"or", "\x2228"[0]},
                    {"ordf", "\x00aa"[0]},
                    {"ordm", "\x00ba"[0]},
                    {"Oslash", "\x00d8"[0]},
                    {"oslash", "\x00f8"[0]},
                    {"Otilde", "\x00d5"[0]},
                    {"otilde", "\x00f5"[0]},
                    {"otimes", "\x2297"[0]},
                    {"Ouml", "\x00d6"[0]},
                    {"ouml", "\x00f6"[0]},
                    {"para", "\x00b6"[0]},
                    {"part", "\x2202"[0]},
                    {"permil", "\x2030"[0]},
                    {"perp", "\x22a5"[0]},
                    {"Phi", "\x03a6"[0]},
                    {"phi", "\x03c6"[0]},
                    {"Pi", "\x03a0"[0]},
                    {"pi", "\x03c0"[0]},
                    {"piv", "\x03d6"[0]},
                    {"plusmn", "\x00b1"[0]},
                    {"pound", "\x00a3"[0]},
                    {"prime", "\x2032"[0]},
                    {"Prime", "\x2033"[0]},
                    {"prod", "\x220f"[0]},
                    {"prop", "\x221d"[0]},
                    {"Psi", "\x03a8"[0]},
                    {"psi", "\x03c8"[0]},
                    {"quot", "\x0022"[0]},
                    {"radic", "\x221a"[0]},
                    {"rang", "\x232a"[0]},
                    {"raquo", "\x00bb"[0]},
                    {"rarr", "\x2192"[0]},
                    {"rArr", "\x21d2"[0]},
                    {"rceil", "\x2309"[0]},
                    {"rdquo", "\x201d"[0]},
                    {"real", "\x211c"[0]},
                    {"reg", "\x00ae"[0]},
                    {"rfloor", "\x230b"[0]},
                    {"Rho", "\x03a1"[0]},
                    {"rho", "\x03c1"[0]},
                    {"rlm", "\x200f"[0]},
                    {"rsaquo", "\x203a"[0]},
                    {"rsquo", "\x2019"[0]},
                    {"sbquo", "\x201a"[0]},
                    {"Scaron", "\x0160"[0]},
                    {"scaron", "\x0161"[0]},
                    {"sdot", "\x22c5"[0]},
                    {"sect", "\x00a7"[0]},
                    {"shy", "\x00ad"[0]},
                    {"Sigma", "\x03a3"[0]},
                    {"sigma", "\x03c3"[0]},
                    {"sigmaf", "\x03c2"[0]},
                    {"sim", "\x223c"[0]},
                    {"spades", "\x2660"[0]},
                    {"sub", "\x2282"[0]},
                    {"sube", "\x2286"[0]},
                    {"sum", "\x2211"[0]},
                    {"sup", "\x2283"[0]},
                    {"sup1", "\x00b9"[0]},
                    {"sup2", "\x00b2"[0]},
                    {"sup3", "\x00b3"[0]},
                    {"supe", "\x2287"[0]},
                    {"szlig", "\x00df"[0]},
                    {"Tau", "\x03a4"[0]},
                    {"tau", "\x03c4"[0]},
                    {"there4", "\x2234"[0]},
                    {"Theta", "\x0398"[0]},
                    {"theta", "\x03b8"[0]},
                    {"thetasym", "\x03d1"[0]},
                    {"thinsp", "\x2009"[0]},
                    {"THORN", "\x00de"[0]},
                    {"thorn", "\x00fe"[0]},
                    {"tilde", "\x02dc"[0]},
                    {"times", "\x00d7"[0]},
                    {"trade", "\x2122"[0]},
                    {"Uacute", "\x00da"[0]},
                    {"uacute", "\x00fa"[0]},
                    {"uarr", "\x2191"[0]},
                    {"uArr", "\x21d1"[0]},
                    {"Ucirc", "\x00db"[0]},
                    {"ucirc", "\x00fb"[0]},
                    {"Ugrave", "\x00d9"[0]},
                    {"ugrave", "\x00f9"[0]},
                    {"uml", "\x00a8"[0]},
                    {"upsih", "\x03d2"[0]},
                    {"Upsilon", "\x03a5"[0]},
                    {"upsilon", "\x03c5"[0]},
                    {"Uuml", "\x00dc"[0]},
                    {"uuml", "\x00fc"[0]},
                    {"weierp", "\x2118"[0]},
                    {"Xi", "\x039e"[0]},
                    {"xi", "\x03be"[0]},
                    {"Yacute", "\x00dd"[0]},
                    {"yacute", "\x00fd"[0]},
                    {"yen", "\x00a5"[0]},
                    {"yuml", "\x00ff"[0]},
                    {"Yuml", "\x0178"[0]},
                    {"Zeta", "\x0396"[0]},
                    {"zeta", "\x03b6"[0]},
                    {"zwj", "\x200d"[0]},
                    {"zwnj", "\x200c"[0]}
                });
            }
        }

        public List<string> Problems() {
            var allProblems = new List<string>(_problems.Yield());
            for (var i = 0; i < ElementCache[_type].Count; i++) {
                var elementKey = ElementCache[_type][i];
                var list = (IList)_metadata[elementKey].Getter(this);
                foreach (var node in list) {
                    if (node is CfgNode) {
                        allProblems.AddRange(((CfgNode)node).Problems());
                    }
                }
            }
            return allProblems;
        }

        private static Dictionary<string, CfgMetadata> GetMetadata(Type type, CfgProblems problems, StringBuilder sb) {
            Dictionary<string, CfgMetadata> metadata;
            if (MetadataCache.TryGetValue(type, out metadata))
                return metadata;

            NameCache[type] = new Dictionary<string, string>();

            var keyCache = new List<string>();
            var listCache = new List<string>();
            var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            metadata = new Dictionary<string, CfgMetadata>(StringComparer.Ordinal);
            for (var i = 0; i < propertyInfos.Length; i++) {
                var propertyInfo = propertyInfos[i];

                if (propertyInfo.MemberType != MemberTypes.Property)
                    continue;
                if (!propertyInfo.CanRead)
                    continue;
                if (!propertyInfo.CanWrite)
                    continue;

                var attribute = (CfgAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(CfgAttribute));
                if (attribute == null)
                    continue;

                var key = NormalizeName(type, propertyInfo.Name, sb);
                var item = new CfgMetadata(propertyInfo, attribute);

                // report default value and property type mismatches
                if (attribute.value != null) {
                    var attributeType = attribute.value.GetType();
                    if (attributeType != propertyInfo.PropertyType) {
                        item.TypeMismatch = true;
                        problems.TypeMismatch(key, attributeType, propertyInfo.PropertyType);
                    }
                }

                if (propertyInfo.PropertyType.IsGenericType) {
                    listCache.Add(key);
                    item.ListType = propertyInfo.PropertyType.GetGenericArguments()[0];
                    if (item.ListType.BaseType == typeof(CfgNode)) {
                        item.Loader = () => (CfgNode)Activator.CreateInstance(item.ListType);
                    }
                    if (attribute.sharedProperty != null) {
                        item.SharedProperty = attribute.sharedProperty;
                        item.SharedValue = attribute.sharedValue;
                    }
                } else {
                    keyCache.Add(key);
                }
                item.Setter = ReflectionHelper.CreateSetter(propertyInfo);
                item.Getter = ReflectionHelper.CreateGetter(propertyInfo);

                metadata[key] = item;
            }
            MetadataCache[type] = metadata;
            PropertyCache[type] = keyCache;
            ElementCache[type] = listCache;

            //add metadata to cache before you start digging deeper
            foreach (var item in metadata) {
                if (item.Value.ListType != null) {
                    item.Value.UniquePropertiesInList = GetMetadata(item.Value.ListType, problems, sb).Where(p => p.Value.Attribute.unique).Select(p => p.Key).ToArray();
                }
            }
            return metadata;
        }

        // a naive implementation for hand-written configurations
        private static string Encode(string value, StringBuilder builder) {

            builder.Clear();
            for (var i = 0; i < value.Length; i++) {
                var ch = value[0];
                if (ch <= '>') {
                    switch (ch) {
                        case '<':
                            builder.Append("&lt;");
                            break;
                        case '>':
                            builder.Append("&gt;");
                            break;
                        case '"':
                            builder.Append("&quot;");
                            break;
                        case '\'':
                            builder.Append("&#39;");
                            break;
                        case '&':
                            builder.Append("&amp;");
                            break;
                        default:
                            builder.Append(ch);
                            break;
                    }
                } else {
                    builder.Append(ch);
                }
            }
            return builder.ToString();
        }

        public static string Decode(string input, StringBuilder builder) {

            builder.Clear();
            var htmlEntityEndingChars = new[] { CfgConstants.ENTITY_END, CfgConstants.ENTITY_START };

            for (var i = 0; i < input.Length; i++) {
                var c = input[i];

                if (c == CfgConstants.ENTITY_START) {
                    // Found &. Look for the next ; or &. If & occurs before ;, then this is not entity, and next & may start another entity
                    var index = input.IndexOfAny(htmlEntityEndingChars, i + 1);
                    if (index > 0 && input[index] == CfgConstants.ENTITY_END) {
                        var entity = input.Substring(i + 1, index - i - 1);

                        if (entity.Length > 1 && entity[0] == '#') {

                            bool parsedSuccessfully;
                            uint parsedValue;
                            if (entity[1] == 'x' || entity[1] == 'X') {
                                parsedSuccessfully = UInt32.TryParse(entity.Substring(2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out parsedValue);
                            } else {
                                parsedSuccessfully = UInt32.TryParse(entity.Substring(1), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out parsedValue);
                            }

                            if (parsedSuccessfully) {
                                parsedSuccessfully = (0 < parsedValue && parsedValue <= CfgConstants.UNICODE_00_END);
                            }

                            if (parsedSuccessfully) {
                                if (parsedValue <= CfgConstants.UNICODE_00_END) {
                                    // single character
                                    builder.Append((char)parsedValue);
                                } else {
                                    // multi-character
                                    var utf32 = (int)(parsedValue - CfgConstants.UNICODE_01_START);
                                    var leadingSurrogate = (char)((utf32 / 0x400) + CfgConstants.HIGH_SURROGATE);
                                    var trailingSurrogate = (char)((utf32 % 0x400) + CfgConstants.LOW_SURROGATE);

                                    builder.Append(leadingSurrogate);
                                    builder.Append(trailingSurrogate);
                                }

                                i = index;
                                continue;
                            }
                        } else {
                            i = index;
                            char entityChar;
                            Entities.TryGetValue(entity, out entityChar);

                            if (entityChar != (char)0) {
                                c = entityChar;
                            } else {
                                builder.Append(CfgConstants.ENTITY_START);
                                builder.Append(entity);
                                builder.Append(CfgConstants.ENTITY_END);
                                continue;
                            }
                        }
                    }
                }
                builder.Append(c);
            }
            return builder.ToString();
        }

    }

    // Credit to http://stackoverflow.com/users/478478/alex-hope-oconnor
    public static class ReflectionHelper {

        public static Func<object, object> CreateGetter(PropertyInfo property) {
            var getter = property.GetGetMethod();
            var genericMethod = typeof(ReflectionHelper).GetMethod("CreateGetterGeneric");
            var genericHelper = genericMethod.MakeGenericMethod(property.DeclaringType, property.PropertyType);
            return (Func<object, object>)genericHelper.Invoke(null, new object[] { getter });
        }

        public static Func<object, object> CreateGetterGeneric<T, R>(MethodInfo getter) where T : class {
            var getterTypedDelegate = (Func<T, R>)Delegate.CreateDelegate(typeof(Func<T, R>), getter);
            var getterDelegate = (Func<object, object>)((object instance) => getterTypedDelegate((T)instance));
            return getterDelegate;
        }

        public static Action<object, object> CreateSetter(PropertyInfo property) {
            var setter = property.GetSetMethod();
            var genericMethod = typeof(ReflectionHelper).GetMethod("CreateSetterGeneric");
            var genericHelper = genericMethod.MakeGenericMethod(property.DeclaringType, property.PropertyType);
            return (Action<object, object>)genericHelper.Invoke(null, new object[] { setter });
        }

        public static Action<object, object> CreateSetterGeneric<T, V>(MethodInfo setter) where T : class {
            var setterTypedDelegate = (Action<T, V>)Delegate.CreateDelegate(typeof(Action<T, V>), setter);
            var setterDelegate = (Action<object, object>)((instance, value) => setterTypedDelegate((T)instance, (V)value));
            return setterDelegate;
        }

    }
}