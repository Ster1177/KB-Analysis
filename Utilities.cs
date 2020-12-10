using Cmf.Foundation.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

public static class Utilities
{
    /// <summary>
    /// Convert a DataSet to a NgpDataSet
    /// </summary>
    /// <param name="ds">The DataSet</param>
    /// <returns>Returns the DataSet converted in a NgpDataSet</returns>
    public static NgpDataSet FromDataSet(DataSet ds)
    {
        List<string> columnsToIgnore = new List<string>();

        NgpDataSet dsd = new NgpDataSet();
        dsd.Tables = new ObservableCollection<NgpDataTableInfo>();

        foreach (DataTable t in ds.Tables)
        {
            NgpDataTableInfo tableInfo = new NgpDataTableInfo
            {
                TableName = t.TableName
            };

            dsd.Tables.Add(tableInfo);
            tableInfo.Columns = new ObservableCollection<NgpDataColumnInfo>();
            foreach (DataColumn c in t.Columns)
            {
                if (columnsToIgnore == null || (columnsToIgnore != null && !columnsToIgnore.Contains(c.ColumnName)))
                {
                    NgpDataColumnInfo col = new NgpDataColumnInfo
                    {
                        ColumnName = c.ColumnName,
                        ColumnTitle = c.ColumnName,
                        DataTypeName = c.DataType.FullName,
                        MaxLength = c.MaxLength,
                        IsKey = c.Unique,
                        IsReadOnly = (c.Unique || c.ReadOnly),
                        IsRequired = !c.AllowDBNull
                    };

                    if (c.DataType == typeof(Guid))
                    {
                        col.IsReadOnly = true;
                        col.DisplayIndex = -1;
                    }
                    tableInfo.Columns.Add(col);
                }
            }
        }

        dsd.DataXML = ds.GetXml();
        dsd.XMLSchema = ds.GetXmlSchema();

        return dsd;
    }


    /// <summary>
    /// Convert a NgpDataSet to a DataSet
    /// </summary>
    /// <param name="dsd">NgpDataSet to convert</param>
    /// /// <returns>Returns a DataSet with all information of the NgpDataSet</returns>
    public static DataSet ToDataSet(NgpDataSet dsd)
    {
        DataSet ds = new DataSet();

        if (dsd == null || (string.IsNullOrWhiteSpace(dsd.XMLSchema) && string.IsNullOrWhiteSpace(dsd.DataXML)))
        {
            dsd = FromDataSet(ds);
        }

        //Insert schema
        TextReader a = new StringReader(dsd.XMLSchema);
        XmlReader readerS = new XmlTextReader(a);
        ds.ReadXmlSchema(readerS);
        XDocument xdS = XDocument.Parse(dsd.XMLSchema);

        //Insert data
        UTF8Encoding encoding = new UTF8Encoding();
        Byte[] byteArray = encoding.GetBytes(dsd.DataXML);
        MemoryStream stream = new MemoryStream(byteArray);

        XmlReader reader = new XmlTextReader(stream);
        ds.ReadXml(reader);
        XDocument xd = XDocument.Parse(dsd.DataXML);

        foreach (DataTable dt in ds.Tables)
        {
            var rs = from row in xd.Descendants(dt.TableName)
                     select row;

            int i = 0;
            foreach (var r in rs)
            {
                DataRowState state = DataRowState.Added;
                if (r.Attribute("RowState") != null)
                {
                    state = (DataRowState)Enum.Parse(typeof(DataRowState), r.Attribute("RowState").Value);
                }

                DataRow dr = dt.Rows[i];
                dr.AcceptChanges();

                if (state == DataRowState.Deleted)
                {
                    dr.Delete();
                }
                else if (state == DataRowState.Added)
                {
                    dr.SetAdded();
                }
                else if (state == DataRowState.Modified)
                {
                    dr.SetModified();
                }

                i++;
            }
        }

        return ds;
    }

    public static void DebugTable(DataTable table)
    {
        Debug.WriteLine("--- DebugTable(" + table.TableName + ") ---");
        int zeilen = table.Rows.Count;
        int spalten = table.Columns.Count;

        // Header
        for (int i = 0; i < table.Columns.Count; i++)
        {
            string s = table.Columns[i].ToString();
            Debug.Write(String.Format("{0,-20} | ", s));
        }
        Debug.Write(Environment.NewLine);
        for (int i = 0; i < table.Columns.Count; i++)
        {
            Debug.Write("---------------------|-");
        }
        Debug.Write(Environment.NewLine);

        // Data
        for (int i = 0; i < zeilen; i++)
        {
            DataRow row = table.Rows[i];
            for (int j = 0; j < spalten; j++)
            {
                string s = row[j].ToString();
                if (s.Length > 20) s = s.Substring(0, 17) + "...";
                Debug.Write(String.Format("{0,-20} | ", s));
            }
            Debug.Write(Environment.NewLine);
        }
        for (int i = 0; i < table.Columns.Count; i++)
        {
            Debug.Write("---------------------|-");
        }
        Debug.Write(Environment.NewLine);
    }

    public static List<object> GetColumnValues(DataTable dt, string columnName)
    {
        List<object> colValues = new List<object>();
        colValues = (from DataRow row in dt.Rows select row[columnName]).ToList();
        return colValues;
    }

    public static string StripHTML(string input)
    {
        return Regex.Replace(input, "<.*?>", String.Empty);
    }

    public static void ToCSV(DataTable dtDataTable, string strFilePath)
    {
        StreamWriter sw = new StreamWriter(strFilePath, false);
        //headers    
        for (int i = 0; i < dtDataTable.Columns.Count; i++)
        {
            sw.Write(dtDataTable.Columns[i]);
            if (i < dtDataTable.Columns.Count - 1)
            {
                sw.Write(",");
            }
        }
        sw.Write(sw.NewLine);
        foreach (DataRow dr in dtDataTable.Rows)
        {
            for (int i = 0; i < dtDataTable.Columns.Count; i++)
            {
                if (!Convert.IsDBNull(dr[i]))
                {
                    string value = dr[i].ToString();
                    value = StripHTML(value);
                    value = value.Replace("\n", "").Replace("\r", "").Replace(",", " ");
                    sw.Write(value);
                }
                if (i < dtDataTable.Columns.Count - 1)
                {
                    sw.Write(",");
                }
            }
            sw.Write(sw.NewLine);
        }
        sw.Close();
    }
}
