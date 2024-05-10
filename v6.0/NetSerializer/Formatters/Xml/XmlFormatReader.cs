﻿using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using NetSerializer.V6.Formatters.Xml.Infrastructure;

namespace NetSerializer.V6.Formatters.Xml {

    public sealed class XmlFormatReader: FormatReader {

        private const string _schemaResourceName = "NetSerializer.V6.Formatters.Xml.Schemas.DataSchema.xsd";

        private readonly XmlReader _reader;
        private bool _isClosed = false;

        private int _serializerVersion;
        private int _dataVersion;
        private bool _checkNames = true;
        private bool _useNames;
        private bool _useMeta;
        private bool _compactMode;
        private bool _encodedStrings;

        public XmlFormatReader(Stream stream) {

            if (!stream.CanRead)
                throw new InvalidOperationException("Es stream especificado no es de lectura.");

            /*if (_settings.Preprocesor != null) {
                var inputStream = new MemoryStream();
                _settings.Preprocesor.Process(stream, inputStream, false);
                inputStream.Flush();
                inputStream.Seek(0, SeekOrigin.Begin);
                stream = inputStream;
            }
            */

            var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(_schemaResourceName);
            if (resourceStream == null)
                throw new Exception(String.Format("No se encontro el recurso '{0}'", _schemaResourceName));
            var schema = XmlSchema.Read(resourceStream, null);
            if (schema == null)
                throw new Exception(String.Format("No se pudo cargar el esquema '{0}'", _schemaResourceName));

            var readerSettings = new XmlReaderSettings {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                IgnoreProcessingInstructions = true,
                CheckCharacters = true,
                CloseInput = true,
                ValidationType = ValidationType.Schema,
                MaxCharactersInDocument = 1000000,
                ConformanceLevel = ConformanceLevel.Document
            };
            readerSettings.Schemas.Add(schema);

            _reader = XmlReader.Create(stream, readerSettings);

            _reader.Read();

            // Processa la seccio <document>
            //
            _reader.Read();
            if (_reader.HasAttributes) {
                var attributes = _reader.ReadAttributes();
                string value;
                if (attributes.TryGetValue("version", out value))
                    _serializerVersion = XmlConvert.ToInt32(value);
                if (attributes.TryGetValue("useNames", out value))
                    _useNames = XmlConvert.ToBoolean(value);
                if (attributes.TryGetValue("compactMode", out value))
                    _compactMode = XmlConvert.ToBoolean(value);
                if (attributes.TryGetValue("useMeta", out value))
                    _useMeta = XmlConvert.ToBoolean(value);
                if (attributes.TryGetValue("encodeStrings", out value))
                    _encodedStrings = XmlConvert.ToBoolean(value);
            }

            if (_checkNames && !_useNames)
                throw new InvalidOperationException("No es posible verificar los nombres de los elementos. La aplicacion requiere 'useNames=true'.");

            // Processa la seccio <data>
            //
            _reader.Read();
            if (_reader.HasAttributes) {
                var attributes = _reader.ReadAttributes();
                if (attributes.TryGetValue("version", out string value))
                    _dataVersion = XmlConvert.ToInt32(value);
            }

            // Es posiciona en el seguent element despres de <data>
            //
            _reader.Read();
        }

        /// <inheritdoc/>
        /// 
        public override void Close() {

            if (!_isClosed) {
                _reader.Close();
                _isClosed = true;
            }
        }

        /// <inheritdoc/>
        /// 
        public override void Dispose() {

            Close();
        }

        /// <inheritdoc/>
        /// 
        public override bool ReadBool(string name) {

            if (!CheckValueNode(name))
                throw new InvalidOperationException($"Se esperaba un nodo '<value Name=\"{name}\">.");

            return _reader.ReadElementContentAsBoolean();
        }

        /// <inheritdoc/>
        /// 
        public override int ReadInt(string name) {

            if (!CheckValueNode(name))
                throw new InvalidOperationException($"Se esperaba un nodo '<value Name=\"{name}\">.");

            return _reader.ReadElementContentAsInt();
        }

        /// <inheritdoc/>
        /// 
        public override float ReadSingle(string name) {

            if (!CheckValueNode(name))
                throw new InvalidOperationException($"Se esperaba un nodo '<value Name=\"{name}\">.");

            return _reader.ReadElementContentAsFloat();
        }

        /// <inheritdoc/>
        /// 
        public override double ReadDouble(string name) {

            if (!CheckValueNode(name))
                throw new InvalidOperationException($"Se esperaba un nodo '<value Name=\"{name}\">.");

            return _reader.ReadElementContentAsDouble();
        }

        /// <inheritdoc/>
        /// 
        public override decimal ReadDecimal(string name) {

            if (!CheckValueNode(name))
                throw new InvalidOperationException($"Se esperaba un nodo '<value Name=\"{name}\">.");

            return _reader.ReadElementContentAsDecimal();
        }

        /// <inheritdoc/>
        /// 
        public override T ReadEnum<T>(string name) {

            if (!CheckValueNode(name))
                throw new InvalidOperationException($"Se esperaba un nodo '<value Name=\"{name}\">.");

            return Enum.Parse<T>(_reader.ReadElementContentAsString());
        }

        /// <inheritdoc/>
        /// 
        public override string? ReadString(string name) {

            if (!CheckValueNode(name))
                throw new InvalidOperationException($"Se esperaba un nodo '<value Name=\"{name}\">.");

            return _reader.ReadElementContentAsString();
        }

        /// <inheritdoc/>
        /// 
        public override object? ReadObject(string name) {

            return null;
        }

        /// <summary>
        /// Comprova si un node <value> es correcte.</value>
        /// </summary>
        /// <param name="name">El nom del node</param>
        /// <returns>True si tot es correcte.</returns>
        /// 
        private bool CheckValueNode(string name) {

            if (_reader.Name != (_compactMode ? "v" : "value"))
                return false;

            if (_useNames && _checkNames)
                if (name != _reader.GetAttribute("name"))
                    return false;

            return true;
        }
    }
}