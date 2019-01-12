using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Xml.Serialization;

namespace DataSet2JSON
{
    public class Formatter
    {
        private static string _JsonModelFolder { get; set; }
        private static string _JsonModelFileExt { get; set; }

        public static string Help()
        {
            StringBuilder sr = new StringBuilder();
            sr.AppendLine(" Sample data to give to link parent and child");
            sr.AppendLine("Example : 1:parent!1^Reqno:2^Reqno~1^onshoresa|2^Reqno:3^Reqno~2^gdnsa~single");
            sr.AppendLine("The first Datatable should be always a config like the sample config which should be in tables[0] in the dataset passed.");
            sr.AppendLine("If there are no object in this just pass '1:parent'");
            sr.AppendLine("1:parent -> Objects should be created under this parent.");
            sr.AppendLine("!  -> To Split parent table to child");
            sr.AppendLine("~ -> Split to object and get object name and in which the object should be populated");
            sr.AppendLine("~single -> After object names says that the object is not a list");
            sr.AppendLine("2^Reqno:3^Reqno -> 2 (Table Number) Reqno column value should be equal to 3rd table Reqno and these values should be inserted into ~2^gdnsa (2nd Table gdnsa object)");
            sr.AppendLine("Note:The sequence in config is very important it should go from parent to child relationship.");
            sr.AppendLine("Ex: 1:parent!1^Reqno:2^Reqno~1^onshoresa|2^Reqno:3^Reqno~2^gdnsa");
            sr.AppendLine("But not like");
            sr.AppendLine("1:parent!2^Reqno:3^Reqno~1^onshoresa|1^Reqno:2^Reqno~2^gdnsa");
            sr.AppendLine("");
            sr.AppendLine("How to use?");
            sr.AppendLine("CustomDataFormatter.FormatDataSet(yourDataSet,\"JSON\")");
            sr.AppendLine("CustomDataFormatter.FormatDataTable(yourDataTable,\"JSON\")");
            sr.AppendLine("CustomDataFormatter.FormatDataSet(yourDataSet,\"XML\")");
            sr.AppendLine("CustomDataFormatter.FormatDataTable(yourDataTable,\"XML\")");
            return sr.ToString();
        }

        public Formatter(string JsonModelFolder,string JsonModelExtenssion = ".txt")
        {
            _JsonModelFolder = JsonModelFolder;
            _JsonModelFileExt = JsonModelExtenssion;
        }


        private static char esclamationLevel = '!', pipeLevel = '|', tildaLevel = '~', caretlevel = '^', colonlevel = ':', andLevel = '&', commaLevel = ',', starLevel = '*';

        private class createdConfigRelations
        {
            public string objName;
            public int ParentTableNo;
            public int childTableNo;
            public DataTable data;
            public string filter;
            public StringBuilder OutputResult;
        }

        private class DictionaryConfig
        {
            public string valueToBeReplaces;
            public createdConfigRelations createConfig;
        }

