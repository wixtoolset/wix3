//-----------------------------------------------------------------------
// <copyright file="XMLGenerator.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>converts objects into xml</summary>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using WixTest.Burn.OM.ElementAttribute;
using System.Xml;

namespace WixTest.Burn.OM.Generator
{
    public class XMLGenerator
    {
        /// <summary>
        /// Serializes a Burn.OM object into xml.
        /// </summary>
        /// <param name="inputObj">any Burn.OM object</param>
        /// <returns>xml string for that object and all its children</returns>
        public static string GetXmlString(object inputObj)
        {
            string xml = "";  // BUGBUG TODO, use real XML class, not strings
            
            if (inputObj.GetType().IsArray)
            {
                // if we were passed an array of objects (any type of objects) then generate the xml for each item in the array and concatenate them.
                foreach (object oneObj in (object[])inputObj)
                {
                    xml += GetXmlString(oneObj);
                }
            } 
            else if (ObjectIsXmlElement(inputObj))
            {
                string elementName = GetXmlElementName(inputObj);
                string elementNamespacePrefix = GetXmlNamespacePrefix(inputObj);

                // write the element opening tag
                xml += "<" + elementName;
                PropertyInfo[] propInfoList = inputObj.GetType().GetProperties();

                // Add the element namespace (if any)
                if (!string.IsNullOrEmpty(elementNamespacePrefix))
                {
                    xml += string.Format(" xmlns=\"{0}\"", elementNamespacePrefix);
                }
                // loop thru the attributes of this element and write them
                foreach (PropertyInfo propInfo in propInfoList)
                {
                    if (PropertyIsXmlAttribute(propInfo))
                    {
                        string attributeName = "";
                        string attributeValue = "";
                        attributeName = GetXmlAttributeName(propInfo);
                        try
                        {
                            // This could fail if the property in the inputObj is null.  
                            // If that happens, we don't write the attribute at all (i.e. it was probably optional)
                            attributeValue = propInfo.GetValue(inputObj, null).ToString();
                            if (attributeValue == "String") // String needs to changed to lower case as "string" C# keyword and therefore cannot be used a enum for Variable type
                            {
                                attributeValue = attributeValue.ToLower();
                            }

                            if (!string.IsNullOrEmpty(attributeValue))
                            {
                                xml += " " + attributeName + "=\"" + attributeValue + "\"";
                            }
                        }
                        catch { }
                    }
                }
                
                //finish the element opening tag
                xml += ">";

                // loop thru all the child elements of this element and write them
                foreach (PropertyInfo propInfo in propInfoList)
                {
                    if (PropertyIsXmlChildElement(propInfo))
                    {
                        try
                        {
                            // this could fail if the inputObj is null.  If that happens, don't try to recurse into it.
                            object propObj = propInfo.GetValue(inputObj, null);
                            xml += XMLGenerator.GetXmlString(propObj);
                        }
                        catch { }
                    }
                    else if (PropertyIsXmlBlob(propInfo))
                    {
                        try
                        {
                            // this could fail if the inputObj is null.  If that happens, don't store anything for it in the generated xml
                            object propObj = propInfo.GetValue(inputObj, null);
                            xml += propObj.ToString();
                        }
                        catch { }
                    }
                }

                // store the element closing tag
                xml += "</" + elementName + ">";
            }
            return xml;
        }


        // BUGBUG These static methods should probably be refactored... lots of nearly duplicate code

        public static bool PropertyIsXmlAttribute(PropertyInfo inputObj)
        {
            bool isXmlAttribute = false;

            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(inputObj);
            foreach (System.Attribute attr in attrs)
            {
                if (attr is BurnXmlAttribute)
                {
                    isXmlAttribute = true;
                }
            }
            return isXmlAttribute;
        }

        public static bool PropertyIsXmlChildElement(PropertyInfo inputObj)
        {
            bool isXmlChildElement = false;

            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(inputObj);
            foreach (System.Attribute attr in attrs)
            {
                if (attr is BurnXmlChildElement)
                {
                    isXmlChildElement = true;
                }
            }
            return isXmlChildElement;
        }

        public static bool PropertyIsXmlBlob(PropertyInfo inputObj)
        {
            bool isXmlBlob = false;

            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(inputObj);
            foreach (System.Attribute attr in attrs)
            {
                if (attr is BurnXmlBlob)
                {
                    isXmlBlob = true;
                }
            }

            return isXmlBlob;
        }

        public static bool ObjectIsXmlElement(object inputObj)
        {
            bool isXmlElement = false;

            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(inputObj.GetType());
            foreach (System.Attribute attr in attrs)
            {
                if (attr is BurnXmlElement)
                {
                    isXmlElement = true;
                }
            }

            return isXmlElement;
        }

        public static string GetXmlElementName(object inputObj)
        {
            string elementName = "";

            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(inputObj.GetType());
            foreach (System.Attribute attr in attrs)
            {
                if (attr is BurnXmlElement)
                {
                    elementName = ((BurnXmlElement)attr).Name;
                }
            }

            return elementName;
        }

        /// <summary>
        /// To get the namespace prefix associated with particular element
        /// </summary>
        /// <param name="inputObj">XML element object</param>
        /// <returns>Namespace prefix</returns>
        public static string GetXmlNamespacePrefix(object inputObj)
        {
            string namespacePrefix = string.Empty;

            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(inputObj.GetType());
            foreach (System.Attribute attr in attrs)
            {
                if (attr is BurnXmlElement)
                {
                    namespacePrefix = ((BurnXmlElement)attr).NamespacePrefix;
                }
            }
            return namespacePrefix;
        }

        public static string GetXmlAttributeName(PropertyInfo inputObj)
        {
            string attributeName = "";

            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(inputObj);
            foreach (System.Attribute attr in attrs)
            {
                if (attr is BurnXmlAttribute)
                {
                    attributeName = ((BurnXmlAttribute)attr).Name;
                }
            }

            return attributeName;
        }
    }
}
