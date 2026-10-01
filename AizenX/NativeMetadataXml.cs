using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Xml;
using CodeX.Core.Engine;
using CodeX.Core.Numerics;
using CodeX.Core.Utilities;
namespace AizenX;
public static class NativeMetadataXml {
[ThreadStatic] static bool _legacyFlags;
public static bool HasSchema(string root,bool pso) => (pso ? CodeX.Games.RDR2.RPF8.Rpf8Schemas.PsoSchema : CodeX.Games.RDR2.RPF8.Rpf8Schemas.RscSchema).GetClass(ParseHash(root)) != null;
public static string Canonicalize(string xml,bool pso) => Parse(xml,pso ? CodeX.Games.RDR2.RPF8.Rpf8Schemas.PsoSchema : CodeX.Games.RDR2.RPF8.Rpf8Schemas.RscSchema).ToXml();
public static byte[] Compile(string xml,bool pso) {
 var schema=pso ? CodeX.Games.RDR2.RPF8.Rpf8Schemas.PsoSchema : CodeX.Games.RDR2.RPF8.Rpf8Schemas.RscSchema;
 var bag=Parse(xml,schema);
 if(pso) { var m=new CodeX.Games.RDR2.Files.PsoFile("<"+bag.Class.Name+"/>"){Bag=bag}; return m.Save(); }
 var r=new CodeX.Games.RDR2.RSC8.Rsc8Meta("<"+bag.Class.Name+"/>"){Bag=bag}; return r.Save();
}
public static uint ParseHash(string text) {
 text = text.Trim();
 foreach(var prefix in new[]{"UNK_MEMBER_0x", "hash_", "0x"})
  if(text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && uint.TryParse(text[prefix.Length..],NumberStyles.HexNumber,CultureInfo.InvariantCulture,out var n)) return n;
 return JenkHash.GenHash(text);
}
static JenkHash[] GetHashes(XmlElement e) => e.SelectNodes("Item|item")!.Cast<XmlNode>().Select(x => new JenkHash(ParseHash(x.InnerText))).ToArray();
public static DataBag2 Parse(string xml, DataSchema schema) {
 var d=new XmlDocument { XmlResolver=null }; d.LoadXml(xml);
 var previous=_legacyFlags;
_legacyFlags=d.SelectNodes("//*")!.Cast<XmlElement>().Any(e=>e.Name.StartsWith("UNK_MEMBER_",StringComparison.Ordinal) || e.GetAttribute("content")=="int64_array");
try { return FromXml(d.DocumentElement!,schema); } finally { _legacyFlags=previous; }
}	public static DataBag2 FromXml(XmlNode node, DataSchema schema = null, string type = "")
	{
		DataSchemaClass dataSchemaClass = null;
		if (schema != null)
		{
			dataSchemaClass = schema.GetClass(ParseHash(string.IsNullOrEmpty(type) ? node.Name : type));
			if (dataSchemaClass == null)
			{
				_ = node.HasChildNodes;
				throw new InvalidDataException("Unknown native metadata class: " + (string.IsNullOrEmpty(type) ? node.Name : type));
			}
		}
		else
		{
			throw new InvalidDataException("A native schema is required.");
		}
		if (dataSchemaClass == null)
		{
			throw new InvalidDataException("Unknown native metadata class: " + (string.IsNullOrEmpty(type) ? node.Name : type));
		}
		DataBag2 dataBag = new DataBag2(dataSchemaClass);
		if (dataSchemaClass.FieldsLookup == null)
		{
			return dataBag;
		}
		Dictionary<JenkHash, DataSchemaField> dictionary = new Dictionary<JenkHash, DataSchemaField>();
		foreach (XmlAttribute attribute in node.Attributes)
		{
			DataSchemaField field = dataSchemaClass.GetField(ParseHash(attribute.Name));
			if (field != null && field.DataType == DataBagValueType.String)
			{
				dataBag.SetString(field.Name, attribute.Value);
			}
		}
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode is XmlElement xmlElement)
			{
				uint num = ParseHash(xmlElement.Name);
				int num2 = 1;
				string name = xmlElement.Name;
				while (dictionary.ContainsKey(num))
				{
					num = ParseHash(name + num2);
					num2++;
				}
				DataSchemaField field2 = dataSchemaClass.GetField(num);
				if (field2 == null)
				{
					throw new InvalidDataException("Unknown field: " + xmlElement.Name + " in " + dataSchemaClass.Name);
				}
				dictionary[num] = field2;
ValidateScalar(xmlElement, field2);
				switch (field2.DataType)
				{
				case DataBagValueType.Float:
					dataBag.SetFloat(num, Xml.GetFloatAttribute(xmlElement));
					break;
				case DataBagValueType.Float2:
					dataBag.SetFloat2(num, Xml.GetVector2Attributes(xmlElement));
					break;
				case DataBagValueType.Float3:
					dataBag.SetFloat3(num, Xml.GetVector3Attributes(xmlElement));
					break;
				case DataBagValueType.Float4:
					dataBag.SetFloat4(num, Xml.GetVector4Attributes(xmlElement));
					break;
				case DataBagValueType.Float3x4:
					dataBag.SetFloat3x4(num, new Matrix3x4(Xml.GetRawFloatArray(xmlElement)));
					break;
				case DataBagValueType.Float4x4:
					dataBag.SetFloat4x4(num, Matrix4x4Ext.FromArray(Xml.GetRawFloatArray(xmlElement)));
					break;
				case DataBagValueType.Int8:
					dataBag.SetInt8(num, (sbyte)Xml.GetIntAttribute(xmlElement));
					break;
				case DataBagValueType.Int16:
					dataBag.SetInt16(num, (short)Xml.GetIntAttribute(xmlElement));
					break;
				case DataBagValueType.Int32:
					dataBag.SetInt32(num, Xml.GetIntAttribute(xmlElement));
					break;
				case DataBagValueType.Int64:
					dataBag.SetInt64(num, Xml.GetLongAttribute(xmlElement));
					break;
				case DataBagValueType.UInt8:
					dataBag.SetUInt8(num, (byte)Xml.GetUIntAttribute(xmlElement));
					break;
				case DataBagValueType.UInt16:
					dataBag.SetUInt16(num, (ushort)Xml.GetUIntAttribute(xmlElement));
					break;
				case DataBagValueType.UInt32:
					dataBag.SetUInt32(num, Xml.GetUIntAttribute(xmlElement));
					break;
				case DataBagValueType.UInt64:
					dataBag.SetUInt64(num, Xml.GetULongAttribute(xmlElement));
					break;
				case DataBagValueType.Enum8:
					dataBag.SetUInt8(num, (byte)ParseEnumValue(field2, xmlElement.InnerText, schema));
					break;
				case DataBagValueType.Enum16:
					dataBag.SetUInt16(num, (ushort)ParseEnumValue(field2, xmlElement.InnerText, schema));
					break;
				case DataBagValueType.Enum32:
					dataBag.SetUInt32(num, (uint)ParseEnumValue(field2, xmlElement.InnerText, schema));
					break;
				case DataBagValueType.Flags32:
					dataBag.SetUInt32(num, (uint)ParseEnumValue(field2, xmlElement.InnerText, schema));
					break;
				case DataBagValueType.Double:
					dataBag.SetDouble(num, Xml.GetDoubleAttribute(xmlElement));
					break;
				case DataBagValueType.Boolean:
					dataBag.SetBoolean(num, Xml.GetBoolAttribute(xmlElement));
					break;
				case DataBagValueType.Hash:
					dataBag.SetHash(num, ParseHash(xmlElement.InnerText));
					JenkIndex.Ensure(xmlElement.InnerText, "XML");
					break;
				case DataBagValueType.FnvHash:
					dataBag.SetFnvHash(num, FnvHash.GenHash(xmlElement.InnerText));
					break;
				case DataBagValueType.Char:
					dataBag.SetChar(num, Xml.GetCharAttribute(xmlElement));
					break;
				case DataBagValueType.String:
					dataBag.SetString(num, xmlElement.InnerText);
					break;
				case DataBagValueType.Array:
if (xmlElement.SelectNodes("Item|item")!.Count > ushort.MaxValue) throw new InvalidDataException("Native array exceeds 65535 entries.");
					if (field2.ArrayType == DataBagValueType.Object || field2.ArrayType == DataBagValueType.Struct)
					{
						string text4 = Xml.GetStringAttribute(xmlElement, "itemType");
						if (string.IsNullOrEmpty(text4) && (uint)field2.TypeName != 0)
						{
							text4 = field2.TypeName.ToString();
						}
						XmlNodeList xmlNodeList3 = xmlElement.SelectNodes("Item");
						if (xmlNodeList3.Count == 0)
						{
							xmlNodeList3 = xmlElement.SelectNodes("item");
						}
						List<DataBag2> list3 = new List<DataBag2>();
						foreach (XmlNode item2 in xmlNodeList3)
						{
							if (item2 is XmlElement node2)
							{
								string stringAttribute4 = Xml.GetStringAttribute(node2, "type");
								DataBag2 dataBag2 = FromXml(node2, schema, stringAttribute4 ?? text4);
								if (dataBag2 != null)
								{
									list3.Add(dataBag2);
								}
							}
						}
						DataBag2[] v3 = ((list3.Count > 0) ? list3.ToArray() : null);
						dataBag.SetObject(num, v3);
					}
					else if (field2.ArraySize != 0)
					{
						switch (field2.ArrayType)
						{
						case DataBagValueType.Float:
							dataBag.SetEmbeddedArray(num, Xml.GetRawFloatArray(xmlElement));
							break;
						case DataBagValueType.Float2:
							dataBag.SetEmbeddedArray(num, Xml.GetRawVector2Array(xmlElement));
							break;
						case DataBagValueType.Float3:
							dataBag.SetEmbeddedArray(num, Xml.GetRawVector3Array(xmlElement));
							break;
						case DataBagValueType.Float4:
							dataBag.SetEmbeddedArray(num, Xml.GetRawVector4Array(xmlElement));
							break;
						case DataBagValueType.UInt8:
							dataBag.SetEmbeddedArray(num, Xml.GetRawByteArray(xmlElement, 10));
							break;
						case DataBagValueType.UInt16:
							dataBag.SetEmbeddedArray(num, Xml.GetRawUshortArray(xmlElement));
							break;
						case DataBagValueType.UInt32:
							dataBag.SetEmbeddedArray(num, Xml.GetRawUintArray(xmlElement));
							break;
						case DataBagValueType.UInt64:
							dataBag.SetEmbeddedArray(num, Xml.GetRawUlongArray(xmlElement));
							break;
						case DataBagValueType.Int8:
							dataBag.SetEmbeddedArray(num, Xml.GetRawSByteArray(xmlElement));
							break;
						case DataBagValueType.Int16:
							dataBag.SetEmbeddedArray(num, Xml.GetRawShortArray(xmlElement));
							break;
						case DataBagValueType.Int32:
							dataBag.SetEmbeddedArray(num, Xml.GetRawIntArray(xmlElement));
							break;
						case DataBagValueType.Int64:
							dataBag.SetEmbeddedArray(num, Xml.GetRawLongArray(xmlElement));
							break;
						case DataBagValueType.Enum8:
							dataBag.SetEmbeddedArray(num, Xml.GetRawByteArray(xmlElement, 10));
							break;
						case DataBagValueType.Enum16:
							dataBag.SetEmbeddedArray(num, Xml.GetRawUshortArray(xmlElement));
							break;
						case DataBagValueType.Enum32:
							dataBag.SetEmbeddedArray(num, Xml.GetRawUintArray(xmlElement));
							break;
						case DataBagValueType.Flags32:
							dataBag.SetEmbeddedArray(num, Xml.GetRawUintArray(xmlElement));
							break;
						case DataBagValueType.Boolean:
							dataBag.SetEmbeddedArray(num, Xml.GetRawByteArray(xmlElement, 10));
							break;
						case DataBagValueType.Hash:
							dataBag.SetEmbeddedArray(num, GetHashes(xmlElement));
							break;
						case DataBagValueType.Char:
							dataBag.SetEmbeddedArray(num, Encoding.ASCII.GetBytes(xmlElement.InnerText));
							break;
						case DataBagValueType.Array:
						{
							if ((uint)field2.TypeName == 25)
							{
								XmlNodeList xmlNodeList5 = xmlElement.SelectNodes("Item");
								if (xmlNodeList5.Count == 0)
								{
									xmlNodeList5 = xmlElement.SelectNodes("item");
								}
								List<JenkHash[]> list5 = new List<JenkHash[]>();
								foreach (XmlNode item3 in xmlNodeList5)
								{
									List<JenkHash> list6 = new List<JenkHash>();
									foreach (XmlNode item4 in item3.SelectNodes("Item"))
									{
										if (item4 is XmlElement xmlElement3)
										{
											list6.Add(new JenkHash(xmlElement3.InnerText));
										}
									}
									list5.Add((list6.Count > 0) ? list6.ToArray() : null);
								}
								JenkHash[][] v5 = ((list5.Count > 0) ? list5.ToArray() : null);
								dataBag.SetObject(num, v5);
								break;
							}
							string text5 = Xml.GetStringAttribute(xmlElement, "itemType");
							if (string.IsNullOrEmpty(text5) && (uint)field2.TypeName != 0)
							{
								text5 = field2.TypeName.ToString();
							}
							XmlNodeList xmlNodeList6 = xmlElement.SelectNodes("Item");
							if (xmlNodeList6.Count == 0)
							{
								xmlNodeList6 = xmlElement.SelectNodes("item");
							}
							List<DataBag2[]> list7 = new List<DataBag2[]>();
							foreach (XmlNode item5 in xmlNodeList6)
							{
								List<DataBag2> list8 = new List<DataBag2>();
								foreach (XmlNode item6 in item5.SelectNodes("Item"))
								{
									if (item6 is XmlElement node3)
									{
										string stringAttribute5 = Xml.GetStringAttribute(node3, "type");
										DataBag2 dataBag3 = FromXml(node3, schema, stringAttribute5 ?? text5);
										if (dataBag3 != null)
										{
											list8.Add(dataBag3);
										}
									}
								}
								list7.Add((list8.Count > 0) ? list8.ToArray() : null);
							}
							DataBag2[][] v6 = ((list7.Count > 0) ? list7.ToArray() : null);
							dataBag.SetObject(num, v6);
							break;
						}
						case DataBagValueType.String:
						{
							XmlNodeList xmlNodeList4 = xmlElement.SelectNodes("Item");
							if (xmlNodeList4.Count == 0)
							{
								xmlNodeList4 = xmlElement.SelectNodes("item");
							}
							List<string> list4 = new List<string>();
							foreach (XmlNode item7 in xmlNodeList4)
							{
								list4.Add(item7.InnerText);
							}
							string[] v4 = ((list4.Count > 0) ? list4.ToArray() : null);
							dataBag.SetObject(num, v4);
							break;
						}
						}
					}
					else
					{
						switch (field2.ArrayType)
						{
						case DataBagValueType.Hash:
							dataBag.SetObject(num, GetHashes(xmlElement));
							break;
						case DataBagValueType.String:
							dataBag.SetObject(num, Xml.GetStringItemArray(xmlElement));
							break;
						case DataBagValueType.UInt8:
							dataBag.SetObject(num, Xml.GetRawByteArray(xmlElement, 10));
							break;
						case DataBagValueType.UInt16:
							dataBag.SetObject(num, Xml.GetRawUshortArray(xmlElement));
							break;
						case DataBagValueType.UInt32:
							dataBag.SetObject(num, Xml.GetRawUintArray(xmlElement));
							break;
						case DataBagValueType.UInt64:
							dataBag.SetObject(num, Xml.GetRawUlongArray(xmlElement));
							break;
						case DataBagValueType.Int8:
							dataBag.SetObject(num, Xml.GetRawSByteArray(xmlElement, 10));
							break;
						case DataBagValueType.Int16:
							dataBag.SetObject(num, Xml.GetRawShortArray(xmlElement));
							break;
						case DataBagValueType.Int32:
							dataBag.SetObject(num, Xml.GetRawIntArray(xmlElement));
							break;
						case DataBagValueType.Int64:
							dataBag.SetObject(num, Xml.GetRawLongArray(xmlElement));
							break;
						case DataBagValueType.Float:
							dataBag.SetObject(num, Xml.GetRawFloatArray(xmlElement));
							break;
						case DataBagValueType.Float2:
							dataBag.SetObject(num, Xml.GetRawVector2Array(xmlElement));
							break;
						case DataBagValueType.Float3:
							dataBag.SetObject(num, Xml.GetRawVector4Array(xmlElement));
							break;
						case DataBagValueType.Float4:
							dataBag.SetObject(num, Xml.GetRawVector4Array(xmlElement));
							break;
						case DataBagValueType.Boolean:
							dataBag.SetObject(num, Xml.GetRawBoolArray(xmlElement));
							break;
						case DataBagValueType.Enum32:
							dataBag.SetObject(num, Xml.GetRawUintArray(xmlElement));
							break;
						}
					}
					break;
				case DataBagValueType.Lookup:
				{
					string text3 = Xml.GetStringAttribute(xmlElement, "itemType");
					if (string.IsNullOrEmpty(text3) && (uint)field2.TypeName != 0)
					{
						text3 = field2.TypeName.ToString();
					}
					XmlNodeList xmlNodeList = xmlElement.SelectNodes("Item");
					if (xmlNodeList.Count == 0)
					{
						xmlNodeList = xmlElement.SelectNodes("item");
					}
					List<DataBag2> list = new List<DataBag2>();
					if (xmlNodeList.Count > ushort.MaxValue) throw new InvalidDataException("Native lookup exceeds 65535 entries.");
foreach (XmlNode item8 in xmlNodeList)
					{
						if (!(item8 is XmlElement xmlElement2))
						{
							continue;
						}
						string stringAttribute = Xml.GetStringAttribute(xmlElement2, "key");
						string stringAttribute2 = Xml.GetStringAttribute(xmlElement2, "type");
if (string.IsNullOrEmpty(stringAttribute2)) { if ((uint)field2.TypeName == 23) stringAttribute2 = "ULongs"; else if (xmlElement2.SelectSingleNode("Item") != null) stringAttribute2 = "Array"; }
						object obj = null;
						switch (stringAttribute2)
						{
						case "String":
							obj = xmlElement2.InnerText;
							break;
						case "Int":
						{
							int.TryParse(xmlElement2.InnerText, out var result);
							obj = result;
							break;
						}
						case "Uint":
						{
							uint.TryParse(xmlElement2.InnerText, out var result2);
							obj = result2;
							break;
						}
						case "Float":
						{
							FloatUtil.TryParse(xmlElement2.InnerText, out var f);
							obj = f;
							break;
						}
						case "Float4":
							obj = Xml.GetVector4Attributes(xmlElement2);
							break;
						case "ULongs":
							obj = Xml.GetRawUlongArray(xmlElement2);
							break;
						case "Array":
						{
							string stringAttribute3 = Xml.GetStringAttribute(xmlElement2, "itemType");
if (string.IsNullOrEmpty(stringAttribute3)) stringAttribute3 = text3;
							XmlNodeList? xmlNodeList2 = xmlElement2.SelectNodes("Item");
							List<DataBag2> list2 = new List<DataBag2>();
							foreach (XmlNode item9 in xmlNodeList2)
							{
								DataBag2 item = FromXml(item9, schema, stringAttribute3);
								list2.Add(item);
							}
							obj = list2.ToArray();
							break;
						}
						default:
							obj = FromXml(xmlElement2, schema, stringAttribute2 ?? text3);
							break;
						}
						MakeKeyValuePair(stringAttribute, obj, field2, schema, out var kvp);
						list.Add(kvp);
					}
					dataBag.SetObject(num, (list.Count > 0) ? list.ToArray() : null);
					break;
				}
				case DataBagValueType.Object:
				{
					string text2 = Xml.GetStringAttribute(xmlElement, "type");
					if (string.IsNullOrEmpty(text2) && (uint)field2.TypeName != 0)
					{
						text2 = field2.TypeName.ToString();
					}
					if (field2.TypeName.Hash == 2)
					{
						byte[] rawByteArray = Xml.GetRawByteArray(xmlElement);
						dataBag.SetObject(num, rawByteArray);
					}
					else
					{
						DataBag2 v2 = FromXml(xmlElement, schema, text2);
						dataBag.SetObject(num, v2);
					}
					break;
				}
				case DataBagValueType.Struct:
				{
					string text = Xml.GetStringAttribute(xmlElement, "type");
					if (string.IsNullOrEmpty(text) && (uint)field2.TypeName != 0)
					{
						text = field2.TypeName.ToString();
					}
					DataBag2 v = FromXml(xmlElement, schema, text);
					dataBag.SetObject(num, v);
					break;
				}
				}
			}
			else if (childNode is XmlText xmlText)
			{
				dataBag.SetString(0u, xmlText.Value);
			}
			else
			{
				_ = childNode is XmlComment;
			}
		}
		return dataBag;
	}
	private static void MakeKeyValuePair(string key, object item, DataSchemaField fld, DataSchema schema, out DataBag2 kvp)
	{
		DataSchemaClass dataSchemaClass = new DataSchemaClass();
		DataSchemaField dataSchemaField = new DataSchemaField
		{
			Name = 1620616462u,
			DataType = DataBagValueType.Hash
		};
		DataSchemaField dataSchemaField2 = new DataSchemaField
		{
			Name = 104834034u,
			DataType = DataBagValueType.Object
		};
		switch (fld.ArrayType)
		{
		case DataBagValueType.Hash:
			dataSchemaField.DataType = DataBagValueType.Hash;
			break;
		case DataBagValueType.String:
			dataSchemaField.DataType = DataBagValueType.String;
			break;
		case DataBagValueType.Int16:
			dataSchemaField.DataType = DataBagValueType.Int16;
			break;
		case DataBagValueType.UInt32:
			dataSchemaField.DataType = DataBagValueType.UInt32;
			break;
		case DataBagValueType.Enum32:
			dataSchemaField.DataType = DataBagValueType.Enum32;
			break;
		}
		uint dataSize = 16u;
		if (item is string)
		{
			dataSchemaField2.DataType = DataBagValueType.String;
		}
		else if (item is int)
		{
			dataSchemaField2.DataType = DataBagValueType.Int32;
		}
		else if (item is uint)
		{
			dataSchemaField2.DataType = DataBagValueType.UInt32;
		}
		else if (item is float)
		{
			dataSchemaField2.DataType = DataBagValueType.Float;
		}
		else if (item is Vector4)
		{
			dataSchemaField2.DataType = DataBagValueType.Float4;
			dataSize = 32u;
		}
		else if (item is ulong[])
		{
			dataSchemaField2.DataType = DataBagValueType.Array;
			dataSchemaField2.ArrayType = DataBagValueType.UInt64;
		}
		else if (item is DataBag2[])
		{
			dataSchemaField2.DataType = DataBagValueType.Array;
			dataSchemaField2.ArrayType = DataBagValueType.Struct;
		}
		else if (item is DataBag2 dataBag)
		{
			dataSize = 8 + (dataBag.Class?.DataSize ?? 8);
		}
		dataSchemaClass.Name = 1661349098u;
		dataSchemaClass.DataSize = dataSize;
		dataSchemaClass.Fields = new DataSchemaField[2] { dataSchemaField, dataSchemaField2 };
		dataSchemaClass.BuildFieldsLookup(null);
		dataSchemaField.Offset = 0;
		dataSchemaField2.Offset = 8;
		dataSchemaField2.TypeName = fld.TypeName;
		_ = (uint)fld.TypeName;
		DataBag2 dataBag2 = new DataBag2(dataSchemaClass);
		if (item is int v)
		{
			dataBag2.SetInt32(dataSchemaField2.Name, v);
		}
		else if (item is uint v2)
		{
			dataBag2.SetUInt32(dataSchemaField2.Name, v2);
		}
		else if (item is float v3)
		{
			dataBag2.SetFloat(dataSchemaField2.Name, v3);
		}
		else if (item is Vector4 v4)
		{
			dataBag2.SetFloat4(dataSchemaField2.Name, v4);
		}
		else
		{
			dataBag2.SetObject(dataSchemaField2.Name, item);
		}
		switch (fld.ArrayType)
		{
		case DataBagValueType.Hash:
			dataBag2.SetHash(dataSchemaField.Name, new JenkHash(ParseHash(key)));
			break;
		case DataBagValueType.String:
			dataBag2.SetString(dataSchemaField.Name, key);
			break;
		case DataBagValueType.Int16:
		{
			short.TryParse(key, out var result3);
			dataBag2.SetInt16(dataSchemaField.Name, result3);
			break;
		}
		case DataBagValueType.UInt32:
		{
			uint.TryParse(key, out var result2);
			dataBag2.SetUInt32(dataSchemaField.Name, result2);
			break;
		}
		case DataBagValueType.Enum32:
		{
			uint.TryParse(key, out var result);
			dataBag2.SetUInt32(dataSchemaField.Name, result);
			break;
		}
		}
		kvp = dataBag2;
	}
	private static long ParseEnumValue(DataSchemaField f,string text,DataSchema schema) {
 text=text.Trim(); if(text.Length==0) return 0;
 if(long.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var n))return n;
 var en=schema.GetEnum(f.TypeName);
 bool flags=f.DataType==DataBagValueType.Flags32 || (f.DataType==DataBagValueType.Enum16 && _legacyFlags && ReferenceEquals(schema,CodeX.Games.RDR2.RPF8.Rpf8Schemas.RscSchema));
 long result=0;
 foreach(var part in (flags?text.Split(new[]{',', ' ', '\t', '\r', '\n', '|'},StringSplitOptions.RemoveEmptyEntries):new[]{text})) {
   var h=ParseHash(part.Trim()); var value=en?.Values.FirstOrDefault(x=>(uint)x.Name==h);
   if(value==null) {
     if(part.StartsWith("0x",StringComparison.OrdinalIgnoreCase)&&long.TryParse(part[2..],NumberStyles.HexNumber,CultureInfo.InvariantCulture,out n)){result=flags?result|n:n;continue;}
     throw new InvalidDataException("Unknown enum value: "+part+" in "+f.Name);
   }
   result=flags?result | (1L << (int)value.Value):value.Value;
 }
 return result;
}static void ValidateScalar(XmlElement e,DataSchemaField f) {
 var v=e.GetAttribute("value"); if(v.Length==0)return;
 bool valid=true;
 switch(f.DataType) {
 case DataBagValueType.Int8: valid=sbyte.TryParse(v,NumberStyles.Integer,CultureInfo.InvariantCulture,out _);break;
 case DataBagValueType.Int16: valid=short.TryParse(v,NumberStyles.Integer,CultureInfo.InvariantCulture,out _);break;
 case DataBagValueType.Int32: valid=int.TryParse(v,NumberStyles.Integer,CultureInfo.InvariantCulture,out _);break;
 case DataBagValueType.Int64: valid=long.TryParse(v,NumberStyles.Integer,CultureInfo.InvariantCulture,out _);break;
 case DataBagValueType.UInt8: valid=byte.TryParse(v,NumberStyles.Integer,CultureInfo.InvariantCulture,out _);break;
 case DataBagValueType.UInt16: valid=ushort.TryParse(v,NumberStyles.Integer,CultureInfo.InvariantCulture,out _);break;
 case DataBagValueType.UInt32: valid=uint.TryParse(v,NumberStyles.Integer,CultureInfo.InvariantCulture,out _);break;
 case DataBagValueType.UInt64: valid=v.StartsWith("0x",StringComparison.OrdinalIgnoreCase) ? ulong.TryParse(v[2..],NumberStyles.HexNumber,CultureInfo.InvariantCulture,out _) : ulong.TryParse(v,NumberStyles.Integer,CultureInfo.InvariantCulture,out _);break;
 case DataBagValueType.Boolean: valid=bool.TryParse(v,out _) || v is "0" or "1";break;
 case DataBagValueType.Float: valid=float.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out var fv)&&float.IsFinite(fv);break;
 case DataBagValueType.Double: valid=double.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out var dv)&&double.IsFinite(dv);break;
 }
 if(!valid)throw new InvalidDataException("Invalid or out-of-range value for "+e.Name+": "+v);
}
}