        public static string FormatDataSet(DataSet ds, string FormatType = "JSON")
        {

            try
            {
                List<DictionaryConfig> lstReplace = new List<DictionaryConfig>();
                //1:parent!1^Reqno:2^Reqno~1^onshoresa|1^Reqno:3^Reqno~1^gdnsa
                string formatConfig = Convert.ToString(ds.Tables[0].Rows[0][0]);

                string[] parentAndChild = formatConfig.Split(esclamationLevel);

                //1:parent
                string[] outputTables = parentAndChild[0].Split(commaLevel);
                //int parentTableNo = Convert.ToInt32(parentAndChild[0].Split(colonlevel)[0]);

                //  CustomPlugins.ErrorLog.WriteLogMessage(ds.ToXml(), "");

                if (parentAndChild.Length > 1)
                {
                    //1^Reqno:2^Reqno~1^onshoresa|1^Reqno:3^Reqno~1^gdnsa
                    string[] childConfig = parentAndChild[1].Split(pipeLevel);


                    //foreach (string item in childConfig)
                    for (int i = childConfig.Length; i > 0; i--)
                    {

                        string item = childConfig[i - 1];
                        if (!item.StartsWith(starLevel.ToString()))
                        {
                            //1^Reqno:2^Reqno~1^onshoresa
                            string[] singleConfig = item.Split(tildaLevel);

                            //1^onshoresa
                            string[] objDetails = singleConfig[1].Split(caretlevel);
                            string objName = objDetails[1];

                            //1
                            int tableNo = Convert.ToInt32(objDetails[0]);

                            //To Create empty Object
                            if (tableNo <= ds.Tables.Count)
                            {
                                if (!ds.Tables[tableNo].Columns.Contains(objName))
                                    ds.Tables[tableNo].Columns.Add(objName);
                            }


                            ////To set values to replace in empty object column
                            foreach (DataRow dr in ds.Tables[tableNo].Rows)
                            {

                                //1^Reqno:2^Reqno&
                                string[] andConditions = singleConfig[0].Split(andLevel);
                                int count = 0;
                                string filter = string.Empty;
                                int outer_tableno = 0;
                                int inner_tableno = 0;

                                foreach (string andValue in andConditions)
                                {
                                    //To add and condition
                                    if (count > 0 && !string.IsNullOrEmpty(filter))
                                        filter += " AND ";
                                    string[] columnValues = andValue.Split(colonlevel);

                                    string[] inner_tableno_columnname = columnValues[0].Split(caretlevel);
                                    Convert.ToInt32(inner_tableno_columnname[0]);
                                    string inner_columnname = string.Empty;
                                    if (inner_tableno_columnname.Length > 1)
                                        inner_columnname = inner_tableno_columnname[1];

                                    string[] outer_tableno_columnname = columnValues[1].Split(caretlevel);
                                    outer_tableno = Convert.ToInt32(outer_tableno_columnname[0]);
                                    string outer_columnname = string.Empty;
                                    if (outer_tableno_columnname.Length > 1)
                                        outer_columnname = outer_tableno_columnname[1];

                                    if (!string.IsNullOrEmpty(inner_columnname) || !string.IsNullOrEmpty(outer_columnname))
                                        filter += "[" + outer_columnname + "] = '" + Convert.ToString(dr[inner_columnname]) + "'";
                                    count++;
                                }

                                string createMoniker = "{replaceMoniker_" + DateTime.Now.ToString().Replace("/", "").Replace(":", "").Replace(" ", "") + "_RANDOM_" + GetRandomNumber(0, 100000) + "}";

                                dr[objName] = createMoniker;

                                DataView dv = new DataView(ds.Tables[outer_tableno]);
                                dv.RowFilter = filter;

                                createdConfigRelations c = new createdConfigRelations();
                                c.childTableNo = outer_tableno;
                                c.ParentTableNo = inner_tableno;
                                c.objName = objName;
                                c.data = dv.ToTable();
                                c.filter = dv.RowFilter;

                                StringBuilder outputResult = new StringBuilder();
                                StringBuilder tempResult = new StringBuilder();
                                outputResult = FormatDataTable(dv.ToTable(), FormatType);
                                tempResult.Append(outputResult);

                                if (singleConfig.Length > 2)
                                {
                                    if (singleConfig[2] == "single")
                                    {
                                        switch (FormatType)
                                        {
                                            case "XML":
                                                if (outputResult.Length > 36)
                                                {
                                                    outputResult.Clear();
                                                    outputResult.Append(tempResult.ToString(18, tempResult.Length - 36));
                                                }
                                                else if (outputResult.ToString().Trim() == "<DocumentElement />")
                                                    outputResult.Clear();
                                                break;
                                            case "JSON":
                                                if (tempResult.Length >= 2)
                                                {
                                                    outputResult.Clear();
                                                    //outputResult.Append(tempResult.ToString(1, outputResult.Length - 2));
                                                    outputResult.Append(tempResult.ToString(1, tempResult.Length - 2));
                                                }
                                                break;
                                            default:
                                                outputResult.Clear();
                                                outputResult.Append(tempResult.ToString(1, outputResult.Length - 2));
                                                break;
                                        }
                                    }

                                    if (singleConfig[2] == "singlevar")
                                    {
                                        switch (FormatType)
                                        {
                                            case "JSON":
                                                if (tempResult.Length >= 4)
                                                {
                                                    outputResult.Clear();
                                                    //outputResult.Append(tempResult.ToString(1, outputResult.Length - 2));
                                                    outputResult.Append(tempResult.ToString(2, tempResult.Length - 4));
                                                }
                                                break;
                                            default:
                                                outputResult.Clear();
                                                outputResult.Append(tempResult.ToString(1, outputResult.Length - 2));
                                                break;
                                        }
                                    }

                                }

                                c.OutputResult = outputResult;
                                DictionaryConfig d = new DictionaryConfig();
                                d.valueToBeReplaces = createMoniker;
                                d.createConfig = c;

                                lstReplace.Add(d);
                            }
                        }
                        else
                        {
                            //*2*ChildTopics*parenttopic_id*Topic_ID*
                            //*2*ParentId:TopicID~TopicMenuList

                            string[] items = item.Split(starLevel);
                            int tableNo = Convert.ToInt32(item[1].ToString());
                            string newObjName = string.Empty;
                            string parentColumnFilter = string.Empty;
                            string childColumnFilter = string.Empty;

                            string[] level1Split = items[2].Split(tildaLevel);
                            string[] level2Split = level1Split[0].Split(colonlevel);
                            if (level1Split.Length > 1)
                                newObjName = level1Split[1].ToString();
                            if (level2Split.Length > 0)
                                parentColumnFilter = level2Split[0];
                            if (level2Split.Length > 1)
                                childColumnFilter = level2Split[1];

                            if (!ds.Tables[tableNo].Columns.Contains(newObjName))
                                ds.Tables[tableNo].Columns.Add(newObjName);



                            foreach (DataRow dr in ds.Tables[tableNo].Rows)
                            {
                                if (Convert.ToInt32(dr[parentColumnFilter]) == 0)
                                {
                                    DataTable dtClone = new DataTable();
                                    dtClone = ds.Tables[tableNo].Clone();

                                    foreach (DataRow child in GetChilds(Convert.ToInt32(dr[childColumnFilter]), ds.Tables[tableNo], ref lstReplace, parentColumnFilter, childColumnFilter, newObjName).Rows)
                                    {
                                        dtClone.ImportRow(child);
                                    }
                                    if (dtClone.Rows.Count > 0)
                                    {
                                        string createMoniker = "{replaceMoniker_" + DateTime.Now.ToString().Replace("/", "").Replace(":", "").Replace(" ", "") + "_RANDOM_" + GetRandomNumber(0, 100000) + "}";
                                        dr[newObjName] = createMoniker;
                                        createdConfigRelations c = new createdConfigRelations();
                                        c.data = dtClone;
                                        c.OutputResult = FormatDataTable(dtClone, "JSON"); ;
                                        DictionaryConfig d = new DictionaryConfig();
                                        d.valueToBeReplaces = createMoniker;
                                        d.createConfig = c;

                                        lstReplace.Add(d);
                                    }
                                }
                            }
                        }
                    }
                }

                //
                StringBuilder returnOutputFormat = new StringBuilder();
                //= FormatDataTable(ds.Tables[parentTableNo],FormatType);

                ArrayList al = new ArrayList();

                foreach (string item in outputTables)
                {
                    string[] data = item.Split(colonlevel);
                    StringBuilder formattedSingleData = new StringBuilder();
                    if (!data[0].ToString().StartsWith(starLevel.ToString()))
                    {
                        int TableNumber = Convert.ToInt32(data[0]);
                        string objName = string.Empty;
                        if (data.Length > 1)
                            objName = data[1];
                        else
                            objName = "objName" + TableNumber;
                        bool single = false;
                        string singleOrsingleVar = string.Empty;
                        if (data.Length > 2)
                        {
                            single = true;
                            if (data[2].ToLower() == "single")
                                singleOrsingleVar = "single";
                            if (data[2].ToLower() == "singlevar")
                                singleOrsingleVar = "singlevar";
                        }
                        string singleData = '"' + objName + "\" : ";
                        StringBuilder tempData = new StringBuilder();
                        if (ds.Tables.Count >= TableNumber)
                            formattedSingleData = FormatDataTable(ds.Tables[TableNumber], FormatType);

                        tempData.Append(formattedSingleData);
                        if (single)
                        {
                            if (singleOrsingleVar == "single")
                            {
                                formattedSingleData.Clear();
                                if (tempData.Length >= 2)
                                    formattedSingleData.Append(tempData.ToString(1, tempData.Length - 2));
                            }
                        }
                        if (IsStringBuilderNullOrEmpty(formattedSingleData) && single)
                        {
                            formattedSingleData.Clear();
                            formattedSingleData.Append("{}");
                        }
                        else if (IsStringBuilderNullOrEmpty(formattedSingleData) && !single)
                        {
                            formattedSingleData.Clear();
                            formattedSingleData.Append("[]");
                        }
                        if (singleOrsingleVar == "singlevar")
                        {
                            formattedSingleData.Clear();
                            if (tempData.Length >= 4)
                                formattedSingleData.Append(tempData.ToString(2, tempData.Length - 4));
                        }
                        else
                        {
                            formattedSingleData.Prepend(singleData);
                        }
                        al.Add(formattedSingleData);
                    }
                    else
                    {
                        string singleData;

                        string TableNo = data[0].Replace(starLevel.ToString(), "");
                        if (data.Length > 1)
                            singleData = '"' + data[1] + "\" : ";
                        else
                            singleData = '"' + "objName" + TableNo + "\" : ";
                        DataTable dttemp = new DataTable();
                        if (Convert.ToInt32(TableNo) <= ds.Tables.Count)
                            dttemp = ds.Tables[Convert.ToInt32(TableNo)];

                        DataView dv = new DataView(dttemp);
                        if (data.Length > 2)
                            dv.RowFilter = Convert.ToString(data[2]);

                        formattedSingleData = FormatDataTable(dv.ToTable(), "JSON");
                        formattedSingleData.Prepend(singleData);
                        al.Add(formattedSingleData);
                    }

                }

                returnOutputFormat.Append("{" + string.Join(",", al.ToArray()) + "}");

                //To Ensure nothing is missed while replacing.
                for (int j = 0; j <= 10; j++)
                    for (int i = lstReplace.Count - 1; i >= 0; i--)
                    {
                        DictionaryConfig values = new DictionaryConfig();
                        if (lstReplace[i] != null)
                        {
                            values = lstReplace[i];
                        }

                        StringBuilder newValue = values.createConfig.OutputResult;

                        if (!IsStringBuilderNullOrEmpty(newValue))
                        {
                            switch (FormatType)
                            {
                                case "JSON":
                                    returnOutputFormat.Replace('"' + values.valueToBeReplaces + '"', values.createConfig.OutputResult.ToString());
                                    break;
                                case "XML":
                                    returnOutputFormat.Replace(values.valueToBeReplaces, values.createConfig.OutputResult.ToString());
                                    break;
                                default:
                                    returnOutputFormat.Replace('"' + values.valueToBeReplaces + '"', values.createConfig.OutputResult.ToString());
                                    break;
                            }
                        }
                        else
                        {
                            returnOutputFormat.Replace(values.valueToBeReplaces, string.Empty);
                        }
                    }

                // CustomPlugins.ErrorLog.WriteLogMessage(returnOutputFormat.ToString(), "");

                return returnOutputFormat.ToString();
            }
            catch (Exception ex)
            {
                ErrorLog.WriteExLog(ex);
                return ReturnCustomError(ex);
            }

        }


