/*
	Generated by DBSP2ClassGen. 
	Do not modified any of source below. 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data; 
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;


namespace SyncnetPlatform
{
namespace database
{
namespace db_%%DATABASE_NAME%% 
{
				
public class dbclass_%%DATABASE_NAME%%
{
	protected SqlConnection conn;
	protected SqlCommand cmd;

	protected bool IsCmdSuccess;
	protected string ErrorMessage; 

	public dbclass_%%DATABASE_NAME%%()
	{
		string connString = "server=%%SERVER_IP%%,%%SERVER_PORT%%;uid=%%USERNAME%%;pwd=%%PASSWORD%%;database=%%DATABASE_NAME%%";
		conn = new SqlConnection(connString);
		cmd = new SqlCommand();            
		cmd.Connection = conn;
		IsCmdSuccess = true; 
	}
	public bool IsOK()
	{
		return IsCmdSuccess;
	}
	public string GetLastError()
	{
		return ErrorMessage; 
	}	
}

%%DBSP2CLASSES%%
			
			 
} // end of namespace db_%%DATABASE_NAME%%
} // end of namespace database 
} // end of namespace SyncnetPlatform 