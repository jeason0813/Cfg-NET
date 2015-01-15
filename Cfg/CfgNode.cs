﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Transformalize.Libs.Cfg.Net {

    public abstract class CfgNode {

        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertiesCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        private static Dictionary<string, char> _entities;

        private bool _configuredWithProperties;
        private readonly List<string> _propertyKeys = new List<string>();
        private readonly List<string> _classKeys = new List<string>();
        private readonly Dictionary<string, CfgProperty> _properties = new Dictionary<string, CfgProperty>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, CfgProperty>> _classProperties = new Dictionary<string, Dictionary<string, CfgProperty>>(StringComparer.Ordinal);
        private readonly List<string> _requiredProperties = new List<string>();
        private readonly List<string> _uniqueProperties = new List<string>();
        private readonly Dictionary<string, CfgNode[]> _collections = new Dictionary<string, CfgNode[]>(StringComparer.Ordinal);
        private readonly List<string> _requiredClasses = new List<string>();
        private readonly List<string> _problems = new List<string>();
        private readonly Dictionary<string, Func<CfgNode>> _elementLoaders = new Dictionary<string, Func<CfgNode>>(StringComparer.Ordinal);
        private readonly StringBuilder _builder = new StringBuilder();

        private static Dictionary<Type, Func<string, object>> Converter {
            get {
                return new Dictionary<Type, Func<string, object>> {
                    {typeof(String), (x => x)},
                    {typeof(Guid), (x => Guid.Parse(x))},
                    {typeof(Int16), (x => Convert.ToInt16(x))},
                    {typeof(Int32), (x => Convert.ToInt32(x))},
                    {typeof(Int64), (x => Convert.ToInt64(x))},
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

        // Get an element by index
        public CfgNode this[string element, int i] {
            get { return _collections[element][i]; }
        }

        // Get an attribute by name
        public CfgProperty this[string name] {
            get { return _properties[name]; }
        }

        public void Load(string xml, Dictionary<string, string> parameters = null) {

            NanoXmlNode node = null;
            try {
                node = new NanoXmlDocument(xml).RootNode;
                var environmentDefaults = LoadEnvironment(node).ToArray();
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
                _problems.Add(string.Format(CfgConstants.PROBLEM_XML_PARSE, ex.Message));
                return;
            }

            LoadProperties(node, null, parameters);
            LoadCollections(node, null, parameters);
            PopulateProperties();
        }

        protected IEnumerable<string[]> LoadEnvironment(NanoXmlNode node) {

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
                        if (!environmentNode.TryAttribute(CfgConstants.ATTRIBUTE_NAME, out environmentName))
                            continue;

                        if (defaultEnvironment.Value != environmentName.Value || !environmentNode.HasSubNode())
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

        /// <summary>
        /// Add a dictionary style "collection" to this configuration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="required"></param>
        protected void Collection<T>(string name, bool required = false) {
            _elementLoaders[name] = () => (CfgNode)Activator.CreateInstance(typeof(T));
            if (required) {
                _requiredClasses.Add(name);
            }
        }

        protected void Collection<T>(Type type, string element, bool required = false, string sharedProperty = null, T sharedValue = default(T)) {
            this.Collection(type, element, required);
            if (!string.IsNullOrEmpty(sharedProperty)) {
                SharedProperty(element, sharedProperty, sharedValue);
            }
        }

        protected void Collection(Type type, string name, bool required = false) {
            _elementLoaders[name] = () => (CfgNode)Activator.CreateInstance(type);
            if (required) {
                _requiredClasses.Add(name);
            }
        }

        protected void Collection<T1, T2>(string element, bool required = false, string sharedProperty = null, T2 sharedValue = default(T2)) {
            this.Collection<T1>(element, required);
            if (!string.IsNullOrEmpty(sharedProperty)) {
                SharedProperty(element, sharedProperty, sharedValue);
            }
        }

        /// <summary>
        /// Add a dictionary style "property" to this configuration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="required"></param>
        /// <param name="unique"></param>
        /// <param name="decode"></param>
        protected void Property<T>(string name, T value, bool required = false, bool unique = false, bool decode = false) {
            Property(name, value, typeof(T), required, unique, decode);
        }

        private void Property(string name, object value, Type type, bool required, bool unique, bool decode) {
            if (!_properties.ContainsKey(name)) {
                _propertyKeys.Add(name);
            }
            _properties[name] = new CfgProperty(name, value, type, required, unique, decode);
            if (required) {
                _requiredProperties.Add(name);
            }
            if (unique) {
                _uniqueProperties.Add(name);
            }
        }

        protected void SharedProperty<T>(string className, string propertyName, T value) {
            if (_classProperties.ContainsKey(className)) {
                _classProperties[className][propertyName] = new CfgProperty(propertyName, value, typeof(T), false, false, false);
            } else {
                _classProperties[className] = new Dictionary<string, CfgProperty>(StringComparer.Ordinal) { { propertyName, new CfgProperty(propertyName, value, typeof(T), false, false, false) } };
            }
        }

        protected CfgNode Load(NanoXmlNode node, string parentName, Dictionary<string, string> parameters) {
            LoadProperties(node, parentName, parameters);
            LoadCollections(node, parentName, parameters);
            return this;
        }

        private void LoadCollections(NanoXmlNode node, string parentName, Dictionary<string, string> parameters = null) {

            ConfigureCollectionsWithPropertyAttributes();

            for (var i = 0; i < node.SubNodes.Count; i++) {
                var subNode = node.SubNodes[i];

                if (_elementLoaders.ContainsKey(subNode.Name)) {

                    if (!_collections.ContainsKey(subNode.Name)) {
                        _classKeys.Add(subNode.Name);
                    }
                    _collections[subNode.Name] = new CfgNode[subNode.SubNodes.Count];

                    for (var j = 0; j < subNode.SubNodes.Count; j++) {
                        var add = subNode.SubNodes[j];
                        if (add.Name.Equals("add", StringComparison.Ordinal)) {
                            var tflNode = _elementLoaders[subNode.Name]().Load(add, subNode.Name, parameters);

                            // handle class properties
                            if (_classProperties.ContainsKey(subNode.Name)) {
                                foreach (var attribute in subNode.Attributes) {
                                    if (!_classProperties[subNode.Name].ContainsKey(attribute.Name))
                                        continue;
                                    var property = _classProperties[subNode.Name][attribute.Name];
                                    property.Value = attribute.Value ?? property.Value;
                                    tflNode.Property(property.Name, property.Value, property.Type, property.Required, property.Unique, property.Decode);
                                }
                            }

                            _collections[subNode.Name][j] = tflNode;
                        } else {
                            _problems.Add(string.Format(CfgConstants.PROBLEM_UNEXPECTED_ELEMENT, add.Name, subNode.Name));
                        }
                    }
                } else {
                    if (parentName == null) {
                        _problems.Add(string.Format(CfgConstants.PROBLEM_INVALID_ELEMENT, node.Name, subNode.Name, node.Name[0].IsVowel() ? "n" : string.Empty));
                    } else {
                        _problems.Add(string.Format(CfgConstants.PROBLEM_INVALID_NESTED_ELEMENT, parentName, node.Name, subNode.Name, parentName[0].IsVowel() ? "n" : string.Empty));
                    }
                }

                // check for duplicates of unique properties required to be unique in collections
                if (_collections[subNode.Name].Length > 0) {
                    var uniques = _collections[subNode.Name][0].UniqueProperties;
                    for (var k = 0; k < uniques.Count; k++) {
                        var unique = uniques[k];
                        var duplicates = _collections[subNode.Name]
                            .Where(n => n[unique].Set)
                            .GroupBy(n => n[unique].Value)
                            .Where(group => group.Count() > 1)
                            .Select(group => group.Key).ToArray();

                        for (var l = 0; l < duplicates.Length; l++) {
                            _problems.Add(string.Format(CfgConstants.PROBLEM_DUPLICATE_SET, unique, duplicates[l], subNode.Name));
                        }
                    }
                }

            }

            CheckRequiredClasses(node, parentName);
        }

        private void ConfigureCollectionsWithPropertyAttributes() {

            if (_elementLoaders.Count != 0)
                return;

            var propertyInfos = GetProperties(GetType(), _builder);
            foreach (var pair in propertyInfos) {
                if (pair.Value.MemberType != MemberTypes.Property)
                    continue;
                var attribute = (CfgAttribute)Attribute.GetCustomAttribute(pair.Value, typeof(CfgAttribute));
                if (attribute == null)
                    continue;
                if (!pair.Value.PropertyType.IsGenericType)
                    continue;
                _configuredWithProperties = true;
                var listType = pair.Value.PropertyType.GetGenericArguments()[0];
                if (attribute.sharedProperty == null) {
                    Collection(listType, ToXmlNameStyle(pair.Value.Name, _builder), attribute.required);
                } else {
                    Collection(listType, ToXmlNameStyle(pair.Value.Name, _builder), attribute.required, attribute.sharedProperty, attribute.sharedValue);
                }
            }

        }

        private void CheckRequiredClasses(NanoXmlNode node, string parentName) {
            for (var i = 0; i < _requiredClasses.Count; i++) {
                if (!_collections.ContainsKey(_requiredClasses[i])) {
                    if (parentName == null) {
                        _problems.Add(string.Format(CfgConstants.PROBLEM_MISSING_ELEMENT, node.Name, _requiredClasses[i], _requiredClasses[i][0].IsVowel() ? CfgConstants.ATTRIBUTE_NAME : string.Empty));
                    } else {
                        _problems.Add(string.Format(CfgConstants.PROBLEM_MISSING_NESTED_ELEMENT, parentName, node.Name, _requiredClasses[i], parentName[0].IsVowel() ? CfgConstants.ATTRIBUTE_NAME : string.Empty, _requiredClasses[i][0].IsVowel() ? CfgConstants.ATTRIBUTE_NAME : string.Empty));
                    }
                } else if (_collections[_requiredClasses[i]].Length == 0) {
                    _problems.Add(string.Format(CfgConstants.PROBLEM_MISSING_ADD_ELEMENT, _requiredClasses[i], _requiredClasses[i][0].IsVowel() ? CfgConstants.ATTRIBUTE_NAME : string.Empty));
                }
            }
        }

        private void LoadProperties(NanoXmlNode node, string parentName, IDictionary<string, string> parameters = null) {

            ConfigurePropertiesWithPropertyAttributes();

            for (var i = 0; i < node.Attributes.Count; i++) {
                var attribute = node.Attributes[i];
                if (_properties.ContainsKey(attribute.Name)) {
                    if (attribute.Value == null)
                        continue;

                    string value;

                    if (attribute.Value.IndexOf('@') >= 0 && parameters != null) {
                        _builder.Clear();
                        for (var j = 0; j < attribute.Value.Length; j++) {
                            if (attribute.Value[j] == CfgConstants.PLACE_HOLDER_FIRST &&
                                attribute.Value.Length > j + 1 &&
                                attribute.Value[j + 1] == CfgConstants.PLACE_HOLDER_SECOND) {
                                var length = 2;
                                while (attribute.Value.Length > j + length && attribute.Value[j + length] != CfgConstants.PLACE_HOLDER_LAST) {
                                    length++;
                                }
                                if (length > 2) {
                                    var key = attribute.Value.Substring(j + 2, length - 2);
                                    if (parameters.ContainsKey(key)) {
                                        _builder.Append(parameters[key]);
                                    } else {
                                        _problems.Add(string.Format(CfgConstants.PROBLEM_MISSING_PLACE_HOLDER_VALUE, key));
                                    }
                                }
                                j = j + length;
                            } else {
                                _builder.Append(attribute.Value[j]);
                            }
                        }
                        value = _builder.ToString();
                    } else {
                        value = attribute.Value;
                    }

                    if (_properties[attribute.Name].Type == typeof(string)) {
                        _properties[attribute.Name].Value = _properties[attribute.Name].Decode && value.IndexOf(CfgConstants.ENTITY_START) >= 0 ? Decode(value, _builder) : value;
                        _properties[attribute.Name].Set = true;
                    } else {
                        try {
                            _properties[attribute.Name].Value = Converter[_properties[attribute.Name].Type](_properties[attribute.Name].Decode && value.IndexOf(CfgConstants.ENTITY_START) >= 0 ? Decode(value, _builder) : value);
                            _properties[attribute.Name].Set = true;
                        } catch (Exception ex) {
                            _problems.Add(string.Format(CfgConstants.PROBLEM_SETTING_VALUE, _properties[attribute.Name].Name, value, parentName, node.Name, ex.Message));
                        }
                    }
                } else {
                    _problems.Add(
                        string.Format(
                            CfgConstants.PROBLEM_INVALID_ATTRIBUTE,
                            parentName,
                            node.Name,
                            attribute.Name, parentName[0].IsVowel() ? CfgConstants.ATTRIBUTE_NAME : string.Empty,
                            string.Join(", ", _properties.Select(kv => kv.Key))
                        )
                    );
                }
            }

            CheckRequiredProperties(node, parentName);
        }

        private void ConfigurePropertiesWithPropertyAttributes() {
            if (_properties.Count != 0)
                return;

            var propertyInfos = GetProperties(this.GetType(), _builder);
            foreach (var pair in propertyInfos) {
                if (pair.Value.MemberType != MemberTypes.Property)
                    continue;
                var attribute = (CfgAttribute)Attribute.GetCustomAttribute(pair.Value, typeof(CfgAttribute));
                if (attribute == null)
                    continue;
                if (pair.Value.PropertyType.IsGenericType)
                    continue;

                _configuredWithProperties = true;
                Property(ToXmlNameStyle(pair.Value.Name, _builder), attribute.value, pair.Value.PropertyType, attribute.required, attribute.unique, attribute.decode);
            }
        }

        private void CheckRequiredProperties(NanoXmlNode node, string parentName) {
            for (var i = 0; i < _requiredProperties.Count; i++) {
                if (!_properties[_requiredProperties[i]].Set) {
                    _problems.Add(string.Format(CfgConstants.PROBLEM_MISSING_ATTRIBUTE, parentName, node.Name, _requiredProperties[i], parentName[0].IsVowel() ? CfgConstants.ATTRIBUTE_NAME : string.Empty));
                }
            }
        }

        protected Dictionary<string, CfgProperty> Properties {
            get { return _properties; }
        }

        protected List<string> UniqueProperties {
            get { return _uniqueProperties; }
        }

        protected Dictionary<string, CfgNode[]> Collections {
            get { return _collections; }
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
            var allProblems = new List<string>();
            for (var i = 0; i < _problems.Count; i++) {
                allProblems.Add(_problems[i]);
            }
            foreach (var pair in _collections) {
                for (var i = 0; i < pair.Value.Length; i++) {
                    var @class = pair.Value[i];
                    allProblems.AddRange(@class.Problems());
                }
            }
            return allProblems;
        }

        protected void PopulateProperties() {

            if (!_configuredWithProperties)
                return;

            var properties = GetProperties(this.GetType(), _builder);

            for (var i = 0; i < _propertyKeys.Count; i++) {
                var key = _propertyKeys[i];
                if (properties.ContainsKey(key)) {
                    try {
                        properties[key].SetValue(this, _properties[key].Value, null);
                    } catch (Exception ex) {
                        _problems.Add(string.Format(CfgConstants.PROBLEM_SETTING_PROPERTY, properties[key].Name, _properties[key].Value, _properties[key].Name, ex.Message));
                    }
                }
            }

            for (var i = 0; i < _classKeys.Count; i++) {
                var key = _classKeys[i];
                if (!properties.ContainsKey(key))
                    continue;
                var list = (IList)Activator.CreateInstance(properties[key].PropertyType);
                for (var j = 0; j < _collections[key].Length; j++) {
                    _collections[key][j].PopulateProperties();
                    list.Add(_collections[key][j]);
                }
                properties[key].SetValue(this, list, null);
            }
        }

        private static string ToXmlNameStyle(string input, StringBuilder sb) {
            sb.Clear();
            for (var i = 0; i < input.Length; i++) {
                var c = input[i];
                if (char.IsUpper(c)) {
                    if (i > 0) {
                        sb.Append('-');
                    }
                    sb.Append(char.ToLower(c));
                } else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static Dictionary<string, PropertyInfo> GetProperties(Type type, StringBuilder sb) {
            Dictionary<string, PropertyInfo> properties;
            if (PropertiesCache.TryGetValue(type, out properties))
                return properties;

            properties = new Dictionary<string, PropertyInfo>();
            var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (var i = 0; i < propertyInfos.Length; i++) {
                var propertyInfo = propertyInfos[i];
                properties[ToXmlNameStyle(propertyInfo.Name, sb)] = propertyInfo;
            }
            PropertiesCache[type] = properties;
            return properties;
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

        public int Count(string n) {
            if (_collections.ContainsKey(n)) {
                return _collections[n].Length;
            }
            return _properties.ContainsKey(n) ? 1 : 0;
        }
    }

}