        private static DataTable GetChilds(int ParentID, DataTable dt, ref List<DictionaryConfig> lstReplace, string parentFilterColumn, string childFilterColumn, string newObjName)
        {
            string filter;
            filter = parentFilterColumn + " = " + ParentID;
            DataView dv = new DataView(dt);
            dv.RowFilter = filter;

            DataTable dtFiltered = dv.ToTable();


            if (dtFiltered.Rows.Count > 0)
            {
                foreach (DataRow dr in dtFiltered.Rows)
                {
                    DataTable dtClone = new DataTable();
                    dtClone = dtFiltered.Clone();
                    foreach (DataRow child in GetChilds(Convert.ToInt32(dr[childFilterColumn]), dt, ref lstReplace, parentFilterColumn, childFilterColumn, newObjName).Rows)
                    {
                        dtClone.ImportRow(child);
                    }
                    if (dtClone.Rows.Count > 0)
                    {
                        string createMoniker = "{replaceMoniker_" + DateTime.Now.ToString().Replace("/", "").Replace(":", "").Replace(" ", "") + "_RANDOM_" + GetRandomNumber(0, 100000) + "}";
                        dr[newObjName] = createMoniker;
                        createdConfigRelations c = new createdConfigRelations();
                        c.data = dtClone;
                        c.OutputResult = FormatDataTable(dtClone, "JSON"); ;
                        DictionaryConfig d = new DictionaryConfig();
                        d.valueToBeReplaces = createMoniker;
                        d.createConfig = c;

                        lstReplace.Add(d);
                    }
                }
            }

            return dtFiltered;
        }

