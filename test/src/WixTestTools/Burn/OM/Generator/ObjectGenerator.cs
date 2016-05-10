// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Reflection;
using WixTest.Burn.OM.ElementAttribute;

namespace WixTest.Burn.OM.Generator
{
    /// <summary>
    /// This class generates object from xml. It deserializes the xml into object
    /// </summary>
    public class ObjectGenerator
    {
        public object GetObjectFromXml(XmlNode xmlNode)
        {
            object obj = null;

            try
            {
                // Create the object matching xml node name
                obj = GetObject(xmlNode.Name);

                // Get the list of attributes
                XmlAttributeCollection xmlAttrCol = xmlNode.Attributes;
                foreach (XmlAttribute xmlAttr in xmlAttrCol)
                {
                    // Get the property matching attribute name
                    PropertyInfo property = GetProperty(xmlAttr.Name, obj);

                    MethodInfo getMethod = property.GetGetMethod();
                    Type returnType = getMethod.ReturnType;

                    // For enum value needs to be converted before assigning it to property
                    if (returnType.IsEnum == true)
                    {
                        FieldInfo enumFieldInfo = returnType.GetField(xmlAttr.Value);
                        object newEnumValue = Enum.ToObject(returnType, enumFieldInfo.GetValue(returnType));
                        property.SetValue(obj, newEnumValue, null);
                    }
                    else
                    {
                        // Native datatype (int, uint, string etc) value can be directly assigned to property
                        property.SetValue(obj, xmlAttr.Value, null);
                    }
                }

                // Get the list of child elements
                XmlNodeList childNodes = xmlNode.ChildNodes;
                foreach (XmlNode childNode in childNodes)
                {
                    // BUGBUG need to add support for properties that are arrays/lists of things.
                    // Get the property matching xml node name
                    PropertyInfo childElementproperty = GetProperty(childNode.Name, obj);

                    // Recessively call GetObjectFromXml to iterate through all child xml nodes and xml attributes
                    object childObj = GetObjectFromXml(childNode);

                    // Set the property matching child xml node
                    if (childElementproperty != null)
                    {
                        childElementproperty.SetValue(obj, childObj, null);
                    }
                }
            }
            catch
            {
                // TODO: should we throw?  or should we just ignore unknown things we don't know how to deserialize?
                Console.WriteLine("FAIL");
            }

            return obj;
        }

        /// <summary>
        /// Get property matching xml attribute name
        /// </summary>
        /// <param name="xmlAttributeName">xml attribute name</param>
        /// <param name="obj">object under which property is searched</param>
        /// <returns>property matching xml attribute name</returns>
        private PropertyInfo GetProperty(string xmlAttributeName, object obj)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                object[] attributes = property.GetCustomAttributes(true);

                foreach (object attr in attributes)
                {
                    BurnXmlAttribute burnAttribute = attr as BurnXmlAttribute;

                    if (burnAttribute != null && burnAttribute.Name == xmlAttributeName)
                    {
                        return property;
                    }
                    else
                    {
                        BurnXmlChildElement burnChildElement = attr as BurnXmlChildElement;
                        if (burnChildElement != null)
                        {
                            Type burnChildElementReturnType = property.GetGetMethod().ReturnType;

                            object[] burnChildElementAttrs = burnChildElementReturnType.GetCustomAttributes(true);

                            foreach (object childAttr in burnChildElementAttrs)
                            {
                                BurnXmlElement burnXmlElementAttr = childAttr as BurnXmlElement;

                                if (burnXmlElementAttr != null && burnXmlElementAttr.Name == xmlAttributeName)
                                {
                                    return property;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// It iterates through the class and nested class to create object matching xml element name
        /// </summary>
        /// <param name="xmlElementName">Name of xml element</param>
        /// <returns>object</returns>
        private object GetObject(string xmlElementName)
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            Type[] types = assem.GetTypes();
            object obj = null;

            foreach (Type type in types)
            {
                // Filter out non-public class
                if (type.IsPublic == true)
                {
                    // Look into nested class
                    Type[] nestedTypes = type.GetNestedTypes();

                    foreach (Type nestedType in nestedTypes)
                    {
                        obj = CreateObject(nestedType, xmlElementName);

                        // Break the loop if a matching type was found.
                        if (obj != null)
                            break;
                    }

                    if (obj == null)
                    {
                        obj = CreateObject(type, xmlElementName);
                        // Break the loop if a matching type was found.
                        if (obj != null)
                            break;
                    }
                    else
                    {
                        // Break the loop if nested objected was created
                        break;
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// It creates an objet matching xml element name by invoking default constructor
        /// </summary>
        /// <param name="type"></param>
        /// <param name="xmlElementName"></param>
        /// <returns></returns>
        private object CreateObject(Type type, string xmlElementName)
        {
            object[] attrs = type.GetCustomAttributes(true);

            foreach (object attr in attrs)
            {
                BurnXmlElement burnAttribute = attr as BurnXmlElement;

                if (burnAttribute != null && burnAttribute.Name == xmlElementName)
                {
                    ConstructorInfo[] ci = type.GetConstructors();

                    foreach (ConstructorInfo constructor in ci)
                    {
                        ParameterInfo[] pi = constructor.GetParameters();

                        if (pi.Length == 0)
                        {
                            object ob = constructor.Invoke(null);
                            return ob;
                        }
                    }
                }
            }
            return null;
        }
    }
}
