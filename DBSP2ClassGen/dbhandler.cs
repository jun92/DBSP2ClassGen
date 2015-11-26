﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace DBSP2ClassGen
{
    class dbhandler
    {

        private StringBuilder MainTemplate;
        private StringBuilder SPClassesTemplate;
        private StringBuilder SqlParamInit_Output;
        private StringBuilder SqlParamInit_Input;
        private StringBuilder SqlParamAdd;
        private StringBuilder DeclareOutputParam;
        private StringBuilder SaveOutputParam;

        private StringBuilder IP;
        private StringBuilder Port;
        private StringBuilder Username;
        private StringBuilder Password;
        private StringBuilder dbname;

        private Dictionary<String, List<SQLParam>> aspi;

        public dbhandler()
        {
            IP = new StringBuilder();
            Port = new StringBuilder();
            Username = new StringBuilder();
            Password = new StringBuilder();
            dbname = new StringBuilder();
        }

        public void LoadAllTemplate()
        {
            MainTemplate = new StringBuilder();
            MainTemplate.Append(LoadTemplate("../../Source_Template/DBSP2Class_MainTemplate.cs"));
            SPClassesTemplate = new StringBuilder();
            SPClassesTemplate.Append(LoadTemplate("../../Source_Template/DBSP2Class_SPClassesTemplate.cs"));
            SqlParamInit_Output = new StringBuilder();
            SqlParamInit_Output.Append(LoadTemplate("../../Source_Template/DBSP2Class_SqlParamInit_Output.cs"));
            SqlParamInit_Input = new StringBuilder();
            SqlParamInit_Input.Append(LoadTemplate("../../Source_Template/DBSP2Class_SqlParamInit_Input.cs"));
            SqlParamAdd = new StringBuilder();
            SqlParamAdd.Append(LoadTemplate("../../Source_Template/DBSP2Class_SqlParamAdd.cs"));
            DeclareOutputParam = new StringBuilder();
            DeclareOutputParam.Append(LoadTemplate("../../Source_Template/DBSP2Class_DeclareOutputParam.cs"));
            SaveOutputParam = new StringBuilder();
            SaveOutputParam.Append(LoadTemplate("../../Source_Template/DBSP2Class_SaveOutputParam.cs"));
        }

        public string LoadTemplate(string filename)
        {
            StringBuilder temp = new StringBuilder();
            StreamReader sr = new StreamReader(filename);
            while (sr.Peek() >= 0)
            {
                temp.Append(sr.ReadLine());
                temp.Append(System.Environment.NewLine);
            }
            sr.Close();
            return temp.ToString();
        }

        public bool SaveAsFile(string body, string filename )
        {
            StreamWriter sw = new StreamWriter(filename);
            sw.Write(body);
            sw.Close();
            return true;
        }
        struct SQLParam
        {
            public int ParamID;
            public String ParamName;
            public String ParamType;
            public short ParamSize;
            public bool IsOutput;
        }
        public List<string> GetDatabaseList(string server, string port, string id, string pwd)
        {

            List<string> databases_name = new List<string>();            
            string connString = "server=" + server + ","+ port + ";uid=" + id + ";pwd=" + pwd;
            SqlConnection con = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT name FROM sys.databases";
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            if( sdr.HasRows )
            {
                while( sdr.Read()) databases_name.Add(sdr["name"].ToString());                
            }
            sdr.Close();
            con.Close();
            return databases_name; 
        }

        public bool Build()
        {
            StringBuilder SPClasses = new StringBuilder();
            StringBuilder Body = new StringBuilder();

            Body = MainTemplate;

            SPClasses.Append(CreateClassSource()); 

            Body.Replace("%%DBSP2CLASSES%%", SPClasses.ToString());
            Body.Replace("%%DATABASE_NAME%%", dbname.ToString());
            Body.Replace("%%SERVER_IP%%", IP.ToString());
            Body.Replace("%%SERVER_PORT%%", Port.ToString());
            Body.Replace("%%USERNAME%%", Username.ToString());
            Body.Replace("%%PASSWORD%%", Password.ToString());

            SaveAsFile(Body.ToString(), @"D:\test.cs");

            return true;
        }
        private string CreateClassSource()
        {
            StringBuilder result = new StringBuilder();

            foreach ( KeyValuePair<string, List<SQLParam>> pair in aspi)
            {
                StringBuilder classstring = new StringBuilder();
                classstring.Append(SPClassesTemplate.ToString() ); 

                StringBuilder declare = new StringBuilder();
                declare.Append(MakeDeclareOutputParam(pair.Value));

                StringBuilder sqlparam_init = new StringBuilder();
                sqlparam_init.Append(MakeSqlParamInit(pair.Value));

                StringBuilder sqlParamAdd = new StringBuilder();
                sqlParamAdd.Append(MakeSqlParamAdd(pair.Value));


                classstring.Replace("%%DECLARE_OUTPUT_PARAM%%", declare.ToString());
                classstring.Replace("%%SP_NAME%%", pair.Key.ToString());
                classstring.Replace("%%SQLPARAM_INIT%%", sqlparam_init.ToString());
                classstring.Replace("%%SQLPARAM_ADD%%", sqlParamAdd.ToString());


                result.Append(classstring.ToString());                
            }
            return result.ToString();
        }
        private string MakeSqlParamAdd( List<SQLParam> param)
        {
            StringBuilder result = new StringBuilder();

            foreach(SQLParam sp in param)
            {
                StringBuilder addstring = new StringBuilder();
                addstring.Append(SqlParamAdd.ToString());

                StringBuilder p_name = new StringBuilder(sp.ParamName.ToString());
                p_name.Remove(0, 1);

                addstring.Replace("%%VARIABLE%%", p_name.ToString());

                result.Append(addstring.ToString());
            }
            return result.ToString();
        }
        private string MakeSqlParamInit(List<SQLParam> param)
        {
            StringBuilder result = new StringBuilder();

            foreach( SQLParam sp in param )
            {
                StringBuilder initstring = new StringBuilder();
                
                if( sp.IsOutput == true )
                {
                    initstring.Append(SqlParamInit_Output.ToString());
                    StringBuilder p_name = new StringBuilder(sp.ParamName.ToString());
                    p_name.Remove(0, 1); // remove '@' literal at head.
                    initstring.Replace("%%SQL_PARAM_NAME%%", p_name.ToString());
                    initstring.Replace("%%PARAM_TYPE%%", ConvertSqlDbType2String(sp.ParamType.ToString()));
                    initstring.Replace("%%PARAM_SIZE%%", sp.ParamSize.ToString());
                    initstring.Replace("%%DIRECTION%%", "Output");
                }
                else
                {
                    initstring.Append(SqlParamInit_Input.ToString());
                    StringBuilder p_name = new StringBuilder(sp.ParamName.ToString());
                    p_name.Remove(0, 1); // remove '@' literal at head.
                    initstring.Replace("%%SQL_PARAM_NAME%%", p_name.ToString());
                    initstring.Replace("%%DIRECTION%%", "Input");
                }
                result.Append(initstring.ToString());
            }
            
            return result.ToString();
        }

        private string MakeDeclareOutputParam(List<SQLParam> param)
        {
            StringBuilder result = new StringBuilder();
            foreach( SQLParam sp in param )
            {
                if( sp.IsOutput == true )
                {                    
                    StringBuilder varstring = new StringBuilder();
                    StringBuilder p_name = new StringBuilder(sp.ParamName.ToString());                     
                    p_name.Remove(0, 1); // remove '@' literal at head.

                    varstring.Append(DeclareOutputParam.ToString());
                    varstring.Replace("%%OUTPUT_PARAM_NAME%%", p_name.ToString());
                    varstring.Replace("%%OUTPUT_PARAM_TYPE%%", ConvertSqlType2CSType(sp.ParamType.ToString()));

                    result.Append(varstring.ToString());
                    //result.Append(System.Environment.NewLine);
                    varstring = null; 
                }
            }
            return result.ToString();
        }

        private string ConvertSqlDbType2String(string typestring_from_db )
        {
            Dictionary<string, string> conv_table = new Dictionary<string, string>()
            {
                {"bigint", "BigInt"},
                {"binary", "Binary"},
                {"bit", "Bit"},
                {"char", "Char"},
                {"datetime", "DateTime"},
                {"decimal", "Decimal"},
                {"float", "Float"},
                {"image", "Image"},
                {"int", "Int"},
                {"money", "Money"},
                {"nchar", "NChar"},
                {"ntext", "NText"},
                {"nvarchar", "NVarChar"},
                {"real", "Real"},
                {"smalldatetime", "SmallDateTime"},
                {"smallint", "SmallInt"},
                {"text", "Text"},
                {"timestamp", "Timestamp"},
                {"tinyint", "TinyInt"},
                {"varbinary", "VarBinary"},
                {"varchar", "VarChar"},
                {"date", "Date"},
                {"time", "Time"}
            };
            return conv_table[typestring_from_db.ToString()].ToString();
        }

        public string ConvertSqlType2CSType(string sqlType)
        {
            // add new type by your own if you're going to use another type; 
            StringBuilder cs_type = new StringBuilder();
            switch( sqlType.ToLower() )
            {
                case "bigint":
                    cs_type.Append("int64");
                    break;
                case "int":
                    cs_type.Append("int");
                    break;
                case "smallint":
                    cs_type.Append("short");
                    break;
                case "tinyint":
                    cs_type.Append("byte");
                    break;
                case "bit":
                    cs_type.Append("bool");
                    break;
                case "varchar":
                    cs_type.Append("string");
                    break;
                case "char":
                    cs_type.Append("string");
                    break;
                case "nvarchar":
                    cs_type.Append("string");
                    break;
                case "nchar":
                    cs_type.Append("string");
                    break;
                case "decimal":
                    cs_type.Append("decimal");
                    break;
                case "float":
                    cs_type.Append("float");
                    break;
                case "datetime":
                    cs_type.Append("DateTime");
                    break;
            }
            return cs_type.ToString();
        }
        public bool BuildAllStoredProcedureInfo(string server, string port, string id, string pwd, string dbname)
        {
            aspi = new Dictionary<String, List<SQLParam>>();
            List<SQLParam> spl = new List<SQLParam>();
            StringBuilder CurrentSP = new StringBuilder();

            IP.Append(server.ToString());
            Port.Append(port.ToString());
            Username.Append(id.ToString());
            Password.Append(pwd.ToString());
            this.dbname.Append( dbname.ToString());
            
            string connString = "server=" + server + "," + port + ";uid=" + id + ";pwd=" + pwd + ";database=" + dbname;
            SqlConnection con = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT SCHEMA_NAME(SCHEMA_ID) AS [Schema], SO.name AS [ObjectName], SO.Type_Desc AS [ObjectType(UDF/SP)], P.parameter_id AS [ParameterID],P.name AS [ParameterName],TYPE_NAME(P.user_type_id) AS [ParameterDataType],P.max_length AS [ParameterMaxBytes], P.is_output AS [IsOutputParameter] FROM sys.objects AS SO INNER JOIN sys.parameters AS P ON SO.OBJECT_ID = P.OBJECT_ID WHERE SO.OBJECT_ID IN (SELECT OBJECT_ID FROM sys.objects WHERE TYPE IN ('P'))  ORDER BY [Schema], SO.name, P.parameter_id";
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            if (sdr.HasRows)
            {
                while( sdr.Read())
                {
                    if( CurrentSP.Length == 0 ) CurrentSP.Append( sdr["ObjectName"].ToString());                     
                    if(0 != CurrentSP.ToString().CompareTo(sdr["ObjectName"].ToString()))
                    {
                        //new record comes, initialize vars; 
                        aspi.Add(CurrentSP.ToString(), spl);
                        spl = null;
                        spl = new List<SQLParam>();
                        CurrentSP.Clear();
                        CurrentSP.Append(sdr["ObjectName"].ToString());
                    }
                    SQLParam pm;
                    pm.ParamID = (int)sdr["ParameterID"];
                    pm.ParamName = (string)sdr["ParameterName"];
                    pm.ParamType = (string)sdr["ParameterDataType"];
                    pm.ParamSize = (short)sdr["ParameterMaxBytes"];
                    pm.IsOutput = (bool)sdr["IsOutputParameter"];
                    spl.Add(pm);
                }
                aspi.Add(CurrentSP.ToString(), spl); // for last one.
            }

            sdr.Close();
            con.Close();
            return true;
        }


        




        
    }
}