        private static string ReturnCustomError(Exception ex, string additionMessage = "")
        {
            DataSet ds_error = new DataSet();

            DataTable dt0 = new DataTable();
            dt0.Columns.Add("Config");
            dt0.Rows.Add("1:CustomFormatterError");

            DataTable dt = new DataTable();
            dt.Columns.Add("Error Message");
            dt.Columns.Add("Error Stack Trace");
            dt.Columns.Add("Additional Message");


            ds_error.Tables.Add(dt0);
            ds_error.Tables.Add(dt);

            dt.Rows.Add(ex.Message, ex.StackTrace, additionMessage);

            return FormatDataSet(ds_error, "JSON");
        }

        public static StringBuilder FormatDataTable(DataTable table, string FormatType)
        {
            StringBuilder outputString = new StringBuilder();
            FormatType = FormatType.ToUpper();
            switch (FormatType)
            {
                case "JSON":
                    outputString.Append(Newtonsoft.Json.JsonConvert.SerializeObject(table));
                    break;
                case "XML":
                    string xmlString = string.Empty;
                    using (System.IO.TextWriter writer = new System.IO.StringWriter())
                    {
                        table.WriteXml(writer);
                        outputString.Append(writer.ToString());
                    }
                    break;
                default:
                    outputString.Append(Newtonsoft.Json.JsonConvert.SerializeObject(table));
                    break;
            }
            return outputString;
        }

