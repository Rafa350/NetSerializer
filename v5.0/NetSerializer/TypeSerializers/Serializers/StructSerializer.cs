﻿using System;
using System.Diagnostics;
using NetSerializer.V5.Descriptors;

namespace NetSerializer.V5.TypeSerializers.Serializers {

    public class StructSerializer: TypeSerializer {

        /// <inheritdoc/>
        /// 
        public override bool CanProcess(Type type) =>
            type.IsValueType && !type.IsPrimitive && !type.IsEnum;

        /// <inheritdoc/>
        /// 
        public override void Serialize(SerializationContext context, string name, Type type, object? obj) {

            Debug.Assert(CanProcess(type));

            var writer = context.Writer;

            if (writer.CanWriteValue(type))
                writer.WriteValue(name, obj);

            else {
                writer.WriteStructHeader(name, obj);
                SerializeStruct(context, obj);
                writer.WriteStructTail();
            }
        }

        /// <inheritdoc/>
        /// 
        public override void Deserialize(DeserializationContext context, string name, Type type, out object? obj) {

            Debug.Assert(CanProcess(type));

            var reader = context.Reader;

            if (reader.CanReadValue(type))
                obj = reader.ReadValue(name, type);

            else {
                reader.ReadStructHeader(name, type);

                obj = Activator.CreateInstance(type);
                if (obj == null)
                    throw new InvalidOperationException($"No es posible crear una instancia de '{type}'.");
                
                DeserializeStruct(context, obj);

                reader.ReadStructTail();
            }
        }

        /// <summary>
        /// Serialitza l'objecte.
        /// </summary>
        /// <param name="context">El context de serialitzacio.</param>
        /// <param name="obj">L'objecte a serialitzar.</param>
        /// 
        protected virtual void SerializeStruct(SerializationContext context, object obj) {

            var type = obj.GetType();
            var typeDescriptor = TypeDescriptorProvider.Instance.GetDescriptor(type);

            foreach (var propertyDescriptor in typeDescriptor.PropertyDescriptors)
                SerializeProperty(context, obj, propertyDescriptor);
        }

        /// <summary>
        /// Serialitza una propietat.
        /// </summary>
        /// <param name="context">El context de serialitzacio.</param>
        /// <param name="obj">L'objecte.</param>
        /// <param name="propertyDescriptor">El descriptor de la propietat.</param>
        /// 
        protected virtual void SerializeProperty(SerializationContext context, object obj, PropertyDescriptor propertyDescriptor) {

            if (propertyDescriptor.CanGetValue) {

                var typeSerializer = context.GetTypeSerializer(propertyDescriptor.Type);
                Debug.Assert(typeSerializer != null);
                
                typeSerializer.Serialize(context, propertyDescriptor.Name, propertyDescriptor.Type, propertyDescriptor.GetValue(obj));
            }
        }

        /// <summary>
        /// Deserialitza l'objecte.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="obj">L'objecte a deserialitzar.</param>
        /// 
        protected virtual void DeserializeStruct(DeserializationContext context, object obj) {

            var type = obj.GetType();
            var typeDescriptor = TypeDescriptorProvider.Instance.GetDescriptor(type);

            foreach (var propertyDescriptor in typeDescriptor.PropertyDescriptors)
                DeserializeProperty(context, obj, propertyDescriptor);
        }

        /// <summary>
        /// Deserialitza una propietat.
        /// </summary>
        /// <param name="context">El context.</param>
        /// <param name="obj">L'objecte.</param>
        /// <param name="propertyDescriptor">El descriptor de la propietat.</param>
        /// 
        protected virtual void DeserializeProperty(DeserializationContext context, object obj, PropertyDescriptor propertyDescriptor) {

            if (propertyDescriptor.CanSetValue) {
                
                var typeSerializer = context.GetTypeSerializer(propertyDescriptor.Type);
                Debug.Assert(typeSerializer != null);

                typeSerializer.Deserialize(context, propertyDescriptor.Name, propertyDescriptor.Type, out object? value);
                propertyDescriptor.SetValue(obj, value);
            }
        }
    }
}
