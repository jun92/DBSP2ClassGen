using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;



namespace DBSP2ClassGen
{
    public class adHocQueryAll
    {
        public List<adHocQuery> AdHocQueries;
    }
    public class adHocQuery
    {
        public string QueryId { get; set; }
        public string QueryString { get; set; }
        public List<ParamInfo> ParamInfos;
    }
    public class ParamInfo
    {
        public String name { get; set; }
        public String type { get; set; }
    }

    public class ConfigInfo
    {
        public String IP;
        public String port;
        public String Username;
        public String password;
        public String dbname;
        public String adHocFilename;
        public String GeneratedFilename;
    }
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
        private List<AdHocQuery> AdHocQueryInfo;
        private ConfigInfo ci;

        struct SQLParam
        {
            public int ParamID;
            public String ParamName;
            public String ParamType;
            public short ParamSize;
            public bool IsOutput;
        }
        struct AdHocQuery
        {
            public String QueryId;
            public String QueryString;
            public Dictionary<String, String> ParamInfo;
        }

        public dbhandler()
        {
            IP = new StringBuilder();
            Port = new StringBuilder();
            Username = new StringBuilder();
            Password = new StringBuilder();
            dbname = new StringBuilder();
            ci = new ConfigInfo();
        }

        public void LoadAllTemplate()
        {
            MainTemplate = new StringBuilder();
            MainTemplate.Append(LoadTemplate("./DBSP2Class_MainTemplate.cs"));
            SPClassesTemplate = new StringBuilder();
            SPClassesTemplate.Append(LoadTemplate("./DBSP2Class_SPClassesTemplate.cs"));
            SqlParamInit_Output = new StringBuilder();
            SqlParamInit_Output.Append(LoadTemplate("./DBSP2Class_SqlParamInit_Output.cs"));
            SqlParamInit_Input = new StringBuilder();
            SqlParamInit_Input.Append(LoadTemplate("./DBSP2Class_SqlParamInit_Input.cs"));
            SqlParamAdd = new StringBuilder();
            SqlParamAdd.Append(LoadTemplate("./DBSP2Class_SqlParamAdd.cs"));
            DeclareOutputParam = new StringBuilder();
            DeclareOutputParam.Append(LoadTemplate("./DBSP2Class_DeclareOutputParam.cs"));
            SaveOutputParam = new StringBuilder();
            SaveOutputParam.Append(LoadTemplate("./DBSP2Class_SaveOutputParam.cs"));
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

        public bool Build(string filename)
        {
            StringBuilder SPClasses = new StringBuilder();
            StringBuilder Body = new StringBuilder();

            ci.GeneratedFilename = filename;
            Body = MainTemplate;

            SPClasses.Append(CreateClassSource()); 

            Body.Replace("%%DBSP2CLASSES%%", SPClasses.ToString());
            Body.Replace("%%DATABASE_NAME%%", dbname.ToString());
            Body.Replace("%%SERVER_IP%%", IP.ToString());
            Body.Replace("%%SERVER_PORT%%", Port.ToString());
            Body.Replace("%%USERNAME%%", Username.ToString());
            Body.Replace("%%PASSWORD%%", Password.ToString());

            SaveAsFile(Body.ToString(), filename);

            return true;
        }
        
        private string CreateClassSource()
        {
            StringBuilder result = new StringBuilder();

            foreach ( KeyValuePair<string, List<SQLParam>> pair in aspi)
            {
                // building class form.
                StringBuilder classstring = new StringBuilder();
                classstring.Append(SPClassesTemplate.ToString() ); 

                // building "decalre variables" <- it should be output param.
                StringBuilder declare = new StringBuilder();
                declare.Append(MakeDeclareOutputParam(pair.Value));

                // build variables initialization code 
                StringBuilder sqlparam_init = new StringBuilder();
                sqlparam_init.Append(MakeSqlParamInit(pair.Value));

                // build variables added to SqlCommand object.
                StringBuilder sqlParamAdd = new StringBuilder();
                sqlParamAdd.Append(MakeSqlParamAdd(pair.Value));

                // build code to return the output params 
                StringBuilder saveOutputParam = new StringBuilder();
                saveOutputParam.Append(MakeSaveOutputParam(pair.Value));

                // build method's signature.
                StringBuilder DeclareMethodParam = new StringBuilder();
                DeclareMethodParam.Append(MakeDeclareMethodParam(pair.Value)); 


                classstring.Replace("%%DECLARE_OUTPUT_PARAM%%", declare.ToString());
                classstring.Replace("%%SP_NAME%%", pair.Key.ToString());
                classstring.Replace("%%CLASS_NAME%%", pair.Key.ToString());
                classstring.Replace("%%SQLPARAM_INIT%%", sqlparam_init.ToString());
                classstring.Replace("%%SQLPARAM_ADD%%", sqlParamAdd.ToString());
                classstring.Replace("%%SAVE_OUTPUT_PARAM%%", saveOutputParam.ToString());
                classstring.Replace("%%DECLARE_METHOD_PARAMS%%", DeclareMethodParam.ToString());

                result.Append(classstring.ToString());                
            }
            if (AdHocQueryInfo != null) // if there are any ad-hoc queries.
            {
                foreach (AdHocQuery item in AdHocQueryInfo)
                {
                    // building class form.
                    StringBuilder classstring = new StringBuilder();
                    classstring.Append(SPClassesTemplate.ToString());

                    StringBuilder sqlparam_init = new StringBuilder();
                    sqlparam_init.Append(MakeSqlParamInit_AdHoc(item.ParamInfo));

                    StringBuilder sqlparam_add = new StringBuilder();
                    sqlparam_add.Append(MakeSqlParamAdd_AdHoc(item.ParamInfo));

                    StringBuilder DeclareMethodParams = new StringBuilder();
                    DeclareMethodParams.Append(MakeDeclareMethodParam_AdHoc(item.ParamInfo));


                    classstring.Replace("System.Data.CommandType.StoredProcedure", "System.Data.CommandType.Text");
                    classstring.Replace("%%DECLARE_OUTPUT_PARAM%%", "");
                    classstring.Replace("%%SAVE_OUTPUT_PARAM%%", "");
                    classstring.Replace("%%SP_NAME%%", item.QueryString.ToString());
                    classstring.Replace("%%CLASS_NAME%%", item.QueryId.ToString());

                    classstring.Replace("%%SQLPARAM_INIT%%", sqlparam_init.ToString());
                    classstring.Replace("%%SQLPARAM_ADD%%", sqlparam_add.ToString());

                    classstring.Replace("%%DECLARE_METHOD_PARAMS%%", DeclareMethodParams.ToString());

                    classstring.Replace("%%SAVE_OUTPUT_PARAM%%", "");
                    result.Append(classstring.ToString());
                }
            }
            
            return result.ToString();
        }

        private string MakeDeclareMethodParam( List<SQLParam> param)
        {
            StringBuilder result = new StringBuilder();
            List<String> declare_param_list = new List<String>(); 

            foreach( SQLParam sp in param)
            {
                if( !sp.IsOutput ) // input parameters should be in method's signature.
                {
                    StringBuilder p = new StringBuilder();
                    p.Append(ConvertSqlType2CSType(sp.ParamType.ToString()));
                    p.Append(" ");

                    StringBuilder p_name = new StringBuilder(sp.ParamName.ToString());
                    p_name.Remove(0, 1);
                    p.Append(p_name.ToString());

                    declare_param_list.Add(p.ToString());
                }
            }
            for( int i = 0; i < declare_param_list.Count; i++)
            {
                result.Append(declare_param_list[i].ToString());
                if (i != (declare_param_list.Count - 1))
                    result.Append(", ");
            }
            return result.ToString();
        }
        private  string MakeDeclareMethodParam_AdHoc(Dictionary<String,String> param)
        {
            StringBuilder result = new StringBuilder();
            int i = 0 ;             
            foreach (KeyValuePair<String, String> pair in param)
            {
                result.Append(pair.Value.ToString() + " " + pair.Key.ToString());
                i++;
                if( i != param.Count )
                {
                    result.Append(",");
                }
            }
            return result.ToString(); 
        }
        private string MakeSaveOutputParam( List<SQLParam> param)
        {
            StringBuilder result = new StringBuilder();

            foreach( SQLParam sp in param)
            {
                if (sp.IsOutput)
                {
                    StringBuilder savestring = new StringBuilder();
                    savestring.Append(SaveOutputParam.ToString());

                    StringBuilder p_name = new StringBuilder(sp.ParamName.ToString());
                    p_name.Remove(0, 1);

                    savestring.Replace("%%SQL_PARAM_NAME%%", p_name.ToString());
                    savestring.Replace("%%OUTPUT_PARAM_TYPE%%", ConvertSqlType2CSType(sp.ParamType.ToString()));

                    result.Append(savestring.ToString());
                }

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
        private string MakeSqlParamAdd_AdHoc( Dictionary<String, String> param)
        {
            StringBuilder result = new StringBuilder();
            

            foreach (KeyValuePair<String, String> pair in param)
            {
                StringBuilder addstring = new StringBuilder();
                addstring.Append(SqlParamAdd.ToString()); // 템플릿 로그 
                addstring.Replace("%%VARIABLE%%", pair.Key.ToString());
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
        
        private string MakeSqlParamInit_AdHoc(Dictionary<string,string> param)
        {
            StringBuilder result = new StringBuilder();

            foreach( KeyValuePair<String, String> pair in param)            
            {
                StringBuilder initstring = new StringBuilder();
                initstring.Append(SqlParamInit_Input.ToString()); // 템플릿 로그 
                initstring.Replace("%%SQL_PARAM_NAME%%", pair.Key.ToString());
                initstring.Replace("%%DIRECTION%%", "Input"); 
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
                case "bigint":      cs_type.Append("long");    break;
                case "int":         cs_type.Append("int");      break;
                case "smallint":    cs_type.Append("short");    break;
                case "tinyint":     cs_type.Append("byte");     break;
                case "bit":         cs_type.Append("bool");     break;
                case "varchar":     cs_type.Append("string");   break;
                case "char":        cs_type.Append("string");   break;
                case "nvarchar":    cs_type.Append("string");   break;
                case "nchar":       cs_type.Append("string");   break;
                case "decimal":     cs_type.Append("decimal");  break;
                case "float":       cs_type.Append("float");    break;
                case "datetime":    cs_type.Append("DateTime"); break;
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

            ci.IP = server;
            ci.port = port;
            ci.Username = id;
            ci.password = pwd;
            ci.dbname = dbname;
            
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
        public bool LoadAdHocQueryInfo(string filename)
        {
            ci.adHocFilename = filename;
            AdHocQueryInfo = new List<AdHocQuery>();

            string jsonText = LoadJson(filename);
            adHocQueryAll aqa = JsonConvert.DeserializeObject<adHocQueryAll>(jsonText);

            foreach (DBSP2ClassGen.adHocQuery aq in aqa.AdHocQueries)
            {
                AdHocQuery ahq = new AdHocQuery();
                ahq.QueryId = aq.QueryId;
                ahq.QueryString = aq.QueryString;

                ahq.ParamInfo = new Dictionary<string, string>();

                foreach( DBSP2ClassGen.ParamInfo pi in aq.ParamInfos)
                {
                    ahq.ParamInfo.Add(pi.name, pi.type);
                }
                AdHocQueryInfo.Add(ahq);
            }
            return true;
        } 

        public string LoadJson(string filename)
        {
            StringBuilder temp = new StringBuilder();
            StreamReader sr = new StreamReader(filename);
            while (sr.Peek() >= 0)
            {
                temp.Append(sr.ReadLine());                
            }
            sr.Close();
            return temp.ToString();
        }
        public void SaveJson(string jsontext)
        {
            StreamWriter sw = new StreamWriter("./config.json");
            sw.Write(jsontext);
            sw.Close();

        }
        public void SaveConfig()
        {
            string output = JsonConvert.SerializeObject(ci);
            SaveJson(output);
        }
        public void GetConfigInfo(ref ConfigInfo ci)
        {
            string jsonText = LoadJson("./config.json");
            ci = JsonConvert.DeserializeObject<ConfigInfo>(jsonText);
        }
    } // end of class 
    
} // end of namespace 