        private static readonly Random getrandom = new Random();
        private static readonly object syncLock = new object();

        public static string JsonModelFolder1 => _JsonModelFolder;

        private static int GetRandomNumber(int min, int max)
        {
            lock (syncLock)
            { // synchronize
                return getrandom.Next(min, max);
            }
        }

        public static string ReturnErrorStatus(Exception ex, string errorcode, string errordescription)
        {
            DataSet ds = new DataSet();

            DataTable dt0 = new DataTable();
            dt0.Columns.Add("Config");
            dt0.Rows.Add("1:Status");

            DataTable dt = new DataTable();
            dt.Columns.Add("ErrorCode");
            dt.Columns.Add("ErrorDescription");

            ds.Tables.Add(dt0);
            ds.Tables.Add(dt);

            dt.Rows.Add(errorcode, errordescription);

            return FormatDataSet(ds, "JSON");
        }

        public static bool CompareJsonWithModel(JObject input, string type, ref IList<string> errorMessages)
        {
            try
            {
                string FileName = type;
                string Filepath = JsonModelFolder1 + FileName + _JsonModelFileExt;
                string jsonModelData = System.IO.File.ReadAllText(Filepath);
                JSchema schemaToCompare = JSchema.Parse(jsonModelData);

                return input.IsValid(schemaToCompare, out errorMessages);
            }
            catch (Exception ex)
            {
                errorMessages.Add(ex.Message);
                errorMessages.Add(ex.StackTrace);
                return false;
            }
        }

        private static bool IsStringBuilderNullOrEmpty(StringBuilder sb)
        {
            return sb == null || sb.Length == 0;
        }

    }

    public static class MyExtensions
    {
        public static StringBuilder Prepend(this StringBuilder sb, string content)
        {
            if (sb != null)
            {
                return sb.Insert(0, content);
            }
            else
            {
                return sb;
            }
        }

        public static string ToXml(this DataSet ds)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (TextWriter streamWriter = new StreamWriter(memoryStream))
                {
                    var xmlSerializer = new XmlSerializer(typeof(DataSet));
                    xmlSerializer.Serialize(streamWriter, ds);
                    return Encoding.UTF8.GetString(memoryStream.ToArray());
                }
            }
        }
    }
}
